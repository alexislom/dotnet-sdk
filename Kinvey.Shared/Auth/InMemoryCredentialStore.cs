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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinvey
{
	/// <summary>
	/// In memory credential store.
	/// </summary>
    public class InMemoryCredentialStore : ICredentialStore
    {
		/// <summary>
		/// The store.
		/// </summary>
        private Dictionary<string, Credential> store = new Dictionary<string, Credential>();

		/// <summary>
		/// Load the specified userId.
		/// </summary>
		/// <param name="userId">User._id.</param>
		/// <param name="ssoGroupKey">SSO Group Key.</param>
		public Credential Load(string userId, string ssoGroupKey)
        {
            return store.ContainsKey(userId) ? store[userId] : null;
        }

        /// <summary>
        /// Store the specified userId and credential.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="ssoGroupKey">SSO Group Key.</param>
        /// <param name="credential">Credential.</param>
        public void Store(string userId, string ssoGroupKey, Credential credential)
        {
            if (userId != null)
            {
				Credential cred = new Credential(userId, credential.AccessToken, credential.AuthSocialID, credential.AuthToken, credential.UserName, credential.Attributes, credential.UserKMD, credential.RefreshToken, credential.RedirectUri, credential.DeviceID, credential.MICClientID);
                store.Add(userId, cred);
            }
        }

		/// <summary>
		/// Delete the specified userId.
		/// </summary>
		/// <param name="userId">User identifier.</param>
		/// <param name="ssoGroupKey">SSO Group Key.</param>
		public void Delete(string userId, string ssoGroupKey)
        {
            if (userId != null)
            {
                store.Remove(userId);
            }
        }

		public Credential GetStoredCredential (string ssoGroupKey){
			return store.FirstOrDefault ().Value;
		}

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.


                // set large fields to null.
                lock (store)
                {
                    if (store != null)
                    {
                        store.Clear();
                        store = null;
                    }
                }

                disposedValue = true;
            }
        }

        ~InMemoryCredentialStore() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
