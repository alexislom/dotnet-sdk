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
using Newtonsoft.Json;

namespace Kinvey
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ThirdPartyIdentity
	{
		[JsonProperty("_socialIdentity")]
		public Provider provider { get; set; }

		public ThirdPartyIdentity (Provider provider)
		{
			this.provider = provider;
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class Provider
	{
		[JsonProperty("facebook")]
		public AccessToken facebook { get; set; }

		[JsonProperty("google")]
		public AccessToken google { get; set; }

		[JsonProperty("twitter")]
		public AccessToken twitter { get; set; }

		[JsonProperty("linkedin")]
		public AccessToken linkedin { get; set; }

		[JsonProperty("authlink")]
		public AccessToken authlink { get; set; }

		[JsonProperty("salesforce")]
		public AccessToken salesforce { get; set; }

		[JsonProperty("kinveyAuth")]
		public AccessToken kinveyAuth { get; set; }
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class FacebookCredential : OAuth2
	{
		public FacebookCredential(string accessToken) : base(accessToken) {}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class GoogleCredential : OAuth2
	{
		public GoogleCredential(string accesstoken) : base(accesstoken) {}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class TwitterCredential : OAuth1
	{
		public TwitterCredential(string accesstoken, string accesstokensecret, string consumerkey, string consumersecret) : base(accesstoken, accesstokensecret, consumerkey, consumersecret) {}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class LinkedInCredential : OAuth1
	{
		public LinkedInCredential(string accesstoken, string accesstokensecret, string consumerkey, string consumersecret) : base(accesstoken, accesstokensecret, consumerkey, consumersecret) {}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class AuthLinkCredential : OAuth2
	{
		public AuthLinkCredential(string accesstoken, string refreshtoken) : base(accesstoken, refreshtoken) {}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class SalesforceCredential : OAuth2
	{
		[JsonProperty]
		public string client_id { get; set; }

		[JsonProperty]
		public string id { get; set; }

		public SalesforceCredential(string access, string reauth, string clientid, string id) : base (access, reauth)
		{
			this.client_id = clientid;
			this.id = id;
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class MICCredential : OAuth2
	{
		public MICCredential(string accessToken) : base(accessToken) {}

	}

	[JsonObject(MemberSerialization.OptIn)]
	public class OAuth2 : AccessToken
	{
		[JsonProperty("refresh_token")]
		private string refreshToken { get; set; }

		public OAuth2(string accessToken) : base(accessToken) {}

		public OAuth2(string accessToken, string refreshtoken) : base(accessToken)
		{
			this.refreshToken = refreshtoken;
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class OAuth1 : AccessToken
	{
		[JsonProperty("access_token_secret")]
		protected string accessTokenSecret { get; set; }

		[JsonProperty("consumer_key")]
		protected string consumerKey { get; set; }

		[JsonProperty("consumer_secret")]
		protected string consumerSecret { get; set; }

		public OAuth1(string accessToken) : base(accessToken) {}

		public OAuth1(string accessToken, string accesstokensecret, string consumerkey, string consumersecret) : base(accessToken)
		{
			this.accessTokenSecret = accesstokensecret;
			this.consumerKey = consumerkey;
			this.consumerSecret = consumersecret;
		}
	}

	[JsonObject(MemberSerialization.OptIn)]
	public class AccessToken
	{
		[JsonProperty("access_token")]
		public string accessToken { get; set; }

		public AccessToken(string access)
		{
			this.accessToken = access;
		}
	}
}
