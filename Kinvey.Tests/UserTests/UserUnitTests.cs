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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Kinvey;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;

namespace Kinvey.Tests
{
    [TestClass]
    public class UserUnitTests
    {
        [TestMethod]
        public async Task TestMICLoginAutomatedAuthFlowBad()
        {
            // Arrange
            var moqRestClient = new Mock<HttpClientHandler>();
            var moqResponse = new HttpResponseMessage();

            JObject moqResponseContent = new JObject
            {
                { "error", "MOCK RESPONSE ERROR" },
                { "description", "Mock Gaetway Timeout error" },
                { "debug", "Mock debug" }
            };
            moqResponse.Content = new StringContent(JsonConvert.SerializeObject(moqResponseContent));

            moqResponse.StatusCode = System.Net.HttpStatusCode.GatewayTimeout; // Status Code - 504

            moqRestClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(moqResponse)
                .Verifiable();

            Client.Builder cb = new Client.Builder(TestSetup.app_key, TestSetup.app_secret)
                .setFilePath(TestSetup.db_dir)
                .SetRestClient(new HttpClient(moqRestClient.Object));

            Client c = cb.Build();
            c.MICApiVersion = "v2";

            string username = "testuser";
            string password = "testpass";
            string redirectURI = "kinveyAuthDemo://";

            // Act
            // Assert
            Exception er = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate ()
            {
                await User.LoginWithAuthorizationCodeAPIAsync(username, password, redirectURI, c);
            });

            Assert.IsNotNull(er);
            KinveyException ke = er as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, ke.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, ke.ErrorCode);
            Assert.AreEqual(504, ke.StatusCode); // HttpStatusCode.GatewayTimeout
        }

        [TestMethod]
        public async Task TestMICValidateAuthServiceID()
        {
            // Arrange
            Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);
            Client client = builder.Build();
            string appKey = ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey;
            string micID = "12345";
            string expectedClientId = TestSetup.app_key + "." + micID;

            // Act

            // Test AuthServiceID after setting a clientId
            var requestWithClientID = User.GetMICToken(client, "fake_code", appKey + Constants.CHAR_PERIOD + micID);
            string clientId = ((KinveyClientRequestInitializer)client.RequestInitializer).AuthServiceID;

            // Test to verify that initializing a request other than `/oauth/token` will
            // reset the AuthServiceID back to the default, which is AppKey.
            var req = User.BuildMICTempURLRequest(client, null);
            string shouldBeDefaultClientId = ((KinveyClientRequestInitializer)client.RequestInitializer).AuthServiceID;

            // Assert
            Assert.IsTrue(clientId == expectedClientId);
            Assert.IsTrue(shouldBeDefaultClientId == appKey);
        }

        [TestMethod]
        public async Task TestMICRenderURLScopeID()
        {
            // Arrange
            var builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);
            var client = builder.Build();
            var autoEvent = new System.Threading.AutoResetEvent(false);
            string urlToTestForScopeID = String.Empty;

            var micDelegate = new KinveyMICDelegate<User>()
            {
                onError = (user) => { },
                onSuccess = (error) => { },
                onReadyToRender = (url) =>
                {
                    urlToTestForScopeID = url;
                    autoEvent.Set();
                }
            };

            // Act
            User.LoginWithMIC("mytestredirectURI", micDelegate);

            bool signal = autoEvent.WaitOne(5000);

            // Assert
            Assert.IsTrue(signal);
            Assert.IsFalse(urlToTestForScopeID.Equals(string.Empty));
            Assert.IsTrue(urlToTestForScopeID.Contains("scope=openid"));
        }
    }
}