﻿// Copyright (c) 2015, Kinvey, Inc. All rights reserved.
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
using Remotion.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using Remotion.Linq.Parsing.Structure;

namespace Kinvey
{
	public class KinveyQueryExecutor<K> : IQueryExecutor
	{
		
		public StringQueryBuilder writer;
		public KinveyQueryable<K> queryable;

		public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
		{
			throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "ExecuteCollection<T> method on KinveyQueryExecutor not implemented.");
		}

		public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
		{
			throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "ExecuteSingle<T> method on KinveyQueryExecutor not implemented.");
		}

		public T ExecuteScalar<T>(QueryModel queryModel)
		{
			throw new KinveyException(EnumErrorCategory.ERROR_GENERAL, EnumErrorCode.ERROR_METHOD_NOT_IMPLEMENTED, "ExecuteScalar<T> method on KinveyQueryExecutor not implemented.");
		}
	}
}
