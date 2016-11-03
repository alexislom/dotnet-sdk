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
using Android.Accounts;
using Android.Content;
using Newtonsoft.Json;

namespace KinveyXamarin
{
	/// <summary>
	/// Android native credential store.
	/// </summary>
	public class AndroidNativeCredentialStore : NativeCredentialStore
	{
		private Android.App.Activity appActivity;
		private Context appContext;
		private AccountManager accountManager;

		#region NativeStoreCredential implementation

		/// <summary>
		/// Initializes a new instance of the <see cref="T:KinveyXamarin.AndroidNativeCredentialStore"/> class.
		/// </summary>
		/// <param name="context">App Context.</param>
		public AndroidNativeCredentialStore(Android.App.Activity activity)
		{
			appActivity = activity;
			appContext = appActivity.ApplicationContext;
			accountManager = AccountManager.Get(appContext);
		}

		/// <summary>
		/// Load the specified userID.
		/// </summary>
		/// <param name="userID">User identifier.</param>
		override public Credential Load(string userID, string ssoGroupKey)
		{
			Credential credential = null;

			try
			{
				NativeCredential nc = null;

				var credentials = FindCredentialsForOrg(ssoGroupKey);

				foreach (var c in credentials)
				{
					if (userID.Equals(string.Empty) ||
						userID.Equals(c.UserID))
					{
						nc = c;
						break;
					}
				}

				if (nc != null)
				{
					credential = Credential.From(nc);
				}
			}
			catch (System.Exception e)
			{
				string msg = e.Message;
			}

			return credential;
		}

		/// <summary>
		/// Store the specified userID and credential.
		/// </summary>
		/// <param name="userID">User identifier.</param>
		/// <param name="credential">Credential.</param>
		override public void Store(string userID, string ssoGroupKey, Credential credential)
		{
			Dictionary<string, string> properties = new Dictionary<string, string>();
			properties.Add(Constants.STR_ACCESS_TOKEN, (credential.AccessToken ?? string.Empty));
			properties.Add(Constants.STR_AUTH_TOKEN, (credential.AuthToken ?? string.Empty));
			properties.Add(Constants.STR_REFRESH_TOKEN, (credential.RefreshToken ?? string.Empty));
			properties.Add(Constants.STR_REDIRECT_URI, (credential.RedirectUri ?? string.Empty));
			properties.Add(Constants.STR_USERNAME, (credential.UserName ?? string.Empty));

			properties.Add(Constants.STR_ATTRIBUTES, (credential.Attributes != null ?
										  JsonConvert.SerializeObject(credential.Attributes) :
										  string.Empty));

			properties.Add(Constants.STR_USER_KMD, (credential.UserKMD != null ?
									   JsonConvert.SerializeObject(credential.UserKMD) :
									   string.Empty));


			NativeCredential nc = new NativeCredential(userID, properties);

			try
			{
				SaveNativeCredential(nc, ssoGroupKey);
			}
			catch (System.Exception e)
			{
				string msg = e.Message;
			}
		}

		/// <summary>
		/// Delete the specified userID.
		/// </summary>
		/// <param name="userID">User identifier.</param>
		override public void Delete(string userID, string ssoGroupKey)
		{
			var nativeCredEnumeration = FindCredentialsForOrg(ssoGroupKey);
			foreach (var nc in nativeCredEnumeration)
			{
				if (nc.UserID.Equals(userID))
				{
					Account[] accounts = accountManager.GetAccounts();

					foreach (var account in accounts)
					{
						if (account.Type.Equals(ssoGroupKey))
						{
							accountManager.RemoveAccount(account, null, null);
						}
					}
					break;
				}
			}
		}

		/// <summary>
		/// Gets the active user.
		/// </summary>
		/// <returns>The active user.</returns>
		override public Credential GetStoredCredential(string ssoGroupKey)
		{
			return Load(string.Empty, ssoGroupKey);
		}

		#endregion

		#region Helper methods

		private IEnumerable<NativeCredential> FindCredentialsForOrg(string ssoGroupKey)
		{
			List<NativeCredential> credentials = new List<NativeCredential>();

			Account[] accounts = accountManager.GetAccounts();

			foreach (var account in accounts)
			{
				if (account.Type.Equals(ssoGroupKey))
				{
					credentials.Add(GetCredentialFromAccount(account));
				}
			}

			return credentials;
		}

		private void SaveNativeCredential(NativeCredential nativeCredential, string ssoGroupKey)
		{
			var serializedCredential = nativeCredential.Serialize();

			// If there exists a credential, delete before writing new credential
			var existingCredential = FindCredential(nativeCredential.UserID, ssoGroupKey);
			if (existingCredential != null)
			{
				accountManager.RemoveAccount(new Account(existingCredential.UserID, ssoGroupKey), null, null);

			}

			// Add new credential
			Account account = new Account(serializedCredential, ssoGroupKey);
			Android.OS.Bundle bundle = new Android.OS.Bundle();
			bundle.PutCharArray("kinvey", serializedCredential.ToCharArray());

			//int uid = Android.OS.Binder.CallingUid;
			//var future = accountManager.AddAccount(ssoGroupKey,"kinveyMIC", null, null, this.appActivity, null, null);
			//System.Threading.Tasks.Task<Java.Lang.Object> res = future.GetResultAsync(10, Java.Util.Concurrent.TimeUnit.Seconds);
			//var future = accountManager.AddAccount(ssoGroupKey, "kinvey", null, bundle, null, null, null);


			bool isSaved = accountManager.AddAccountExplicitly(account, "", bundle);
		}

		private NativeCredential FindCredential(string username, string ssoGroupKey)
		{
			NativeCredential nc = null;

			Account[] accounts = accountManager.GetAccounts();

			foreach (var account in accounts)
			{
				if (account.Type.Equals(ssoGroupKey))
				{
					nc = GetCredentialFromAccount(account);
				}
			}

			return nc;
		}

		private NativeCredential GetCredentialFromAccount(Account account)
		{
			var serializedNativeCredential = account.Name;
			return NativeCredential.Deserialize(serializedNativeCredential);
		}

		#endregion
	}

	/// <summary>
	/// Kinvey account authenticator.
	/// </summary>
	public class KinveyAccountAuthenticator : AbstractAccountAuthenticator
	{
		Context ctx;

		public KinveyAccountAuthenticator(Context context)
			: base(context)
		{
			ctx = context;
		}

		/// <summary>
		/// Adds the account.
		/// </summary>
		/// <returns>The account.</returns>
		/// <param name="response">Response.</param>
		/// <param name="accountType">Account type.</param>
		/// <param name="authTokenType">Auth token type.</param>
		/// <param name="requiredFeatures">Required features.</param>
		/// <param name="options">Options.</param>
		public override Android.OS.Bundle AddAccount(AccountAuthenticatorResponse response, string accountType, string authTokenType, string[] requiredFeatures, Android.OS.Bundle options)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Gets the auth token.
		/// </summary>
		/// <returns>The auth token.</returns>
		/// <param name="response">Response.</param>
		/// <param name="account">Account.</param>
		/// <param name="authTokenType">Auth token type.</param>
		/// <param name="options">Options.</param>
		public override Android.OS.Bundle GetAuthToken(AccountAuthenticatorResponse response, Account account, string authTokenType, Android.OS.Bundle options)
		{
			throw new System.NotImplementedException();
		}






		public override Android.OS.Bundle HasFeatures(AccountAuthenticatorResponse response, Account account, string[] features)
		{
			throw new System.NotImplementedException();
		}
		public override Android.OS.Bundle EditProperties(AccountAuthenticatorResponse response, string accountType)
		{
			throw new System.NotImplementedException();
		}
		public override Android.OS.Bundle ConfirmCredentials(AccountAuthenticatorResponse response, Account account, Android.OS.Bundle options)
		{
			throw new System.NotImplementedException();
		}
		public override Android.OS.Bundle UpdateCredentials(AccountAuthenticatorResponse response, Account account, string authTokenType, Android.OS.Bundle options)
		{
			throw new System.NotImplementedException();
		}
		public override string GetAuthTokenLabel(string authTokenType)
		{
			throw new System.NotImplementedException();
		}
	}
}
