﻿// Copyright (c) 2017, Kinvey, Inc. All rights reserved.
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

namespace Kinvey
{
	public class KinveyDataStoreDelegate<T>
	{
		/// <summary>
		/// This action is executed when there is a new or updated
		/// entity available from the realtime service.
		/// </summary>
		public Action<T> OnNext;

		/// <summary>
		/// This action is executed when an exception occurs.  This can be
		/// either on the device, or returned from the realtime service.
		/// </summary>
		public Action<Exception> OnError;

		/// <summary>
		/// This action is executed when there is a connection
		/// status message available from the realtime service.
		/// </summary>
		public Action<KinveyRealtimeStatus> OnStatus;
	}
}
