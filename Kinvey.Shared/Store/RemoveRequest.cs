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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Kinvey
{
	public class RemoveRequest <T> : WriteRequest <T, KinveyDeleteResponse>
	{
		private string entityID;
        private readonly IQueryable<object> _query;


        public RemoveRequest(string entityID, AbstractClient client, string collection, ICache<T> cache, ISyncQueue sync, WritePolicy policy)
			: base(client, collection, cache, sync, policy)
		{
			this.entityID = entityID;
		}

        public RemoveRequest(IQueryable<object> query, AbstractClient client, string collection, ICache<T> cache, ISyncQueue sync, WritePolicy policy) : base(client, collection, cache, sync, policy)
        {
            _query = query;
        }

        public override async Task<KinveyDeleteResponse> ExecuteAsync()
		{
			var kdr = default(KinveyDeleteResponse);

            switch (Policy)
            {
                case WritePolicy.FORCE_LOCAL:
                    // sync
                    if (_query == null)
                    {
                        // cache
                        kdr = Cache.DeleteByID(entityID);

                        var request = Client.NetworkFactory.buildDeleteRequest<KinveyDeleteResponse>(Collection, entityID);

                        var pendingAction = PendingWriteAction.buildFromRequest(request);
                        SyncQueue.Enqueue(pendingAction);
                    }
                    else
                    {
                        // cache
                        kdr = Cache.DeleteByQuery(_query);

                        foreach (var id in kdr.IDs) {

                            var request = Client.NetworkFactory.buildDeleteRequest<KinveyDeleteResponse>(Collection, id);
                            var pendingAction = PendingWriteAction.buildFromRequest(request);
                            SyncQueue.Enqueue(pendingAction);
                        }
                    }
                    break;

                case WritePolicy.FORCE_NETWORK:
                    // network
                    if (_query == null)
                    {
                        kdr = await Client.NetworkFactory.buildDeleteRequest<KinveyDeleteResponse>(Collection, entityID).ExecuteAsync();
                    }
                    else
                    {
                        var mongoQuery = KinveyMongoQueryBuilder.GetQueryForRemoveOperation<T>(_query);
                        kdr = await Client.NetworkFactory.buildDeleteRequestWithQuery<KinveyDeleteResponse>(Collection, mongoQuery).ExecuteAsync();
                    }
                    break;

                case WritePolicy.NETWORK_THEN_LOCAL:
                    if (_query == null)
                    {
                        // cache
                        kdr = Cache.DeleteByID(entityID);

                        // network
                        kdr = await Client.NetworkFactory.buildDeleteRequest<KinveyDeleteResponse>(Collection, entityID).ExecuteAsync();
                    }
                    else
                    {
                        // cache
                        kdr = Cache.DeleteByQuery(_query);

                        // network
                        var mongoQuery = KinveyMongoQueryBuilder.GetQueryForRemoveOperation<T>(_query);
                        kdr = await Client.NetworkFactory.buildDeleteRequestWithQuery<KinveyDeleteResponse>(Collection, mongoQuery).ExecuteAsync();
                    }
                    break;

                case WritePolicy.LOCAL_THEN_NETWORK:                   
                    if (_query == null)
                    {
                        // cache
                        kdr = Cache.DeleteByID(entityID);

                        var deleteRequest = Client.NetworkFactory.buildDeleteRequest<KinveyDeleteResponse>(Collection, entityID);

                        KinveyException kinveyException = null;
                        Exception exception = null;
                        try
                        { 
                            // network
                            kdr = await deleteRequest.ExecuteAsync();
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
                            var pendingAction = PendingWriteAction.buildFromRequest(deleteRequest);
                            SyncQueue.Enqueue(pendingAction);

                            if (kinveyException != null)
                            {
                                throw kinveyException;
                            }
                        }                       
                    }
                    else
                    {
                        // cache
                        kdr = Cache.DeleteByQuery(_query);

                        // network
                        KinveyException kinveyException = null;
                        Exception exception = null;
                        try
                        { 
                            var mongoQuery = KinveyMongoQueryBuilder.GetQueryForRemoveOperation<T>(_query);
                            kdr = await Client.NetworkFactory.buildDeleteRequestWithQuery<KinveyDeleteResponse>(Collection, mongoQuery).ExecuteAsync();
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
                            foreach (var id in kdr.IDs)
                            {

                                var request = Client.NetworkFactory.buildDeleteRequest<KinveyDeleteResponse>(Collection, id);
                                var pendingAction = PendingWriteAction.buildFromRequest(request);
                                SyncQueue.Enqueue(pendingAction);
                            }

                            if (kinveyException != null)
                            {
                                throw kinveyException;
                            }
                        }
                    }
                    break;

                default:
                    throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_GENERAL, "Invalid write policy");
            }

			return kdr;
		}

		public override Task<bool> Cancel()
		{
			throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "Cancel method on RemoveRequest not implemented.");
		}
	}
}
