﻿// Copyright (c) 2019, Kinvey, Inc. All rights reserved.
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
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kinvey
{
	public class SaveRequest <T> : WriteRequest<T, T>
	{
		private T entity;

		public SaveRequest (T entity, AbstractClient client, string collection, ICache<T> cache, ISyncQueue sync, WritePolicy policy)
			: base (client, collection, cache, sync, policy)
		{
			this.entity = entity;
		}

		public override async Task<T> ExecuteAsync()
		{
			T savedEntity = default(T);
			NetworkRequest<T> request = null;
            string entityID = null; ;

            JToken idToken = JObject.FromObject (entity) ["_id"];
			if (idToken != null &&
			    !String.IsNullOrEmpty(idToken.ToString()))
			{
				entityID = idToken.ToString();
				request = Client.NetworkFactory.buildUpdateRequest(Collection, entity, entityID);
			}
			else
			{
				request = Client.NetworkFactory.buildCreateRequest(Collection, entity);
			}
        
            switch (Policy)
			{
				case WritePolicy.FORCE_LOCAL:
					// sync
					PendingWriteAction pendingAction = PendingWriteAction.buildFromRequest(request);

					string saveModeLocal = request.RequestMethod;
					string tempIdLocal = null;

					if (String.Equals("POST", saveModeLocal))
					{
                        tempIdLocal = PrepareCacheSave(ref entity);
						savedEntity = Cache.Save(entity);
						pendingAction.entityId = tempIdLocal;
					}
					else
					{
						savedEntity = Cache.Update(entity);
					}

					SyncQueue.Enqueue(pendingAction);

					break;

				case WritePolicy.FORCE_NETWORK:
					// network
					savedEntity = await request.ExecuteAsync ();
					break;

				case WritePolicy.NETWORK_THEN_LOCAL:
                    // cache
                    string saveModeNetworkThenLocal = request.RequestMethod;
                    string tempIdNetworkThenLocal = null;

                    if (String.Equals("POST", saveModeNetworkThenLocal))
                    {
                        tempIdNetworkThenLocal = PrepareCacheSave(ref entity);
                        Cache.Save(entity);
                    }
                    else
                    {
                        Cache.Update(entity);
                    }

                    // network save
                    savedEntity = await request.ExecuteAsync();

                    if (tempIdNetworkThenLocal != null)
                    {
                        Cache.UpdateCacheSave(savedEntity, tempIdNetworkThenLocal);
                    }

                    break;

                case WritePolicy.LOCAL_THEN_NETWORK:                    
                    string saveModeLocalThenNetwork = request.RequestMethod;

                    // cache
                    if (String.Equals("POST", saveModeLocalThenNetwork))
					{
                        entityID = PrepareCacheSave(ref entity);
                        savedEntity = Cache.Save(entity);
					}
					else
					{
                        savedEntity = Cache.Update(entity);
					}

                    KinveyException kinveyException = null;
                    Exception exception = null;                   
                    try
                    {
                        // network save
                        savedEntity = await request.ExecuteAsync();
                    }                   
                    catch (KinveyException kinveyEx)
                    {
                        kinveyException = kinveyEx;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    if (kinveyException != null || exception != null)
                    {
                        // if the network request fails, save data to sync queue
                        var localPendingAction = PendingWriteAction.buildFromRequest(request);
                        localPendingAction.entityId = entityID;

                        SyncQueue.Enqueue(localPendingAction);

                        if (kinveyException != null)
                        {
                            throw kinveyException;
                        }
                    }
                    else 
					{
						Cache.UpdateCacheSave(savedEntity, entityID);
					}

					break;

                default:
					throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_GENERAL, "Invalid write policy");
			}

			return savedEntity;
		}

		public override Task<bool> Cancel()
		{
			throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "Cancel method on SaveRequest not implemented.");
		}	
	}
}
