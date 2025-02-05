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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Kinvey
{
    public class KinveyClientRequestInitializer : IKinveyRequestInitializer
    {
        /// <summary>
        /// The app key.
        /// </summary>
        private readonly string appKey;
        /// <summary>
        /// The app secret.
        /// </summary>
        private readonly string appSecret;

        /// <summary>
        /// The credential to use to authenticate the request
        /// </summary>
        private Credential credential;

        /// <summary>
        /// the kinvey headers
        /// </summary>
        private readonly KinveyHeaders headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinveyXamarin.KinveyClientRequestInitializer"/> class.
        /// </summary>
        /// <param name="appKey">App key.</param>
        /// <param name="appSecret">App secret.</param>
        /// <param name="headers">Headers.</param>
        public KinveyClientRequestInitializer(string appKey, string appSecret, KinveyHeaders headers) : this(appKey, appSecret, headers, default(Credential)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinveyXamarin.KinveyClientRequestInitializer"/> class.
        /// </summary>
        /// <param name="appKey">App key.</param>
        /// <param name="appSecret">App secret.</param>
        /// <param name="headers">Headers.</param>
        /// <param name="credential">Credential.</param>
        public KinveyClientRequestInitializer(string appKey, string appSecret, KinveyHeaders headers, Credential credential)
        {
            this.appKey = appKey;
            this.appSecret = appSecret;
            this.headers = headers;
            this.credential = credential;
        }

        /// <summary>
        /// Gets the app key.
        /// </summary>
        /// <value>The app key.</value>
        public string AppKey
        {
            get { return appKey; }
        }

        /// <summary>
        /// Gets the app secret.
        /// </summary>
        /// <value>The app secret.</value>
        public string AppSecret
        {
            get { return appSecret; }
        }

        public string AuthServiceID
        {
            get; private set;
        }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public KinveyHeaders Headers
        {
            get { return headers; }
        }

        /// <summary>
        /// Sets the kinvey credential.
        /// </summary>
        /// <value>The kinvey credential.</value>
        public Credential KinveyCredential
        {
            set { this.credential = value; }
        }

        /// <summary>
        /// Initialize the specified request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <typeparam name="T">The response type of the request.</typeparam>

        public void Initialize<T>(AbstractKinveyClientRequest<T> request, string clientId = null)
        {
            AuthServiceID = clientId ?? AppKey;

            if (!request.RequireAppCredentials)
            {
                if (credential == null ||
                    credential.UserId == null ||
                    credential.AuthToken == null)
                {
                    throw new KinveyException(EnumErrorCategory.ERROR_USER, EnumErrorCode.ERROR_USER_NO_ACTIVE, "");
                }
            }

            if (credential != null && !request.RequireAppCredentials)
            {
                credential.Initialize(request);
            }

            if (request.RequireAppCredentials)
            {
                request.RequestAuth = new HttpBasicAuthenticator(AuthServiceID, AppSecret);
            }

            request.AppKey = appKey;

            foreach (var header in Headers)
            {
                request.RequestHeaders.Add(header);
            }

        }

    }

    public class HttpBasicAuthenticator : IAuthenticator
    {

        private readonly string username;
        private readonly string password;
        private readonly string base64;

        public HttpBasicAuthenticator(string username, string password)
        {
            this.username = username;
            this.password = password;
            var bytes = Encoding.UTF8.GetBytes($"{username}:{password}");
            base64 = Convert.ToBase64String(bytes);
        }

        public void Authenticate(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }
    }
}
