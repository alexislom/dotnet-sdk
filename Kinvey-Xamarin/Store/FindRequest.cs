﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Remotion.Linq;

namespace KinveyXamarin
{
	public class FindRequest<T> : ReadRequest<T, List<T>>
	{
		private List<string> EntityIDs { get; }
		private KinveyQuery<T> QueryObj { get; }
		private StringQueryBuilder Writer { get; }

		public FindRequest(AbstractClient client, string collection, ICache<T> cache, ReadPolicy policy, List<string> listIDs, KinveyQuery<T> queryObj)
			: base(client, collection, cache, policy)
		{
			EntityIDs = listIDs;
			QueryObj = queryObj;
			Writer = new StringQueryBuilder();
		}

		public override async Task<List<T>> ExecuteAsync()
		{
			List<T> listResult = default(List<T>);

			switch (Policy)
			{
				case ReadPolicy.FORCE_LOCAL:
					// sync
					if (EntityIDs?.Count > 0)
					{
						listResult = Cache.FindByIDs(EntityIDs);
					}
					else if (QueryObj != null)
					{
						try
						{
							PerformLocalQuery();
						}
						catch (Exception e)
						{
							QueryObj.OnError(e);
						}
					}
					else
					{
						return Cache.FindAll();
					}
					break;

				case ReadPolicy.FORCE_NETWORK:
					// network
					if (EntityIDs?.Count > 0)
					{
						listResult = new List<T>();

						foreach (string entityID in EntityIDs)
						{
							T item = await Client.NetworkFactory.buildGetByIDRequest<T>(Collection, entityID).ExecuteAsync();
							listResult.Add(item);
						}
					}
					else if (QueryObj != null)
					{
						try
						{
							await PerformNetworkQuery();
						}
						catch (Exception e)
						{
							// network error
							QueryObj.OnError(e);
						}
					}
					else
					{
						listResult = await Client.NetworkFactory.buildGetRequest<T>(Collection).ExecuteAsync();
					}
					break;

				case ReadPolicy.BOTH:
					// cache
					if (EntityIDs?.Count > 0)
					{
						// TODO VRG implement
					}
					else if (QueryObj != null)
					{
						try
						{
							// first, perform local query
							PerformLocalQuery();

							// once local query finishes, perform network query
							PerformNetworkQuery();
						}
						catch (Exception e)
						{
							QueryObj.OnError(e);
						}
					}
					else
					{
						// TODO VRG implement
					}
					break;

				default:
					throw new KinveyException(EnumErrorCode.ERROR_GENERAL, "Invalid read policy");
			}

			return listResult;
		}

		public override async Task<bool> Cancel()
		{
			throw new KinveyException(EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "Cancel method on FindRequest not implemented.");
		}

		private async Task PerformNetworkQuery()
		{
			Writer.Reset();

			KinveyQueryVisitor visitor = new KinveyQueryVisitor(Writer, typeof(T));

			QueryModel queryModel = (QueryObj.Query.Provider as KinveyQueryProvider).qm;

			Writer.Write("{");
			queryModel.Accept (visitor);
			Writer.Write("}");

			string mongoQuery = Writer.GetFullString();

			List<T> networkResults = await Client.NetworkFactory.buildGetRequest<T>(Collection, mongoQuery).ExecuteAsync();

			foreach (T networkItem in networkResults)
			{
				QueryObj.OnNext(networkItem);
			}

			QueryObj.OnCompleted();
		}

		private void PerformLocalQuery()
		{
			IQueryable<T> query = QueryObj.Query;
			List<T> cacheResults = Cache.FindByQuery(query.Expression);

			foreach (T cacheItem in cacheResults)
			{
				QueryObj.OnNext(cacheItem);
			}

			QueryObj.OnCompleted();
		}
	}
}

