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

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kinvey
{
	/// <summary>
	/// Kinvey user metadata.
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
	public class KinveyAuthSocialID : JObject
	{
		public KinveyAuthSocialID()
		{
			this.AuthMetaData = new KinveyAuthMetaData();
		}

		/// <summary>
		/// Gets or sets the email verification information for a user.
		/// </summary>
		[Preserve]
		[JsonProperty("kinveyAuth")]
		public KinveyAuthMetaData AuthMetaData { get; set; }

		/// <summary>
		/// A name-value dictionary of custom attributes of the _socialIdentity object
		/// </summary>
		[Preserve]
		[JsonExtensionData]
		public Dictionary<string, JToken> Attributes;
	}
}
