﻿// Copyright (c) 2019, Kinvey, Inc. All rights reserved.
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
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Threading;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Kinvey.Tests
{
    [TestClass]
    public class DataStoreNetworkIntegrationTests : BaseTestClass
	{
		private Client kinveyClient;

        [TestInitialize]
        public override void Setup()
		{
            try
            {
                if (kinveyClient != null)
                {
                    using (var client = kinveyClient)
                    {
                        var user = client.ActiveUser;
                        if (user != null)
                        {
                            user.Logout();
                        }
                    }
                }
            }
            finally
            {
                kinveyClient = null;
            }

            base.Setup();

            System.IO.File.Delete(TestSetup.SQLiteOfflineStoreFilePath);
            System.IO.File.Delete(TestSetup.SQLiteCredentialStoreFilePath);
		}

        [TestCleanup]
        public override void Tear()
		{
            try
            {
                if (kinveyClient != null)
                {
                    using (var client = kinveyClient)
                    {
                        var user = client.ActiveUser;
                        if (user != null)
                        {
                            user.Logout();
                        }
                    }
                }
            }
            finally
            {
                kinveyClient = null;
            }

            base.Tear();

			System.IO.File.Delete(TestSetup.SQLiteOfflineStoreFilePath);
			System.IO.File.Delete(TestSetup.SQLiteCredentialStoreFilePath);
		}

        #region ACL settings

        [TestMethod]
        public async Task TestACLGloballyReadableSave()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            AccessControlList acl = new AccessControlList();
            acl.GloballyReadable = true;
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);
            ToDo todo = new ToDo();
            todo.Acl = acl;

            // Act
            ToDo savedToDo = await todoStore.SaveAsync(todo);

            // Assert
            Assert.AreEqual(true, acl.GloballyReadable);
            Assert.AreEqual(false, acl.GloballyWriteable);
            Assert.AreNotEqual(todo.Acl, savedToDo.Acl);
            Assert.AreNotEqual(todo.Acl.Creator, savedToDo.Acl.Creator);
            Assert.AreEqual(todo.Acl.GloballyReadable, savedToDo.Acl.GloballyReadable);
            Assert.AreEqual(todo.Acl.GloballyWriteable, savedToDo.Acl.GloballyWriteable);

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);
            kinveyClient.ActiveUser.Logout();
        }

        [TestMethod]
        public async Task TestACLGloballyWriteableSave()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            AccessControlList acl = new AccessControlList();
            acl.GloballyWriteable = true;
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);
            ToDo todo = new ToDo();
            todo.Acl = acl;

            // Act
            ToDo savedToDo = await todoStore.SaveAsync(todo);

            // Assert
            Assert.AreEqual(false, acl.GloballyReadable);
            Assert.AreEqual(true, acl.GloballyWriteable);
            Assert.AreNotEqual(todo.Acl, savedToDo.Acl);
            Assert.AreNotEqual(todo.Acl.Creator, savedToDo.Acl.Creator);
            Assert.AreEqual(todo.Acl.GloballyReadable, savedToDo.Acl.GloballyReadable);
            Assert.AreEqual(todo.Acl.GloballyWriteable, savedToDo.Acl.GloballyWriteable);

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);
            kinveyClient.ActiveUser.Logout();
        }

        [TestMethod]
        public async Task TestACLGroupReadListSave()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            AccessControlList acl = new AccessControlList();
            acl.Groups.Readers.Add("groupread1");
            acl.Groups.Readers.Add("groupread2");
            acl.Groups.Readers.Add("groupread3");
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);
            ToDo todo = new ToDo();
            todo.Acl = acl;

            // Act
            ToDo savedToDo = await todoStore.SaveAsync(todo);

            // Assert
            Assert.AreEqual(false, acl.GloballyReadable);
            Assert.AreEqual(false, acl.GloballyWriteable);
            Assert.AreNotEqual(todo.Acl, savedToDo.Acl);
            Assert.AreNotEqual(todo.Acl.Creator, savedToDo.Acl.Creator);
            Assert.AreEqual(todo.Acl.GloballyReadable, savedToDo.Acl.GloballyReadable);
            Assert.AreEqual(todo.Acl.GloballyWriteable, savedToDo.Acl.GloballyWriteable);
            Assert.IsNotNull(savedToDo.Acl.Readers);
            Assert.IsNotNull(savedToDo.Acl.Writers);
            Assert.IsNotNull(savedToDo.Acl.Groups);
            Assert.IsNotNull(savedToDo.Acl.Groups.Writers);
            Assert.IsNotNull(savedToDo.Acl.Groups.Readers);
            CollectionAssert.AreEqual(todo.Acl.Groups.Readers, savedToDo.Acl.Groups.Readers);
            Assert.AreEqual(3, savedToDo.Acl.Groups.Readers.Count);
            Assert.IsTrue(savedToDo.Acl.Groups.Readers[0].Equals("groupread1"));
            Assert.IsTrue(savedToDo.Acl.Groups.Readers[1].Equals("groupread2"));
            Assert.IsTrue(savedToDo.Acl.Groups.Readers[2].Equals("groupread3"));

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);
            kinveyClient.ActiveUser.Logout();
        }

        [TestMethod]
        public async Task TestACLGroupWriteListSave()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            AccessControlList acl = new AccessControlList();
            acl.Groups.Writers.Add("groupwrite1");
            acl.Groups.Writers.Add("groupwrite2");
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);
            ToDo todo = new ToDo();
            todo.Acl = acl;

            // Act
            ToDo savedToDo = await todoStore.SaveAsync(todo);

            // Assert
            Assert.AreEqual(false, acl.GloballyReadable);
            Assert.AreEqual(false, acl.GloballyWriteable);
            Assert.AreNotEqual(todo.Acl, savedToDo.Acl);
            Assert.AreNotEqual(todo.Acl.Creator, savedToDo.Acl.Creator);
            Assert.AreEqual(todo.Acl.GloballyReadable, savedToDo.Acl.GloballyReadable);
            Assert.AreEqual(todo.Acl.GloballyWriteable, savedToDo.Acl.GloballyWriteable);
            Assert.IsNotNull(savedToDo.Acl.Readers);
            Assert.IsNotNull(savedToDo.Acl.Writers);
            Assert.IsNotNull(savedToDo.Acl.Groups);
            Assert.IsNotNull(savedToDo.Acl.Groups.Readers);
            Assert.IsNotNull(savedToDo.Acl.Groups.Writers);
            CollectionAssert.AreEqual(todo.Acl.Groups.Writers, savedToDo.Acl.Groups.Writers);
            Assert.AreEqual(2, savedToDo.Acl.Groups.Writers.Count);
            Assert.IsTrue(savedToDo.Acl.Groups.Writers[0].Equals("groupwrite1"));
            Assert.IsTrue(savedToDo.Acl.Groups.Writers[1].Equals("groupwrite2"));

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);
            kinveyClient.ActiveUser.Logout();
        }

        [TestMethod]
        public async Task TestACLReadListSave()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            AccessControlList acl = new AccessControlList();
            acl.Readers.Add("reader1");
            acl.Readers.Add("reader2");
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);
            ToDo todo = new ToDo();
            todo.Acl = acl;

            // Act
            ToDo savedToDo = await todoStore.SaveAsync(todo);

            // Assert
            Assert.AreEqual(false, acl.GloballyReadable);
            Assert.AreEqual(false, acl.GloballyWriteable);
            Assert.AreNotEqual(todo.Acl, savedToDo.Acl);
            Assert.AreNotEqual(todo.Acl.Creator, savedToDo.Acl.Creator);
            Assert.AreEqual(todo.Acl.GloballyReadable, savedToDo.Acl.GloballyReadable);
            Assert.AreEqual(todo.Acl.GloballyWriteable, savedToDo.Acl.GloballyWriteable);
            Assert.IsNotNull(savedToDo.Acl.Writers);
            Assert.IsNotNull(savedToDo.Acl.Readers);
            CollectionAssert.AreEqual(todo.Acl.Readers, savedToDo.Acl.Readers);
            Assert.AreEqual(2, savedToDo.Acl.Readers.Count);
            Assert.IsTrue(savedToDo.Acl.Readers[0].Equals("reader1"));
            Assert.IsTrue(savedToDo.Acl.Readers[1].Equals("reader2"));

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);
            kinveyClient.ActiveUser.Logout();
        }

        [TestMethod]
        public async Task TestACLWriteListSave()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            AccessControlList acl = new AccessControlList();
            acl.Writers.Add("writer1");
            acl.Writers.Add("writer2");
            acl.Writers.Add("writer3");
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);
            ToDo todo = new ToDo();
            todo.Acl = acl;

            // Act
            ToDo savedToDo = await todoStore.SaveAsync(todo);

            // Assert
            Assert.AreEqual(false, acl.GloballyReadable);
            Assert.AreEqual(false, acl.GloballyWriteable);
            Assert.AreNotEqual(todo.Acl, savedToDo.Acl);
            Assert.AreNotEqual(todo.Acl.Creator, savedToDo.Acl.Creator);
            Assert.AreEqual(todo.Acl.GloballyReadable, savedToDo.Acl.GloballyReadable);
            Assert.AreEqual(todo.Acl.GloballyWriteable, savedToDo.Acl.GloballyWriteable);
            Assert.IsNotNull(savedToDo.Acl.Readers);
            Assert.IsNotNull(savedToDo.Acl.Writers);
            CollectionAssert.AreEqual(todo.Acl.Writers, savedToDo.Acl.Writers);
            Assert.AreEqual(3, savedToDo.Acl.Writers.Count);
            Assert.IsTrue(savedToDo.Acl.Writers[0].Equals("writer1"));
            Assert.IsTrue(savedToDo.Acl.Writers[1].Equals("writer2"));
            Assert.IsTrue(savedToDo.Acl.Writers[2].Equals("writer3"));

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);
            kinveyClient.ActiveUser.Logout();
        }

        #endregion ACL settings

        #region Client settings

        [TestMethod]
        public async Task TestCollectionSharedClient()
        {
            // Arrange
            kinveyClient = BuildClient();

            // Act
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Assert
            Assert.IsNotNull(todoStore);
            Assert.IsTrue(string.Equals(todoStore.CollectionName, toDosCollection));
        }
        
        [TestMethod]
		public async Task TestCollectionStoreType()
		{
            // Arrange
            kinveyClient = BuildClient();

            // Act
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

			// Assert
			Assert.IsNotNull(todoStore);
			Assert.AreEqual(todoStore.CollectionName, toDosCollection);
			Assert.AreEqual(todoStore.StoreType, DataStoreType.NETWORK);

		}

        #endregion Client settings

        #region DeltaSetFetching

        [TestMethod]
        public void TestDeltaSetFetchEnable()
        {
            // Arrange
            kinveyClient = BuildClient();

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            todoStore.DeltaSetFetchingEnabled = true;

            // Assert
            Assert.IsTrue(todoStore.DeltaSetFetchingEnabled);
        }

        #endregion DeltaSetFetching

        #region Pull, Push, Sync

        [TestMethod]
        public async Task TestStoreInvalidOperation()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(1);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            await Assert.ThrowsExceptionAsync<KinveyException>(async delegate ()
            {
                await todoStore.PullAsync();
            });

            await Assert.ThrowsExceptionAsync<KinveyException>(async delegate ()
            {
                await todoStore.PushAsync();
            });

            await Assert.ThrowsExceptionAsync<KinveyException>(async delegate ()
            {
                await todoStore.SyncAsync();
            });
        }

        [TestMethod]
        public async Task TestThrowJsonExceptionInBuildGetAsyncRequest()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            var anotherNewItem = new ToDo
            {
                Name = "Another Next Task",
                Details = "Another test",
                DueDate = "2016-05-19T20:02:17.635Z"
            };

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            var savedItem = await todoStore.SaveAsync(newItem);

            var savedAnotherItem = await todoStore.SaveAsync(anotherNewItem);

            // Arrange
            var buildGetRequest = kinveyClient.NetworkFactory.buildGetRequest<int>(toDosCollection);

            // Act
            var actualAsyncResult = await Assert.ThrowsExceptionAsync<KinveyException>(async () => await buildGetRequest.ExecuteAsync());

            //Teardown
            await todoStore.RemoveAsync(savedItem.ID);

            await todoStore.RemoveAsync(savedAnotherItem.ID);

            // Assert
            Assert.IsNotNull(actualAsyncResult);
            Assert.IsNotNull(actualAsyncResult.Message);
            Assert.AreEqual($"Received Array for API call {kinveyClient.BaseUrl}appdata/{AppKey}/ToDos, but expected System.Collections.Generic.List`1[System.Int32]", actualAsyncResult.Message);
            Assert.AreEqual(EnumErrorCategory.ERROR_DATASTORE_NETWORK, actualAsyncResult.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_PARSE, actualAsyncResult.ErrorCode);
        }

        #endregion Pull, Push, Sync

        #region Delete

        #region Single delete

        #region Positive

        [TestMethod]
        public async Task TestDeleteAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            ToDo newItem = new ToDo
            {
                Name = "Task to Delete",
                Details = "A delete test"
            };
            ToDo deleteToDo = await todoStore.SaveAsync(newItem);

            // Act
            KinveyDeleteResponse kdr = await todoStore.RemoveAsync(deleteToDo.ID);

            // Assert
            Assert.IsNotNull(kdr);
            Assert.AreEqual(1, kdr.count);
        }

        #endregion

        #region Negative

        [TestMethod]
        public async Task TestDelete400ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.RemoveAsync(TestSetup.id_for_400_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(400, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestDelete401ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem = new ToDo
            {
                Name = "Task to Delete",
                Details = "A delete test"
            };
            var savedToDo = await todoStore.SaveAsync(newItem);

            kinveyClient.ActiveUser.Logout();


             await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStore.RemoveAsync(savedToDo.ID);
            });

            kinveyClient.ActiveUser.Logout();

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            await todoStore.RemoveAsync(savedToDo.ID);

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestDelete403ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.RemoveAsync(TestSetup.id_for_403_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(403, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestDelete404ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStore.RemoveAsync(Guid.NewGuid().ToString());
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(404, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestDelete409ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.RemoveAsync(TestSetup.id_for_409_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(409, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestDelete500ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.RemoveAsync(TestSetup.id_for_500_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(500, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestErrorJsonResponseInDeleteSyncRequestThrowJsonException()
        {
            if (!MockData)
                return;

            // Setup
            kinveyClient = BuildClient();

            // Arrange
            MockResponses(3);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            var savedItem = await todoStore.SaveAsync(newItem);

            // Arrange
            var deleteRequest = kinveyClient.NetworkFactory.buildDeleteRequest<int>(toDosCollection, savedItem.ID);

            // Act
            var actualSyncResult = Assert.ThrowsException<KinveyException>(() => deleteRequest.Execute());

            // Assert
            Assert.IsNotNull(actualSyncResult);
            Assert.IsNotNull(actualSyncResult.Message);
            Assert.AreEqual($"Received Object for API call http://localhost:8080/appdata/_kid_/ToDos/{savedItem.ID}, but expected System.Int32", actualSyncResult.Message);
            Assert.AreEqual(EnumErrorCategory.ERROR_DATASTORE_NETWORK, actualSyncResult.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_PARSE, actualSyncResult.ErrorCode);
        }

        [TestMethod]
        public async Task TestThrowInvalidCastExceptionInBuildDeleteRequest()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            var savedItem = await todoStore.SaveAsync(newItem);

            // Arrange
            var buildDeleteRequest = kinveyClient.NetworkFactory.buildDeleteRequest<JArray>(toDosCollection, savedItem.ID);

            // Act
            var actualAsyncResult = await Assert.ThrowsExceptionAsync<KinveyException>(async () => await buildDeleteRequest.ExecuteAsync());

            // Assert
            Assert.IsNotNull(actualAsyncResult);
            Assert.IsNotNull(actualAsyncResult.Message);
            Assert.AreEqual($"Received Object for API call {kinveyClient.BaseUrl}appdata/{AppKey}/ToDos/{savedItem.ID}, but expected Newtonsoft.Json.Linq.JArray", actualAsyncResult.Message);
            Assert.AreEqual(EnumErrorCategory.ERROR_DATASTORE_NETWORK, actualAsyncResult.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_PARSE, actualAsyncResult.ErrorCode);
        }

        #endregion Negative

        #endregion Single delete

        #region Delete by query

        #region Positive      

        [TestMethod]
        public async Task TestDeleteByQueryStringValueStartsWithExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 to delete",
                Details = "Delete details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Delete details2"
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Not delete details3"
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details.StartsWith("Delet"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem3.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNotNull(existingItem3);           
        }

        [TestMethod]
        public async Task TestDeleteByQueryStringValueWithPlusSymbolEqualExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 to delete",
                Details = "+Delete"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "+Delete"
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Not delete details3"
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details.Equals("+Delete"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem3.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNotNull(existingItem3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryOrExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",                
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                BoolVal = true
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(e => e.Name == "Task2 to delete" || e.BoolVal == true);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);        
        }

        [TestMethod]
        public async Task TestDeleteByQueryBoolValueExplicitEqualsExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                BoolVal = true
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.BoolVal.Equals(true));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);       
        }

        [TestMethod]
        public async Task TestDeleteByQueryBoolValueImplicitEqualExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                BoolVal = true
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.BoolVal);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueGreaterThanExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2017, 4, 20, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 21, 19, 56, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var endDate = new DateTime(2018, 1, 1, 0, 0, 0);

            var query = todoStore.Where(x => x.NewDate > endDate);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);        
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueGreaterThanOrEqualExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2017, 4, 20, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 1, 1, 0, 0, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var endDate = new DateTime(2018, 1, 1, 0, 0, 0);

            var query = todoStore.Where(x => x.NewDate >= endDate);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);            
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueLessThanExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 1, 1, 0, 0, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);

            var query = todoStore.Where(x => x.NewDate < endDate);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueLessThanOrEqualExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);

            var query = todoStore.Where(x => x.NewDate <= endDate);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueGreaterThanExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 1
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 2
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 3
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Value > 1);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);          
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueGreaterThanOrEqualExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 1
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 2
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 3
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);

            var query = todoStore.Where(x => x.Value >= 2);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueLessThanExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 2
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);

            var query = todoStore.Where(x => x.Value < 2);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueLessThanOrEqualExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 2
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Value <= 2);

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);          
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueEqualsExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Value.Equals(1));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);            
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalAndExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task to delete",
                Details = "Details3",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(todo => todo.Details.StartsWith("Deta") && todo.Name.Equals("Task to delete"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalAndWithOrExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "TaskDel2",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(todo => todo.DueDate.Equals("2018-04-22T19:56:00.963Z") && (todo.Name.StartsWith("TaskDel") ||
                        todo.Details.Equals("Details for")));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);        
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalOrExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(todo => todo.Name.StartsWith("Task Del") || todo.Details.Equals("Details for"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);           
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalOrWithAndExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(9);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(todo => todo.Name.StartsWith("Task Del") || todo.DueDate.Equals("2018-04-22T19:56:00.963Z") && todo.Details.Equals("Details for"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesStartsWithAndEqualsExpressionsAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task Del3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details.StartsWith("Details f")).Where(y => y.Name.StartsWith("Task D")).Where(z => z.DueDate.Equals("2018-04-22T19:56:00.963Z"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);
            await todoStore.RemoveAsync(existingItem2.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNotNull(existingItem2);
            Assert.IsNull(existingItem3);           
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesEqualsExpressionsAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Test",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task Test",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details.Equals("Details for")).Where(y => y.Name.Equals("Task Test")).Where(z => z.DueDate.Equals("2018-04-22T19:56:00.963Z"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);
            await todoStore.RemoveAsync(existingItem2.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNotNull(existingItem2);
            Assert.IsNull(existingItem3);           
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesDifferentEqualsExpressionsAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details == ("Details for")).Where(y => y.BoolVal == true).Where(z => z.DueDate.Equals("2018-04-22T19:56:00.963Z"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);
            await todoStore.RemoveAsync(existingItem2.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNotNull(existingItem2);
            Assert.IsNull(existingItem3);          
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesFluentSyntaxEqualExpressionsAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = from t in todoStore where t.Details == "Details for" where t.Name == "Task Del" where t.DueDate == "2018-04-22T19:56:00.963Z" select t;

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);
            await todoStore.RemoveAsync(existingItem2.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNotNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }


        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesWithLogicalAndExpressionsAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task Delete",
                Details = "Details for",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Not Delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details.StartsWith("Details f") && x.DueDate.Equals("2018-04-22T19:56:00.963Z")).Where(y => y.Name.StartsWith("Task Del"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);
            await todoStore.RemoveAsync(existingItem2.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNotNull(existingItem2);
            Assert.IsNull(existingItem3);         
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesWithLogicalOrExpressionsAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task Del1",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Not Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Details.StartsWith("Details f") || x.DueDate.Equals("2018-04-22T19:56:00.963Z")).Where(y => y.Name.StartsWith("Task D"));

            // Act
            var kinveyDeleteResponse = await todoStore.RemoveAsync(query);

            ToDo existingItem1 = null;
            ToDo existingItem2 = null;
            ToDo existingItem3 = null;

            try
            {
                existingItem1 = await todoStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem2 = await todoStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItem3 = await todoStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await todoStore.RemoveAsync(existingItem1.ID);
            await todoStore.RemoveAsync(existingItem2.ID);

            // Assert
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItem1);
            Assert.IsNotNull(existingItem2);
            Assert.IsNull(existingItem3);          
        }
     
        #endregion Positive   

        #region Negative

        [TestMethod]
        public async Task TestDeleteByQueryWhereClauseIsAbsentInQueryUsingSelectClauseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Select(x => x.Details);
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });


            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_WHERE_CLAUSE_IS_ABSENT_IN_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryWhereClauseIsAbsentInQueryUsingOrderClauseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.OrderBy(x => x.Details);
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });


            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_WHERE_CLAUSE_IS_ABSENT_IN_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryWhereClauseIsAbsentInQueryUsingTakeClauseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Take(1);
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });


            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_WHERE_CLAUSE_IS_ABSENT_IN_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryNullQueryAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query: null);
            });


            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_NULL_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryNotSupportedExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del11",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del22",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => true);
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });


            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_DATASTORE_NETWORK);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_LINQ_WHERE_CLAUSE_NOT_SUPPORTED);
        }

        [TestMethod]
        public async Task TestDeleteByQueryNotSupportedStringExpressionAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                BoolVal = false,
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete support",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete support",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => x.Name.Contains("support"));
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });


            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_DATASTORE_NETWORK);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_LINQ_WHERE_CLAUSE_NOT_SUPPORTED);
        }

        [TestMethod]
        public async Task TestDeleteByQuery401ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }

            await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            var query = todoStore.Where(x => x.Details.Equals("Test"));
        
            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStore.RemoveAsync(query);
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        #endregion Negative

        #endregion Delete by query

        #endregion Delete

        #region GetCount

        #region Positive tests

        [TestMethod]
        public async Task TestGetCountAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem = new ToDo();
            newItem.Name = "Next Task";
            newItem.Details = "A test";
            newItem.DueDate = "2016-04-19T20:02:17.635Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";


            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            ToDo t1 = await todoStore.SaveAsync(newItem);
            ToDo t2 = await todoStore.SaveAsync(newItem2);

            // Act
            var count = 0u;
            count = await todoStore.GetCountAsync();

            // Assert
            Assert.AreEqual(2u, count);

            // Teardown
            await todoStore.RemoveAsync(t1.ID);
            await todoStore.RemoveAsync(t2.ID);
        }

        [TestMethod]
        public async Task TestGetCountAsyncWithQuery()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2+",
                DueDate = "2016-04-22T19:56:00.963Z"
            };


            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var t1 = await todoStore.SaveAsync(newItem);
            var t2 = await todoStore.SaveAsync(newItem2);

            // Act
            var count = 0u;
            var query = todoStore.Where(e => e.Details.Equals("details for 2+"));
            count = await todoStore.GetCountAsync(query);

            // Teardown
            await todoStore.RemoveAsync(t1.ID);
            await todoStore.RemoveAsync(t2.ID);

            // Assert
            Assert.AreEqual(1u, count);
        }

        #endregion Positive tests

        #region Negative tests

        [TestMethod]
        public async Task TestGetCount400ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                if (MockData)
                {
                    MockResponses(2);
                }

                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                // Arrange
                var todoStore = DataStore<BadRequestErrorEntity>.Collection(badRequestErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.GetCountAsync();
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(400, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestGetCount401ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }

            await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStore.GetCountAsync();
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestGetCount403ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                // Arrange
                var todoStore = DataStore<ForbiddenErrorEntity>.Collection(forbiddenErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.GetCountAsync();
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(403, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestGetCount404ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                // Arrange
                var todoStore = DataStore<NotFoundErrorEntity>.Collection(notFoundErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.GetCountAsync();
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(404, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestGetCount409ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                // Arrange
                var todoStore = DataStore<ConflictErrorEntity>.Collection(conflictErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.GetCountAsync();
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(409, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestGetCount500ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                // Arrange
                var todoStore = DataStore<InternalServerErrorEntity>.Collection(internalServerErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.GetCountAsync();
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(500, kinveyException.StatusCode);
            }
        }

        #endregion Negative tests
       
        #endregion GetCount

        #region Find

        #region Positive tests

        [TestMethod]
        public async Task TestNetworkStoreFindAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem = new ToDo();
            newItem.Name = "Next Task";
            newItem.Details = "A test";
            newItem.DueDate = "2016-04-19T20:02:17.635Z";
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            ToDo t = await todoStore.SaveAsync(newItem);

            ToDo anotherNewItem = new ToDo();
            anotherNewItem.Name = "Another Next Task";
            anotherNewItem.Details = "Another test";
            anotherNewItem.DueDate = "2016-05-19T20:02:17.635Z";
            ToDo t2 = await todoStore.SaveAsync(anotherNewItem);

            // Act
            List<ToDo> todoList = new List<ToDo>();

            todoList = await todoStore.FindAsync();

            // Assert
            Assert.IsNotNull(todoList);
            Assert.AreEqual(2, todoList.Count);

            // Teardown
            await todoStore.RemoveAsync(t.ID);
            await todoStore.RemoveAsync(t2.ID);
            kinveyClient.ActiveUser.Logout();
        }

        [TestMethod]
		public async Task TestNetworkStoreFindAsyncBad()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(1);
            }
			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var moqRC = new Mock<HttpClientHandler>(MockBehavior.Strict);
            HttpRequestMessage request = null;
            moqRC
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((req, token) => request = req)
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("MOCK RESPONSE"),
                    RequestMessage = request
                })
                .Verifiable();

			Client.Builder cb = new Client.Builder(TestSetup.app_key, TestSetup.app_secret)
				.SetFilePath(TestSetup.db_dir)
                .SetRestClient(new HttpClient(moqRC.Object));

            using (var c = cb.Build())
            {
                // Arrange
                DataStore<ToDo> store = DataStore<ToDo>.Collection("todos", DataStoreType.NETWORK, c);

                // Act
                // Assert
                Exception er = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate ()
                {
                    await store.FindAsync();
                });

                Assert.IsNotNull(er);
                KinveyException ke = er as KinveyException;
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_PARSE, ke.ErrorCode);

                // Teardown
                c.ActiveUser.Logout();
            }
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByIDAsync()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(4);
            }
			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem = new ToDo();
			newItem.Name = "Next Task";
			newItem.Details = "A test";
			newItem.DueDate = "2016-04-19T20:02:17.635Z";
			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
			ToDo t = await todoStore.SaveAsync(newItem);

			// Act
			ToDo entity = null;
			entity = await todoStore.FindByIDAsync(t.ID);

			// Assert
			Assert.IsNotNull(entity);
			Assert.IsTrue(string.Equals(entity.ID, t.ID));

			// Teardown
			await todoStore.RemoveAsync(t.ID);
			kinveyClient.ActiveUser.Logout();
		}

        [TestMethod]
        public async Task TestNetworkStoreFindByMongoQuery()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            string mongoQuery = "{\"details\":\"details for 2\"}";
            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindWithMongoQueryAsync(mongoQuery);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
            Assert.IsNotNull(listToDo[0].Name);
            Assert.IsNotNull(listToDo[0].Details);
            Assert.IsTrue(listToDo[0].Details.Equals("details for 2"));
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByMongoQueryWithPlusSymbol()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            var newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2+";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var mongoQuery = "{\"details\":\"details for 2+\"}";
            var listToDo = new List<ToDo>();

            listToDo = await todoStore.FindWithMongoQueryAsync(mongoQuery);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
            Assert.IsNotNull(listToDo[0].Name);
            Assert.IsNotNull(listToDo[0].Details);
            Assert.IsTrue(listToDo[0].Details.Equals("details for 2+"));
        }

        [TestMethod]
		public async Task TestNetworkStoreFindByQuery()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            ToDo newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);

			// Act
			//var query = from todo in todoStore
			//			where todo.Details.StartsWith("deta")
			//	                      where todo.Name.StartsWith("todo a")
			//			select todo;

			//var query = from todo in todoStore
			//			where todo.Details.StartsWith("deta") && todo.Name.StartsWith("todo a")
			//			select todo;

			//var query = todoStore.Where(x => x.Details.StartsWith("det")).Where(y => y.Name.StartsWith("anoth"));

			var query = todoStore.Where(x => x.Details.StartsWith("det"));

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.AreNotEqual(listToDo.Count, 0);
			Assert.AreEqual(2, listToDo.Count);
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryBoolValueExplicit()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";
			newItem2.BoolVal = true;

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

			var query = from e in todoStore
						where e.Name == "another todo" || e.BoolVal == true
						select e;

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);

			//var query = todoStore.Where(x => x.BoolVal.Equals(true));

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(1, listToDo.Count);
		}

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryBoolValueExplicitEqualsExpression()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";
            newItem2.BoolVal = true;

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            var query = todoStore.Where(x => x.BoolVal.Equals(true));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryBoolValueImplicit()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";
            newItem2.BoolVal = true;

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

            var query = from e in todoStore
                        where e.BoolVal
                        select e;

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryInequalityDateTimeObjectGreaterThan()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";
            newItem1.NewDate = new DateTime(2016, 4, 21, 19, 56, 0);

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";
            newItem2.NewDate = new DateTime(2016, 4, 22, 19, 56, 0);

            ToDo newItem3 = new ToDo();
            newItem3.Name = "another todo";
            newItem3.Details = "details for 2";
            newItem3.DueDate = "2016-04-22T19:56:00.963Z";
            newItem3.NewDate = new DateTime(2017, 4, 22, 19, 56, 0);

            var endDate = new DateTime(2017, 1, 1, 0, 0, 0);
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);
            newItem3 = await todoStore.SaveAsync(newItem3);

            // Act
            var query = todoStore.Where(x => x.NewDate > endDate);

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            await todoStore.RemoveAsync(newItem3.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.AreNotEqual(listToDo.Count, 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryInequalityDateTimeObjectGreaterThanOrEqual()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";
            newItem1.NewDate = new DateTime(2016, 4, 21, 19, 56, 0);

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";
            newItem2.NewDate = new DateTime(2016, 4, 22, 19, 56, 0);

            ToDo newItem3 = new ToDo();
            newItem3.Name = "another todo";
            newItem3.Details = "details for 2";
            newItem3.DueDate = "2016-04-22T19:56:00.963Z";
            newItem3.NewDate = new DateTime(2017, 1, 1, 0, 0, 0);

            var endDate = new DateTime(2017, 1, 1, 0, 0, 0);
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);
            newItem3 = await todoStore.SaveAsync(newItem3);

            // Act
            var query = todoStore.Where(x => x.NewDate >= endDate);

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            await todoStore.RemoveAsync(newItem3.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.AreNotEqual(listToDo.Count, 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryInequalityDateTimeObjectLessThan()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";
            newItem1.NewDate = new DateTime(2016, 4, 21, 19, 56, 0);

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";
            newItem2.NewDate = new DateTime(2016, 4, 22, 19, 56, 0);

            ToDo newItem3 = new ToDo();
            newItem3.Name = "another todo";
            newItem3.Details = "details for 2";
            newItem3.DueDate = "2016-04-22T19:56:00.963Z";
            newItem3.NewDate = new DateTime(2017, 1, 1, 0, 0, 1);

            var endDate = new DateTime(2017, 1, 1, 0, 0, 0);
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);
            newItem3 = await todoStore.SaveAsync(newItem3);

            // Act
            var query = todoStore.Where(x => x.NewDate < endDate);

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            await todoStore.RemoveAsync(newItem3.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(2, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryInequalityDateTimeObjectLessThanOrEqual()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z",
                NewDate = new DateTime(2016, 4, 21, 19, 56, 0)
            };

            var newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z",
                NewDate = new DateTime(2016, 4, 22, 19, 56, 0)
            };

            var newItem3 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z",
                NewDate = new DateTime(2017, 1, 1, 0, 0, 0)
            };

            var endDate = new DateTime(2017, 1, 1, 0, 0, 0);
            var todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);
            newItem3 = await todoStore.SaveAsync(newItem3);

            // Act
            var query = todoStore.Where(x => x.NewDate <= endDate);

            var listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            await todoStore.RemoveAsync(newItem3.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(3, listToDo.Count);
        }

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryInequalityGreaterThan()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";
			newItem1.Value = 1;

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";
			newItem2.Value = 2;

			ToDo newItem3 = new ToDo();
			newItem3.Name = "another todo";
			newItem3.Details = "details for 2";
			newItem3.DueDate = "2016-04-22T19:56:00.963Z";
			newItem3.Value = 2;

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);
			newItem3 = await todoStore.SaveAsync(newItem3);

			// Act
			var query = todoStore.Where(x => x.Value > 1);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			await todoStore.RemoveAsync(newItem3.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(2, listToDo.Count);
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryInequalityGreaterThanOrEqual()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";
			newItem1.Value = 1;

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";
			newItem2.Value = 2;

			ToDo newItem3 = new ToDo();
			newItem3.Name = "another todo";
			newItem3.Details = "details for 2";
			newItem3.DueDate = "2016-04-22T19:56:00.963Z";
			newItem3.Value = 2;

			//var endDate = new DateTime(2017, 1, 1, 0, 0, 0);
			//string end_date = "2016-04-22T19:56:00.963Z";
			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);
			newItem3 = await todoStore.SaveAsync(newItem3);

			// Act
			var query = todoStore.Where(x => x.Value >= 2);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			await todoStore.RemoveAsync(newItem3.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(2, listToDo.Count);
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryInequalityLessThan()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";
			newItem1.Value = 1;

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";
			newItem2.Value = 2;

			ToDo newItem3 = new ToDo();
			newItem3.Name = "another todo";
			newItem3.Details = "details for 2";
			newItem3.DueDate = "2016-04-22T19:56:00.963Z";
			newItem3.Value = 2;

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);
			newItem3 = await todoStore.SaveAsync(newItem3);

			// Act
			var query = todoStore.Where(x => x.Value < 2);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			await todoStore.RemoveAsync(newItem3.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(1, listToDo.Count);
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryInequalityLessThanOrEqual()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";
			newItem1.Value = 1;

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";
			newItem2.Value = 2;

			ToDo newItem3 = new ToDo();
			newItem3.Name = "another todo";
			newItem3.Details = "details for 2";
			newItem3.DueDate = "2016-04-22T19:56:00.963Z";
			newItem3.Value = 3;

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);
			newItem3 = await todoStore.SaveAsync(newItem3);

			// Act
			var query = todoStore.Where(x => x.Value <= 2);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			await todoStore.RemoveAsync(newItem3.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(2, listToDo.Count);
		}

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryIntValue()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";
            newItem2.Value = 1;

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            var query = todoStore.Where(x => x.Value.Equals(1));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryLogicalAnd()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where todo.Details.StartsWith("deta") && todo.Name.Equals("todo")
                        select todo;

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryLogicalAndWithOr()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "a todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "b todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where todo.DueDate.Equals("2016-04-22T19:56:00.963Z") && (todo.Name.StartsWith("a to") || todo.Details.Equals("details for 2"))
                        select todo;

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(2, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryLogicalOr()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "a todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "b todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where todo.Name.StartsWith("a to") || todo.Details.Equals("details for 2")
                        select todo;

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(2, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryLogicalOrWithAnd()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "a todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "b todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where (todo.Name.StartsWith("b to") || todo.DueDate.Equals("2016-04-22T19:56:00.963Z") && todo.Details.Equals("details for 2"))
                        select todo;

            var expectedResults =
                        from todo in new ToDo[] { newItem1, newItem2 }
                        where (todo.Name.StartsWith("b to") || todo.DueDate.Equals("2016-04-22T19:56:00.963Z") && todo.Details.Equals("details for 2"))
                        select todo;

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(expectedResults.Count(), listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryMultipleWhereClauses()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = todoStore.Where(x => x.Details.StartsWith("det")).Where(y => y.Name.StartsWith("todo a")).Where(z => z.DueDate.Equals("2016-04-22T19:56:00.963Z"));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryMultipleWhereClausesEquals()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = todoStore.Where(x => x.Details.Equals("details for 2")).Where(y => y.Name.Equals("todo again")).Where(z => z.DueDate.Equals("2016-04-22T19:56:00.963Z"));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryMultipleWhereClausesEqualSign()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = todoStore.Where(x => x.Details == ("details for 2")).Where(y => y.Name == ("todo again")).Where(z => z.DueDate.Equals("2016-04-22T19:56:00.963Z"));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryMultipleWhereClausesFluentSyntaxEqualSign()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from t in todoStore where t.Details == "details for 2" where t.Name == "todo again" where t.DueDate == "2016-04-22T19:56:00.963Z" select t;

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryMultipleWhereClausesWithLogicalAnd()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = todoStore.Where(x => x.Details.StartsWith("det") && x.DueDate.Equals("2016-04-22T19:56:00.963Z")).Where(y => y.Name.StartsWith("todo a"));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryMultipleWhereClausesWithLogicalOr()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "todo again";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = todoStore.Where(x => x.Details.StartsWith("det") || x.DueDate.Equals("2016-04-22T19:56:00.963Z")).Where(y => y.Name.StartsWith("todo a"));

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);


            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(1, listToDo.Count);
        }

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryNotSupported()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(5);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);

			var query = todoStore.Where(x => true);

			List<ToDo> listToDo = new List<ToDo>();

			// Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
			{
				listToDo = await todoStore.FindAsync(query);
			});

			// Assert
			Assert.IsTrue(e.GetType() == typeof(KinveyException));
			KinveyException ke = e as KinveyException;
			Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_DATASTORE_NETWORK);
			Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_LINQ_WHERE_CLAUSE_NOT_SUPPORTED);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			kinveyClient.ActiveUser.Logout();
		}

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryWithLimit()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = todoStore.Where(x => x.Details.StartsWith("det")).Take(1);

            var expectedResults = new ToDo[] { newItem1, newItem2 }.Where(x => x.Details.StartsWith("det")).Take(1);

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(expectedResults.Count(), listToDo.Count);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryWithSelectField()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(7);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            foreach (var item in await todoStore.FindAsync())
            {
                await todoStore.RemoveAsync(item.ID);
            }

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where todo.Details.StartsWith("deta")
                        select todo.Name;

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(2, listToDo.Count);
            Assert.IsNotNull(listToDo[0].Name);
            Assert.IsNull(listToDo[0].Details);
            Assert.IsNotNull(listToDo[1].Name);
            Assert.IsNull(listToDo[1].Details);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryWithSelectFields()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where todo.Details.StartsWith("deta")
                        select new { todo.Name, todo.Details };

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(2, listToDo.Count);
            Assert.IsNotNull(listToDo[0].Name);
            Assert.IsNotNull(listToDo[0].Details);
            Assert.IsNull(listToDo[0].DueDate);
            Assert.IsNotNull(listToDo[1].Name);
            Assert.IsNotNull(listToDo[1].Details);
            Assert.IsNull(listToDo[1].DueDate);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByQueryWithSelectFieldsBad()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
            if (kinveyClient.ActiveUser != null)
            {
                kinveyClient.ActiveUser.Logout();
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            ToDo newItem1 = new ToDo();
            newItem1.Name = "todo";
            newItem1.Details = "details for 1";
            newItem1.DueDate = "2016-04-22T19:56:00.963Z";

            ToDo newItem2 = new ToDo();
            newItem2.Name = "another todo";
            newItem2.Details = "details for 2";
            newItem2.DueDate = "2016-04-22T19:56:00.963Z";

            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            newItem1 = await todoStore.SaveAsync(newItem1);
            newItem2 = await todoStore.SaveAsync(newItem2);

            // Act
            var query = from todo in todoStore
                        where todo.Details.StartsWith("deta")
                        select new { };

            List<ToDo> listToDo = new List<ToDo>();

            listToDo = await todoStore.FindAsync(query);

            // Teardown
            await todoStore.RemoveAsync(newItem1.ID);
            await todoStore.RemoveAsync(newItem2.ID);
            kinveyClient.ActiveUser.Logout();

            // Assert
            Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
            Assert.AreEqual(2, listToDo.Count);
            Assert.IsNotNull(listToDo[0].Name);
            Assert.IsNotNull(listToDo[0].Details);
            Assert.IsNotNull(listToDo[0].DueDate);
            Assert.IsNotNull(listToDo[1].Name);
            Assert.IsNotNull(listToDo[1].Details);
            Assert.IsNotNull(listToDo[1].DueDate);
        }

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryWithSkip()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);

			// Act
			var query = todoStore.Where(x => x.Details.StartsWith("det")).Skip(1);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
			Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(1, listToDo.Count);
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryWithSortAscending()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);

			// Act
			var query = todoStore.Where(x => x.Details.StartsWith("det")).OrderBy(x => x.Name);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
            Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(2, listToDo.Count);
			Assert.IsTrue(String.Compare(newItem2.Name, listToDo.First().Name) == 0);
		}

        [TestMethod]
		public async Task TestNetworkStoreFindByQueryWithSortDescending()
		{
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(6);
            }
			if (kinveyClient.ActiveUser != null)
			{
				kinveyClient.ActiveUser.Logout();
			}

			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			// Arrange
			ToDo newItem1 = new ToDo();
			newItem1.Name = "todo";
			newItem1.Details = "details for 1";
			newItem1.DueDate = "2016-04-22T19:56:00.963Z";

			ToDo newItem2 = new ToDo();
			newItem2.Name = "another todo";
			newItem2.Details = "details for 2";
			newItem2.DueDate = "2016-04-22T19:56:00.963Z";

			DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

			newItem1 = await todoStore.SaveAsync(newItem1);
			newItem2 = await todoStore.SaveAsync(newItem2);

			// Act
			var query = todoStore.Where(x => x.Details.StartsWith("det")).OrderByDescending(x => x.Name);

			List<ToDo> listToDo = new List<ToDo>();

			listToDo = await todoStore.FindAsync(query);

			// Teardown
			await todoStore.RemoveAsync(newItem1.ID);
			await todoStore.RemoveAsync(newItem2.ID);
			kinveyClient.ActiveUser.Logout();

			// Assert
			Assert.IsNotNull(listToDo);
			Assert.IsTrue(listToDo.Count > 0);
			Assert.AreEqual(2, listToDo.Count);
			Assert.IsTrue(String.Compare(newItem1.Name, listToDo.First().Name) == 0);
		}

        #endregion Positive tests

        #region Negative tests

        [TestMethod]
        public async Task TestNetworkStoreFindByID400ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.FindByIDAsync(TestSetup.id_for_400_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(400, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByID401ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStore.FindByIDAsync(Guid.NewGuid().ToString());
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByID403ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.FindByIDAsync(TestSetup.id_for_403_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(403, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByID404ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStore.FindByIDAsync(Guid.NewGuid().ToString());
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(404, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByID409ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.FindByIDAsync(TestSetup.id_for_409_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(409, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreFindByID500ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.FindByIDAsync(TestSetup.id_for_500_error_response_fake);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(500, kinveyException.StatusCode);
            }
        }

        #endregion Negative tests

        #endregion Find

        #region GroupAndAggregate

        #region Positive tests

        [TestMethod]
        public async Task TestNetworkStoreGetAverageAsync()
        {
            //Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(10);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            DataStore<Person> personStore = DataStore<Person>.Collection(personCollection, DataStoreType.NETWORK);

            Person p1 = new Person();
            p1.FirstName = "Michael";
            p1.LastName = "Bluth";
            p1.Age = 40;
            p1 = await personStore.SaveAsync(p1);

            Person p2 = new Person();
            p2.FirstName = "George Michael";
            p2.LastName = "Bluth";
            p2.Age = 15;
            p2 = await personStore.SaveAsync(p2);

            Person p3 = new Person();
            p3.FirstName = "Tobias";
            p3.LastName = "Funke";
            p3.Age = 46;
            p3 = await personStore.SaveAsync(p3);

            Person p4 = new Person();
            p4.FirstName = "Buster";
            p4.LastName = "Bluth";
            p4.Age = 19;
            p4 = await personStore.SaveAsync(p4);

            // Act
            int avg = 0;
            List<GroupAggregationResults> arrGAR = await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "Age");
            foreach (var gar in arrGAR)
            {
                avg = gar.Result;
                break;
            }

            // Teardown
            await personStore.RemoveAsync(p4.ID);
            await personStore.RemoveAsync(p3.ID);
            await personStore.RemoveAsync(p2.ID);
            await personStore.RemoveAsync(p1.ID);

            // Assert
            Assert.AreNotEqual(0, avg);
            Assert.AreEqual(30, avg);
            Assert.AreEqual(1, arrGAR.Count());
        }

        [TestMethod]
        public async Task TestNetworkStoreGetMaxAsync()
        {
            //Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            DataStore<Person> personStore = DataStore<Person>.Collection(personCollection, DataStoreType.NETWORK);

            Person p1 = new Person();
            p1.FirstName = "Michael";
            p1.LastName = "Bluth";
            p1.Age = 40;
            p1 = await personStore.SaveAsync(p1);

            Person p2 = new Person();
            p2.FirstName = "George Michael";
            p2.LastName = "Bluth";
            p2.Age = 15;
            p2 = await personStore.SaveAsync(p2);

            Person p3 = new Person();
            p3.FirstName = "Tobias";
            p3.LastName = "Funke";
            p3.Age = 46;
            p3 = await personStore.SaveAsync(p3);

            // Act
            int max = 0;
            List<GroupAggregationResults> arrGAR = await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_MAX, "", "Age");
            foreach (var gar in arrGAR)
            {
                max = gar.Result;
                break;
            }

            // Teardown
            await personStore.RemoveAsync(p3.ID);
            await personStore.RemoveAsync(p2.ID);
            await personStore.RemoveAsync(p1.ID);

            // Assert
            Assert.AreNotEqual(0, max);
            Assert.AreEqual(46, max);
            Assert.AreEqual(1, arrGAR.Count());
        }

        [TestMethod]
		public async Task TestNetworkStoreGetMinAsync()
		{
            //Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }

			// Arrange
			await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

			DataStore<Person> personStore = DataStore<Person>.Collection(personCollection, DataStoreType.NETWORK);

			Person p1 = new Person();
			p1.FirstName = "Michael";
			p1.LastName = "Bluth";
			p1.Age = 40;
			p1 = await personStore.SaveAsync(p1);

			Person p2 = new Person();
			p2.FirstName = "George Michael";
			p2.LastName = "Bluth";
			p2.Age = 15;
			p2 = await personStore.SaveAsync(p2);

			Person p3 = new Person();
			p3.FirstName = "Tobias";
			p3.LastName = "Funke";
			p3.Age = 46;
			p3 = await personStore.SaveAsync(p3);

			// Act
			int min = 0;
			List<GroupAggregationResults> arrGAR = await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_MIN, "", "Age");
			foreach (var gar in arrGAR)
			{
				min = gar.Result;
				break;
			}

			// Teardown
			await personStore.RemoveAsync(p3.ID);
			await personStore.RemoveAsync(p2.ID);
			await personStore.RemoveAsync(p1.ID);

			// Assert
			Assert.AreNotEqual(0, min);
			Assert.AreEqual(15, min);
			Assert.AreEqual(1, arrGAR.Count());
		}

        [TestMethod]
        public async Task TestNetworkStoreGetSumAsync()
        {
            //Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(8);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            DataStore<Person> personStore = DataStore<Person>.Collection(personCollection, DataStoreType.NETWORK);

            Person p1 = new Person();
            p1.FirstName = "Michael";
            p1.LastName = "Bluth";
            p1.Age = 40;
            p1 = await personStore.SaveAsync(p1);

            Person p2 = new Person();
            p2.FirstName = "George Michael";
            p2.LastName = "Bluth";
            p2.Age = 15;
            p2 = await personStore.SaveAsync(p2);

            Person p3 = new Person();
            p3.FirstName = "Tobias";
            p3.LastName = "Funke";
            p3.Age = 46;
            p3 = await personStore.SaveAsync(p3);

            var query = personStore.Where(x => x.LastName.Equals("Bluth"));

            // Act
            int sum = 0;
            List<GroupAggregationResults> arrGAR = await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_SUM, "LastName", "Age", query);
            foreach (var gar in arrGAR)
            {
                if (gar.GroupField.Equals("Bluth"))
                {
                    sum = gar.Result;
                    break;
                }
            }

            // Teardown
            await personStore.RemoveAsync(p3.ID);
            await personStore.RemoveAsync(p2.ID);
            await personStore.RemoveAsync(p1.ID);

            // Assert
            Assert.AreNotEqual(0, sum);
            Assert.AreEqual(55, sum);
            Assert.AreEqual(1, arrGAR.Count());
        }

        #endregion Positive tests

        #region Negative tests 

        [TestMethod]
        public async Task TestNetworkStoreGetAverage400ErrorResponseAsync()
        {
            //Setup
            if (MockData)
            {
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var personStore = DataStore<BadRequestErrorEntity>.Collection(badRequestErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "Name");
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(400, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreGetAverage401ErrorResponseAsync()
        {
            //Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }

            // Arrange
            await User.LoginAsync(TestSetup.pass_for_user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            var store = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await store.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "Name");
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestNetworkStoreGetAverage403ErrorResponseAsync()
        {
            if (MockData)
            {
                //Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var personStore = DataStore<ForbiddenErrorEntity>.Collection(forbiddenErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "FirstName");
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(403, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreGetAverage404ErrorResponseAsync()
        {
            if (MockData)
            {
                //Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var personStore = DataStore<NotFoundErrorEntity>.Collection(notFoundErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "FirstName");
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(404, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreGetAverage409ErrorResponseAsync()
        {
            if (MockData)
            {
                //Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var personStore = DataStore<ConflictErrorEntity>.Collection(conflictErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "FirstName");
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(409, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestNetworkStoreGetAverage500ErrorResponseAsync()
        {
            if (MockData)
            {
                //Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var personStore = DataStore<InternalServerErrorEntity>.Collection(internalServerErrorEntityCollection, DataStoreType.NETWORK);

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await personStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "FirstName");
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(500, kinveyException.StatusCode);
            }
        }

        #endregion Negative tests

        #endregion GroupAndAggregate

        #region Save

        #region Single insert

        #region Positive tests

        [TestMethod]
        public async Task TestSaveAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var savedToDo = await todoStore.SaveAsync(newItem);

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);

            // Assert
            Assert.IsNotNull(savedToDo);
            Assert.AreEqual(savedToDo.Name, newItem.Name);
        }

        [TestMethod]
        public async Task TestSaveCreateApiVersion4Async()
        {
            // Setup
            kinveyClient = BuildClient("4");

            if (MockData)
            {
                MockResponses(4);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var savedToDo = await todoStore.SaveAsync(newItem);

            var existingToDos = await todoStore.FindAsync();

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);

            // Assert
            Assert.IsNotNull(existingToDos);
            Assert.AreEqual(1, existingToDos.Count);
            Assert.AreEqual(newItem.Name, existingToDos[0].Name);
            Assert.AreEqual(newItem.Details, existingToDos[0].Details);
            Assert.AreEqual(newItem.DueDate, existingToDos[0].DueDate);
        }

        [TestMethod]
        public async Task TestSaveUpdateApiVersion4Async()
        {
            // Setup
            kinveyClient = BuildClient("4");

            if (MockData)
            {
                MockResponses(4);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo
            {
                ID = Guid.NewGuid().ToString(),
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var savedToDo = await todoStore.SaveAsync(newItem);

            var existingToDos = await todoStore.FindAsync();

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);

            // Assert
            Assert.IsNotNull(existingToDos);
            Assert.AreEqual(1, existingToDos.Count);
            Assert.AreEqual(newItem.ID, existingToDos[0].ID);
            Assert.AreEqual(newItem.Name, existingToDos[0].Name);
            Assert.AreEqual(newItem.Details, existingToDos[0].Details);
            Assert.AreEqual(newItem.DueDate, existingToDos[0].DueDate);
        }

        [TestMethod]
        public async Task TestSaveCreateApiVersion5Async()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(4);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var savedToDo = await todoStore.SaveAsync(newItem);

            var existingToDo = await todoStore.FindByIDAsync(savedToDo.ID);

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);

            // Assert
            Assert.IsNotNull(existingToDo);
            Assert.AreEqual(newItem.Name, existingToDo.Name);
            Assert.AreEqual(newItem.Details, existingToDo.Details);
            Assert.AreEqual(newItem.DueDate, existingToDo.DueDate);
        }

        [TestMethod]
        public async Task TestSaveUpdateApiVersion5Async()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(4);
            }

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo
            {
                ID = Guid.NewGuid().ToString(),
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var savedToDo = await todoStore.SaveAsync(newItem);

            var existingToDos = await todoStore.FindAsync();

            // Teardown
            await todoStore.RemoveAsync(savedToDo.ID);

            // Assert
            Assert.IsNotNull(existingToDos);
            Assert.AreEqual(1, existingToDos.Count);
            Assert.AreEqual(newItem.ID, existingToDos[0].ID);
            Assert.AreEqual(newItem.Name, existingToDos[0].Name);
            Assert.AreEqual(newItem.Details, existingToDos[0].Details);
            Assert.AreEqual(newItem.DueDate, existingToDos[0].DueDate);
        }

        #endregion Positive tests

        #region Negative tests

        [TestMethod]
        public async Task TestSave400ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(3);
            }
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Arrange
            var newItem = new ToDo { Name = "Name1", Details = "Details1", Value = 1, GeoLoc = "[200,200]" };

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                var savedToDo = await todoStore.SaveAsync(newItem);
            });

            var toDosNetwork = await todoStore.FindAsync();

            // Assert
            Assert.IsNotNull(toDosNetwork);
            Assert.AreEqual(0, toDosNetwork.Count);

            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(400, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestSave401ErrorResponseAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(2);
            }
            await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            // Arrange
            var newItem = new ToDo { Name = "Name1", Details = "Details1", Value = 1 };

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                var savedToDo = await todoStore.SaveAsync(newItem);
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestSave403ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                var newItem = new ToDo
                {
                    ID = TestSetup.id_for_403_error_response_fake,
                    Name = "Name1",
                    Details = "Details1",
                    Value = 1
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.SaveAsync(newItem);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(403, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestSave404ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                var newItem = new ToDo
                {
                    ID = TestSetup.id_for_404_error_response_fake,
                    Name = "Name1",
                    Details = "Details1",
                    Value = 1
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.SaveAsync(newItem);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(404, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestSave409ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                var newItem = new ToDo
                {
                    ID = TestSetup.id_for_409_error_response_fake,
                    Name = "Name1",
                    Details = "Details1",
                    Value = 1
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.SaveAsync(newItem);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(409, kinveyException.StatusCode);
            }
        }

        [TestMethod]
        public async Task TestSave500ErrorResponseAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);

                var newItem = new ToDo
                {
                    ID = TestSetup.id_for_500_error_response_fake,
                    Name = "Name1",
                    Details = "Details1",
                    Value = 1
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStore.SaveAsync(newItem);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
                Assert.AreEqual(500, kinveyException.StatusCode);
            }
        }

        #endregion Negative tests

        #endregion Single insert

        #region Multi insert

        #region Positive tests

        [TestMethod]
        public async Task TestSaveMultiInsertNewItemsAsync()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(5);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            var toDos = new List<ToDo>
            {
                new ToDo { Name = "Name1", Details = "Details1", Value = 1 },
                new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
            };

            // Act
            var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

            var existingToDos = await todoStoreNetwork.FindAsync();

            // Teardown
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[0].ID);
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[1].ID);

            // Assert
            Assert.AreEqual(2, savedToDos.Entities.Count);
            Assert.AreEqual(0, savedToDos.Errors.Count);
            Assert.AreEqual(toDos[0].Name, savedToDos.Entities[0].Name);
            Assert.AreEqual(toDos[0].Details, savedToDos.Entities[0].Details);
            Assert.AreEqual(toDos[0].Value, savedToDos.Entities[0].Value);
            Assert.AreEqual(toDos[1].Name, savedToDos.Entities[1].Name);
            Assert.AreEqual(toDos[1].Details, savedToDos.Entities[1].Details);
            Assert.AreEqual(toDos[1].Value, savedToDos.Entities[1].Value);
            Assert.IsNotNull(existingToDos);
            Assert.AreEqual(2, existingToDos.Count);
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.Name == toDos[0].Name && e.Details == toDos[0].Details && e.Value == toDos[0].Value));
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.Name == toDos[1].Name && e.Details == toDos[1].Details && e.Value == toDos[1].Value));
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExistingItemsAsync()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(6);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            var toDos = new List<ToDo>
            {
                new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name1", Details = "Details1", Value = 1 },
                new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 }
            };

            // Act
            var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

            var existingToDos = await todoStoreNetwork.FindAsync();

            // Teardown
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[0].ID);
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[1].ID);

            // Assert
            Assert.AreEqual(2, savedToDos.Entities.Count);
            Assert.AreEqual(0, savedToDos.Errors.Count);
            Assert.AreEqual(toDos[0].Name, savedToDos.Entities[0].Name);
            Assert.AreEqual(toDos[0].Details, savedToDos.Entities[0].Details);
            Assert.AreEqual(toDos[0].Value, savedToDos.Entities[0].Value);
            Assert.AreEqual(toDos[1].Name, savedToDos.Entities[1].Name);
            Assert.AreEqual(toDos[1].Details, savedToDos.Entities[1].Details);
            Assert.AreEqual(toDos[1].Value, savedToDos.Entities[1].Value);
            Assert.IsNotNull(existingToDos);
            Assert.AreEqual(2, existingToDos.Count);
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.ID == toDos[0].ID && e.Name == toDos[0].Name && e.Details == toDos[0].Details && e.Value == toDos[0].Value));
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.ID == toDos[1].ID && e.Name == toDos[1].Name && e.Details == toDos[1].Details && e.Value == toDos[1].Value));
        }

        [TestMethod]
        public async Task TestSaveMultiInsertNewItemsExistingItemsAsync()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(10);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            var toDos = new List<ToDo>();

            var toDo1 = new ToDo { Name = "Name1", Details = "Details1", Value = 1 };
            toDos.Add(toDo1);

            var toDo2 = new ToDo { Name = "Name2", Details = "Details2", Value = 2 };
            toDo2 = await todoStoreNetwork.SaveAsync(toDo2);
            toDo2.Name = "Name22";
            toDo2.Details = "Details22";
            toDo2.Value = 22;
            toDos.Add(toDo2);

            var toDo3 = new ToDo { Name = "Name3", Details = "Details3", Value = 3 };
            toDos.Add(toDo3);

            var toDo4 = new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name4", Details = "Details4", Value = 4 };
            toDos.Add(toDo4);

            // Act
            var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

            var existingToDos = await todoStoreNetwork.FindAsync();

            // Teardown
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[0].ID);
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[1].ID);
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[2].ID);
            await todoStoreNetwork.RemoveAsync(savedToDos.Entities[3].ID);

            // Assert
            Assert.AreEqual(4, savedToDos.Entities.Count);
            Assert.AreEqual(0, savedToDos.Errors.Count);
            Assert.AreEqual(toDos[0].Name, savedToDos.Entities[0].Name);
            Assert.AreEqual(toDos[0].Details, savedToDos.Entities[0].Details);
            Assert.AreEqual(toDos[0].Value, savedToDos.Entities[0].Value);
            Assert.AreEqual(toDos[1].ID, savedToDos.Entities[1].ID);
            Assert.AreEqual(toDos[1].Name, savedToDos.Entities[1].Name);
            Assert.AreEqual(toDos[1].Details, savedToDos.Entities[1].Details);
            Assert.AreEqual(toDos[1].Value, savedToDos.Entities[1].Value);
            Assert.AreEqual(toDos[2].Name, savedToDos.Entities[2].Name);
            Assert.AreEqual(toDos[2].Details, savedToDos.Entities[2].Details);
            Assert.AreEqual(toDos[2].Value, savedToDos.Entities[2].Value);
            Assert.AreEqual(toDos[3].ID, savedToDos.Entities[3].ID);
            Assert.AreEqual(toDos[3].Name, savedToDos.Entities[3].Name);
            Assert.AreEqual(toDos[3].Details, savedToDos.Entities[3].Details);
            Assert.AreEqual(toDos[3].Value, savedToDos.Entities[3].Value);
            Assert.IsNotNull(existingToDos);
            Assert.AreEqual(4, existingToDos.Count);
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.Name == toDos[0].Name && e.Details == toDos[0].Details && e.Value == toDos[0].Value));
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.ID == toDos[1].ID && e.Name == toDos[1].Name && e.Details == toDos[1].Details && e.Value == toDos[1].Value));
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.Name == toDos[2].Name && e.Details == toDos[2].Details && e.Value == toDos[2].Value));
            Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.ID == toDos[3].ID && e.Name == toDos[3].Name && e.Details == toDos[3].Details && e.Value == toDos[3].Value));
        }

        [TestMethod]
        public async Task TestSaveMultiInsertNewItemsWithErrorsAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(4);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                new ToDo { Name = "Name1", Details = "Details1", Value = 1 },
                new ToDo { Name = "Name2", Details = "Details2", Value = 2, GeoLoc = "[200,200]" }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                var existingToDos = await todoStoreNetwork.FindAsync();

                // Teardown
                await todoStoreNetwork.RemoveAsync(savedToDos.Entities[0].ID);

                // Assert
                Assert.AreEqual(2, savedToDos.Entities.Count);
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(toDos[0].Name, savedToDos.Entities[0].Name);
                Assert.AreEqual(toDos[0].Details, savedToDos.Entities[0].Details);
                Assert.AreEqual(toDos[0].Value, savedToDos.Entities[0].Value);
                Assert.IsNull(savedToDos.Entities[1]);
                Assert.IsNotNull(existingToDos);
                Assert.AreEqual(1, existingToDos.Count);
                Assert.IsNotNull(existingToDos.FirstOrDefault(e => e.Name == toDos[0].Name && e.Details == toDos[0].Details && e.Value == toDos[0].Value));
                Assert.AreEqual(1, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertWithErrorsGeolocationIssueAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(6);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>();

                var toDo1 = new ToDo { Name = "Name1", Details = "Details1", Value = 1, GeoLoc = "[200,200]" };
                toDos.Add(toDo1);

                var toDo2 = new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 };
                toDos.Add(toDo2);

                var toDo3 = new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name3", Details = "Details3", Value = 3, GeoLoc = "[200,200]" };
                toDos.Add(toDo3);

                var toDo4 = new ToDo { Name = "Name4", Details = "Details4", Value = 4 };
                toDos.Add(toDo4);

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Teardown
                await todoStoreNetwork.RemoveAsync(savedToDos.Entities[1].ID);
                await todoStoreNetwork.RemoveAsync(savedToDos.Entities[3].ID);

                // Assert
                Assert.AreEqual(4, savedToDos.Entities.Count);
                Assert.AreEqual(2, savedToDos.Errors.Count);
                Assert.IsNull(savedToDos.Entities[0]);
                Assert.IsNotNull(savedToDos.Entities[1]);
                Assert.IsNull(savedToDos.Entities[2]);
                Assert.IsNotNull(savedToDos.Entities[3]);
                Assert.AreEqual(toDos[1].ID, savedToDos.Entities[1].ID);
                Assert.AreEqual(toDos[1].Name, savedToDos.Entities[1].Name);
                Assert.AreEqual(toDos[1].Details, savedToDos.Entities[1].Details);
                Assert.AreEqual(toDos[1].Value, savedToDos.Entities[1].Value);
                Assert.AreEqual(toDos[3].Name, savedToDos.Entities[3].Name);
                Assert.AreEqual(toDos[3].Details, savedToDos.Entities[3].Details);
                Assert.AreEqual(toDos[3].Value, savedToDos.Entities[3].Value);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
                Assert.AreEqual(2, savedToDos.Errors[1].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertWithErrorsTestSetupEntityIssueAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(8);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>();

                var toDo1 = new ToDo { Name = "Name3", Details = "Details3", Value = 3 };
                toDo1 = await todoStoreNetwork.SaveAsync(toDo1);
                toDo1.Name = TestSetup.entity_name_for_400_response_error;
                toDo1.Details = "Details33";
                toDo1.Value = 33;

                toDos.Add(toDo1);
                toDos.Add(new ToDo { Name = TestSetup.entity_name_for_400_response_error, Details = "Details1", Value = 1 });
                toDos.Add(toDo1);
                toDos.Add(new ToDo { Name = "Name2", Details = "Details2", Value = 2 });
                toDos.Add(toDo1);
                toDos.Add(new ToDo { Name = TestSetup.entity_name_for_400_response_error, Details = "Details3", Value = 3 });
                toDos.Add(toDo1);

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Teardown
                await todoStoreNetwork.RemoveAsync(savedToDos.Entities[3].ID);

                // Assert
                Assert.AreEqual(7, savedToDos.Entities.Count);
                Assert.AreEqual(6, savedToDos.Errors.Count);
                Assert.IsNull(savedToDos.Entities[0]);
                Assert.IsNull(savedToDos.Entities[1]);
                Assert.IsNull(savedToDos.Entities[2]);
                Assert.IsNotNull(savedToDos.Entities[3]);
                Assert.IsNull(savedToDos.Entities[4]);
                Assert.IsNull(savedToDos.Entities[5]);
                Assert.IsNull(savedToDos.Entities[6]);
                Assert.AreEqual(toDos[3].Name, savedToDos.Entities[3].Name);
                Assert.AreEqual(toDos[3].Details, savedToDos.Entities[3].Details);
                Assert.AreEqual(toDos[3].Value, savedToDos.Entities[3].Value);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
                Assert.AreEqual(1, savedToDos.Errors[1].Index);
                Assert.AreEqual(2, savedToDos.Errors[2].Index);
                Assert.AreEqual(4, savedToDos.Errors[3].Index);
                Assert.AreEqual(5, savedToDos.Errors[4].Index);
                Assert.AreEqual(6, savedToDos.Errors[5].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting400ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(3);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name1", Details = "Details1", Value = 1 },
                    new ToDo { ID = TestSetup.id_for_400_error_response_fake, Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(1, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting403ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { Name = TestSetup.entity_name_for_403_response_error, Details = "Details1", Value = 1 },
                    new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting403ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(3);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_403_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                    new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting404ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { Name = TestSetup.entity_name_for_404_response_error, Details = "Details1", Value = 1 },
                    new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting404ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(3);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_404_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                    new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting409ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { Name = TestSetup.entity_name_for_409_response_error, Details = "Details1", Value = 1 },
                    new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting409ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(3);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_409_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                    new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting500ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { Name = TestSetup.entity_name_for_500_response_error, Details = "Details1", Value = 1 },
                    new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertExisting500ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(3);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_500_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                    new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var savedToDos = await todoStoreNetwork.SaveAsync(toDos);

                // Assert
                Assert.AreEqual(1, savedToDos.Errors.Count);
                Assert.AreEqual(0, savedToDos.Errors[0].Index);
            }
        }

        #endregion Positive tests

        #region Negative tests

        [TestMethod]
        public async Task TestSaveMultiInsertEmptyArrayAsync()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(1);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStoreNetwork.SaveAsync(new List<ToDo>());
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_GENERAL, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_DATASTORE_EMPTY_ARRAY_OF_ENTITIES, kinveyException.ErrorCode);
        }

        [TestMethod]
        public async Task TestSaveMultiInsertInvalidPermissionsForMultiInsertAsync()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(2);
            }

            // Arrange
            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            var toDos = new List<ToDo>
            {
                new ToDo { Name = "Name1", Details = "Details1", Value = 1 },
                new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
            };

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStoreNetwork.SaveAsync(toDos);
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            Assert.AreEqual(401, kinveyException.StatusCode);
        }

        [TestMethod]
        public async Task TestSaveMultiInsertInvalidPermissionsForUpdateAsync()
        {
            // Setup
            kinveyClient = BuildClient("5");

            if (MockData)
            {
                MockResponses(3);
            }

            // Arrange
            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            await User.LoginAsync(TestSetup.user_without_permissions, TestSetup.pass_for_user_without_permissions, kinveyClient);

            var toDos = new List<ToDo>
            {
                new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name1", Details = "Details1", Value = 1 },
                new ToDo { ID = Guid.NewGuid().ToString(), Name = "Name2", Details = "Details2", Value = 2 }
            };

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStoreNetwork.SaveAsync(toDos);
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
        }

        [TestMethod]
        public async Task TestSaveMultiInsertIncorrectKinveyApiVersionAsync()
        {
            // Setup
            kinveyClient = BuildClient("4");

            if (MockData)
            {
                MockResponses(1);
            }

            // Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            var toDos = new List<ToDo>
            {
                new ToDo { Name = "Name1", Details = "Details1", Value = 1 },
                new ToDo { Name = "Name2", Details = "Details2", Value = 2 }
            };

            // Act
            var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                await todoStoreNetwork.SaveAsync(toDos);
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_GENERAL, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_DATASTORE_NOT_COMPATIBLE_KINVEY_API_VERSION, kinveyException.ErrorCode);
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing400ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { Name = TestSetup.entity_name_for_400_response_error, Details = "Details1", Value = 1 },
                    new ToDo { Name = TestSetup.entity_name_for_400_response_error, Details = "Details3", Value = 3 }
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing400ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(3);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_400_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                    new ToDo { ID = TestSetup.id_for_400_error_response_fake, Name = "Name2", Details = "Details2", Value = 2 }
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing403ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo {  Name = TestSetup.entity_name_for_403_response_error, Details = "Details1", Value = 1 },
                    new ToDo {  Name = TestSetup.entity_name_for_403_response_error, Details = "Details1", Value = 2 }
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing403ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_403_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing404ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo {  Name = TestSetup.entity_name_for_404_response_error, Details = "Details1", Value = 1 },
                    new ToDo {  Name = TestSetup.entity_name_for_404_response_error, Details = "Details1", Value = 2 }
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing404ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_404_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing409ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo {  Name = TestSetup.entity_name_for_409_response_error, Details = "Details1", Value = 1 },
                    new ToDo {  Name = TestSetup.entity_name_for_409_response_error, Details = "Details1", Value = 2 }
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing409ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_409_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing500ErrorResponseInMultiInsertAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo {  Name = TestSetup.entity_name_for_500_response_error, Details = "Details1", Value = 1 },
                    new ToDo {  Name = TestSetup.entity_name_for_500_response_error, Details = "Details1", Value = 2 }
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        [TestMethod]
        public async Task TestSaveMultiInsertThrowing500ErrorResponseInUpdateAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient("5");

                MockResponses(2);

                // Arrange
                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                var todoStoreNetwork = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                var toDos = new List<ToDo>
                {
                    new ToDo { ID = TestSetup.id_for_500_error_response_fake, Name = "Name1", Details = "Details1", Value = 1 },
                };

                // Act
                var exception = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
                {
                    await todoStoreNetwork.SaveAsync(toDos);
                });

                // Assert
                Assert.AreEqual(typeof(KinveyException), exception.GetType());
                var kinveyException = exception as KinveyException;
                Assert.AreEqual(EnumErrorCategory.ERROR_BACKEND, kinveyException.ErrorCategory);
                Assert.AreEqual(EnumErrorCode.ERROR_JSON_RESPONSE, kinveyException.ErrorCode);
            }
        }

        #endregion Negative tests

        #endregion Multi insert

        #endregion Save

        #region Subscribe Unsubscribe

        [TestMethod]
        public async Task TestSubscribeUnsubscribeAsync()
        {
            if (MockData)
            {
                // Setup
                kinveyClient = BuildClient();

                MockResponses(4);


                //Arrange
                var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

                await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

                await Client.SharedClient.ActiveUser.RegisterRealtimeAsync();

                //Act
                var isSuccess = await todoStore.Subscribe(new KinveyDataStoreDelegate<ToDo>
                {
                    OnNext = (result) =>
                    {

                    },
                    OnStatus = (status) =>
                    {

                    },
                    OnError = (error) =>
                    {

                    }
                });

                await todoStore.Unsubscribe();

                //Teardown
                await Client.SharedClient.ActiveUser.UnregisterRealtimeAsync();

                //Assert
                Assert.IsTrue(isSuccess);
            }
        }

        #endregion Subscribe Unsubscribe

        #region GetSyncCount

        [TestMethod]
        public async Task TestGetSyncCount()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(1);
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            // Act
            var exception = Assert.ThrowsException<KinveyException>(delegate
            {
                todoStore.GetSyncCount();
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_DATASTORE_NETWORK, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_DATASTORE_INVALID_SYNC_COUNT_OPERATION, kinveyException.ErrorCode);
        }

        #endregion GetSyncCount

        #region ClearCache

        [TestMethod]
        public async Task TestClearCacheAsync()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(1);
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            // Act
            var exception = Assert.ThrowsException<KinveyException>(delegate
            {
                todoStore.ClearCache();
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_DATASTORE_NETWORK, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_DATASTORE_INVALID_CLEAR_CACHE_OPERATION, kinveyException.ErrorCode);
        }

        #endregion ClearCache

        #region Purge

        [TestMethod]
        public async Task TestPurge()
        {
            // Setup
            kinveyClient = BuildClient();

            if (MockData)
            {
                MockResponses(1);
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK, kinveyClient);

            // Act
            var exception = Assert.ThrowsException<KinveyException>(delegate
            {
                todoStore.Purge();
            });

            // Assert
            Assert.AreEqual(typeof(KinveyException), exception.GetType());
            var kinveyException = exception as KinveyException;
            Assert.AreEqual(EnumErrorCategory.ERROR_DATASTORE_NETWORK, kinveyException.ErrorCategory);
            Assert.AreEqual(EnumErrorCode.ERROR_DATASTORE_INVALID_PURGE_OPERATION, kinveyException.ErrorCode);
        }

        #endregion Purge
    }
}
