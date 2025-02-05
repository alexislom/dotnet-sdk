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
using System.Runtime.Serialization;
using Newtonsoft.Json;
using SQLite;
namespace Kinvey
{
	/// <summary>
	/// Base class for model objects backed by Kinvey.  Implements the
	/// <see cref="IPersistable"/> interface
	/// </summary>
	[JsonObject(MemberSerialization.OptIn)]
    [DataContract]
    public class Entity : IPersistable
	{
		/// <summary>
		/// Gets or sets the Kinvey ID.
		/// </summary>
		/// <value>The identifier.</value>
		[JsonProperty("_id")]
        [DataMember(Name = "_id")]
        [Preserve]
		[PrimaryKey, Column("_id")]
		public string ID { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="AccessControlList"/> for this Kinvey-backed object.
		/// </summary>
		/// <value>The acl.</value>
		[JsonProperty("_acl")]
        [DataMember(Name = "_acl")]
        [Preserve]
		[Column("_acl")]
		public AccessControlList Acl { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="KinveyMetaData"/> for this Kinvey-backed object.
		/// </summary>
		/// <value>The kmd.</value>
		[JsonProperty("_kmd")]
        [DataMember(Name = "_kmd")]
        [Preserve]
		[Column("_kmd")]
		public KinveyMetaData Kmd { get; set; }

        [Obsolete("This property has been deprecated. Please use Acl instead.")]
        public AccessControlList ACL
        {
            get
            {
                return Acl;
            }
            set
            {
                Acl = value;
            }
        }

        [Obsolete("This property has been deprecated. Please use Kmd instead.")]
        public KinveyMetaData KMD
        {
            get
            {
                return Kmd;
            }
            set
            {
                Kmd = value;
            }
        }
    }
}
