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
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Kinvey
{
	/// <summary>
	/// Request operation for pulling all records for a collection during a sync, and refreshing the cache with the
	/// updated data.
	/// </summary>
	public class PullRequest<T> : ReadRequest<T, PullDataStoreResponse<T>>
	{
		public PullRequest(AbstractClient client, string collection, ICache<T> cache, bool deltaSetFetchingEnabled, IQueryable<object> query)
			: base(client, collection, cache, query, ReadPolicy.FORCE_NETWORK, deltaSetFetchingEnabled)
		{
		}

		public override async Task<PullDataStoreResponse<T>> ExecuteAsync()
		{
			var result = await PerformNetworkFind();
			return new PullDataStoreResponse<T>(result.TotalCount, result.ResultSet.Count, result.ResultSet);
		}

		public override Task<bool> Cancel()
		{
			throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "Cancel method on PullRequest not implemented.");
		}
	}
}
