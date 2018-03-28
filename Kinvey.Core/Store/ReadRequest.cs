﻿// Copyright (c) 2016, Kinvey, Inc. All rights reserved.
//
// This software is licensed to you under the Kinvey terms of service located at
// http://www.kinvey.com/terms-of-use. By downloading, accessing and/or using this
// software, you hereby accept such terms of service  (and any agreement referenced
// therein) and agree that you have read, understand and agree to be bound by such
// terms of service and are of legal age to agree to such terms with Kinvey.
//
// This software contains valuable confidential and proprietary information of
// KINVEY, INC and is subject to applicable licensing agreements.
// Unauthorized reproduction, transmission or distribution of this file and its
// contents is a violation of applicable laws.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Remotion.Linq;

namespace Kinvey
{
	public abstract class ReadRequest <T, U> : Request <T, U>
	{
		public ICache<T> Cache { get; }
		public string Collection { get; }
		public ReadPolicy Policy { get; }
		protected IQueryable<object> Query { get; }
		protected bool DeltaSetFetchingEnabled { get; }
		protected List<string> EntityIDs { get; }

		public ReadRequest(AbstractClient client, string collection, ICache<T> cache, IQueryable<object> query, ReadPolicy policy, bool deltaSetFetchingEnabled)
	: base(client)
		{
			this.Cache = cache;
			this.Collection = collection;
			this.Query = query;
			this.Policy = policy;
			this.DeltaSetFetchingEnabled = deltaSetFetchingEnabled;
		}


		public ReadRequest (AbstractClient client, string collection, ICache<T> cache, IQueryable<object> query, ReadPolicy policy, bool deltaSetFetchingEnabled, List<String> entityIds)
			: base(client)
		{
			this.Cache = cache;
			this.Collection = collection;
			this.Query = query;
			this.Policy = policy;
			this.DeltaSetFetchingEnabled = deltaSetFetchingEnabled;
			this.EntityIDs = entityIds;
		}

		/// <summary>
		/// Builds the mongo-style query string to be run against the backend.
		/// </summary>
		/// <returns>The mongo-style query string.</returns>
		protected string BuildMongoQuery()
		{
			if (Query != null)
			{
				StringQueryBuilder queryBuilder = new StringQueryBuilder();

				KinveyQueryVisitor visitor = new KinveyQueryVisitor(queryBuilder, typeof(T));
				QueryModel queryModel = (Query.Provider as KinveyQueryProvider)?.qm;

				queryBuilder.Write("{");
				queryModel?.Accept(visitor);
				queryBuilder.Write("}");

				string mongoQuery = queryBuilder.BuildQueryString();

				return mongoQuery;
			}

			return default (string);
		}


		protected async Task<List<T>> RetrieveDeltaSet(List<T> cacheItems, List<DeltaSetFetchInfo> networkItems, string mongoQuery)
		{
			List<T> listDeltaSetResults = new List<T>();

			#region DSF Step 2: Pull all entity IDs and LMTs of a collection in local storage

			Dictionary<string, string> dictCachedEntities = new Dictionary<string, string>();

			foreach (var cacheItem in cacheItems)
			{
				var item = cacheItem as IPersistable;
				if (item.KMD?.lastModifiedTime != null) {  //if lmt doesn't exist for cache entity, avoid crashing
					dictCachedEntities.Add(item.ID, item.KMD.lastModifiedTime);
				}
			}

			List<string> listCachedEntitiesToRemove = new List<string>(dictCachedEntities.Keys);

			#endregion

			#region DSF Step 3: Compare backend and local entities to see what has been created, deleted and updated since the last fetch

			List<string> listIDsToFetch = new List<string>();

			foreach (var networkEntity in networkItems)
			{
				string ID = networkEntity.ID;
				string LMT = networkEntity.KMD.lastModifiedTime;

				if (!dictCachedEntities.ContainsKey(ID))
				{
					// Case where a new item exists in the backend, but not in the local cache
					listIDsToFetch.Add(ID);
				}
				else if (HelperMethods.IsDateMoreRecent(LMT, dictCachedEntities[ID]))
				{
					// Case where the backend has a more up-to-date version of the entity than the local cache
					listIDsToFetch.Add(ID);
				}

				// Case where the backend has deleted an item that has not been removed from local storage.
				//
				// To begin with, this list has all the IDs currently present in local storage.  If an ID
				// has been found in the set of backend IDs, we will remove it from this list.  What will
				// remain in this list are all the IDs that are currently in local storage that
				// are not present in the backend, and therefore have to be deleted from local storage.
				listCachedEntitiesToRemove.Remove(ID);

				// NO-OPS: Should never hit these cases, because a Push() has to happen prior to a pull
				// 		Case where a new item exists in the local cache, but not in the backend
				// 		Case where the local cache has a more up-to-date version of the entity than the backend
				// 		Case where the local cache has deleted an item that has not been removed from the backend
			}

			#endregion

			#region DSF Step 4: Remove items from local storage that are no longer in the backend

			Cache.DeleteByIDs(listCachedEntitiesToRemove);

			#endregion

			#region DSF Step 5: Fetch selected IDs from backend to update local storage

			// Then, with this set of IDs from the previous step, make a query to the
			// backend, to get full records for each ID that has changed since last fetch.
			int numIDs = listIDsToFetch.Count;

			if (numIDs == networkItems.Count) {
				//Special case where delta set is the same size as the network result.
				//This will occur either when all entities are new/updated, or in error cases such as missing lmts
				return await RetrieveNetworkResults(mongoQuery);
			}

			int start = 0;
			int batchSize = 200;

			while (start < numIDs)
			{
				int count = Math.Min((numIDs - start), batchSize);
				string queryIDs = BuildIDsQuery(listIDsToFetch.GetRange(start, count));
				List<T> listBatchResults = await Client.NetworkFactory.buildGetRequest<T>(Collection, queryIDs).ExecuteAsync();

				start += listBatchResults.Count();
				listDeltaSetResults.AddRange(listBatchResults);
			}

			#endregion

			return listDeltaSetResults;
		}

		protected List<T> PerformLocalFind(KinveyDelegate<List<T>> localDelegate = null)
		{
			List<T> cacheHits = default(List<T>);

			try
			{
				if (Query != null)
				{
					IQueryable<object> query = Query;
					cacheHits = Cache.FindByQuery(query.Expression);
				}
				else if (EntityIDs?.Count > 0)
				{
					cacheHits = Cache.FindByIDs(EntityIDs);
				}
				else
				{
					cacheHits = Cache.FindAll();
				}

				localDelegate?.onSuccess(cacheHits);
			}
			catch (Exception e)
			{
				if (localDelegate != null)
				{
					localDelegate.onError(e);
				}
				else
				{
					throw e;
				}
			}

			return cacheHits;
		}

		protected async Task<NetworkReadResponse<T>> PerformNetworkFind()
		{
			try
			{
				string mongoQuery = this.BuildMongoQuery();

                if (DeltaSetFetchingEnabled && !Cache.IsCacheEmpty())
				{
                    QueryCacheItem queryCacheItem = Client.CacheManager.GetQueryCacheItem(Collection, mongoQuery, null);
                    if (queryCacheItem != null && !string.IsNullOrEmpty(queryCacheItem.lastRequest))
                    {
                        // Able to perform server-side delta set fetch
                        NetworkRequest<DeltaSetResponse<T>> request = Client.NetworkFactory.BuildDeltaSetRequest<DeltaSetResponse<T>>(queryCacheItem.collectionName, queryCacheItem.lastRequest, queryCacheItem.query);
                        DeltaSetResponse<T> results = await request.ExecuteAsync();

                        // With the _deltaset endpoint result from the server:

                        // 1 - Apply deleted set to local cache
                        List<string> listDeletedIDs = new List<string>();
                        foreach (var deletedItem in results.Deleted)
                        {
                            listDeletedIDs.Add(deletedItem.ID);
                        }
                        Cache.DeleteByIDs(listDeletedIDs);

                        // 2 - Apply changed set to local cache
                        Cache.RefreshCache(results.Changed);

                        // 3 - Update the last request time for this combination
                        // of collection:query
                        queryCacheItem.lastRequest = results.LastRequestTime;
                        Client.CacheManager.SetQueryCacheItem(queryCacheItem);

                        // 4 - Return network results
                        return new NetworkReadResponse<T>(results.Changed, results.Changed.Count, true);
                    }
                    else
                    {
                        // Perform regular GET
                        var getResult = await PerformNetworkGet(mongoQuery);
                        queryCacheItem = new QueryCacheItem(Collection, mongoQuery, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        Client.CacheManager.SetQueryCacheItem(queryCacheItem);
                        return getResult;
                    }

					//var localResults = PerformLocalFind();

					//if (localResults?.Count > 0)
					//{
					//	#region DSF Step 1: Pull all entity IDs and LMTs of a collection in the backend
					//	if (String.IsNullOrEmpty(mongoQuery))
					//	{
					//		mongoQuery = "{}";
					//	}

					//	var fieldsQuery = mongoQuery + "&fields=_id,_kmd.lmt";

					//	List<DeltaSetFetchInfo> networkMetadata = await Client.NetworkFactory.buildGetRequest<DeltaSetFetchInfo>(Collection, fieldsQuery).ExecuteAsync();

					//	#endregion

					//	var delta = await RetrieveDeltaSet(localResults, networkMetadata, mongoQuery);
					//	if (delta.Count > 0)
					//	{
					//		Cache.RefreshCache(delta);
					//	}

					//	return new NetworkReadResponse<T>(delta, networkMetadata.Count, true);
					//}
				}

                var networkGetResult = await PerformNetworkGet(mongoQuery);
                var qci = new QueryCacheItem(Collection, mongoQuery, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                Client.CacheManager.SetQueryCacheItem(qci);
                return networkGetResult;

			}
			catch (KinveyException ke)
			{
				throw ke;
			}
			catch (Exception e)
			{
				throw new KinveyException(EnumErrorCategory.ERROR_DATASTORE_NETWORK,
										  EnumErrorCode.ERROR_GENERAL,
										  "Error in FindAsync() for network results.",
										  e);
			}
		}

        protected async Task<List<T>> RetrieveNetworkResults(string mongoQuery)
		{
			List<T> networkResults = default(List<T>);

			if (Query != null)
			{
				networkResults = await Client.NetworkFactory.buildGetRequest<T>(Collection, mongoQuery).ExecuteAsync();
			}
			else if (EntityIDs?.Count > 0)
			{
				networkResults = new List<T>();
				foreach (string entityID in EntityIDs)
				{
					T item = await Client.NetworkFactory.buildGetByIDRequest<T>(Collection, entityID).ExecuteAsync();
					networkResults.Add(item);
				}
			}
			else
			{
				networkResults = await Client.NetworkFactory.buildGetRequest<T>(Collection).ExecuteAsync();
			}

			return networkResults;
		}

		private string BuildIDsQuery(List<string> listIDs)
		{
			System.Text.StringBuilder query = new System.Text.StringBuilder();

			query.Append("{\"_id\": { \"$in\": [");

			bool isNotFirstID = false;
			foreach (var ID in listIDs)
			{
				if (isNotFirstID)
				{
					query.Append(",");
				}

				query.Append("\"");
				query.Append(ID);
				query.Append("\"");

				isNotFirstID = true;
			}

			query.Append("] } }");

			// TODO need to add back in any modifiers from original query

			return query.ToString();
		}

        private async Task<NetworkReadResponse<T>> PerformNetworkGet(string mongoQuery)
        {
            var results = await RetrieveNetworkResults(mongoQuery);
            Cache.Clear(Query?.Expression);
            Cache.RefreshCache(results);
            return new NetworkReadResponse<T>(results, results.Count, false);
        }

		protected class NetworkReadResponse<T>
		{
			public List<T> ResultSet;
			public int TotalCount;
			public bool IsDeltaFetched;

			public NetworkReadResponse(List<T> result, int count, bool isDelta)
			{
				this.ResultSet = result;
				this.TotalCount = count;
				this.IsDeltaFetched = isDelta;
			}
		}

	}
}
