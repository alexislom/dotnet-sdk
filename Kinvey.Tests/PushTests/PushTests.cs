﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Kinvey.Tests
{
    [TestClass]
    public class PushTests : BaseTestClass
    {
        private Client kinveyClient;
        private AbstractPush testPush;
        private const string androidPlatform = "android";
        private const string iosPlatform = "ios";
        private string token;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();

            Client.Builder builder = ClientBuilder
                .SetFilePath(TestSetup.db_dir);

            if (MockData)
            {
                builder.setBaseURL("http://localhost:8080");
            }

            kinveyClient = builder.Build();
            testPush = new TestPush(kinveyClient);
            token = Guid.NewGuid().ToString();
        }

        [TestMethod]
        public async Task TestEnablePushViaRestAndroidAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = testPush.EnablePushViaRest(androidPlatform, token).Execute();

            //Assert
            Assert.IsNotNull(pushPayload);
        }

        [TestMethod]
        public async Task TestEnablePushViaRestIosAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = testPush.EnablePushViaRest(iosPlatform, token).Execute();

            //Assert
            Assert.IsNotNull(pushPayload);
        }

        [TestMethod]
        public async Task TestDisablePushViaRestAndroidAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = testPush.DisablePushViaRest(androidPlatform, token).Execute();

            //Assert
            Assert.IsNotNull(pushPayload);
        }

        [TestMethod]
        public async Task TestDisablePushViaRestIosAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = testPush.DisablePushViaRest(iosPlatform, token).Execute();

            //Assert
            Assert.IsNotNull(pushPayload);
        }

        [TestMethod]
        public async Task TestEnablePushAndroidAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = await testPush.EnablePushAsync(androidPlatform, token);

            //Assert
            Assert.IsNotNull(pushPayload);
        }


        [TestMethod]
        public async Task TestEnablePushIosAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = await testPush.EnablePushAsync(iosPlatform, token);

            //Assert
            Assert.IsNotNull(pushPayload);
        }

        [TestMethod]
        public async Task TestDisablePushAndroidAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = await testPush.DisablePushAsync(androidPlatform, token);

            //Assert
            Assert.IsNotNull(pushPayload);
        }

        [TestMethod]
        public async Task TestDisablePushIosAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(2);
            }
            else
            {
                Assert.Fail("Use this test only with mocks.");
            }

            //Arrange
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            //Act
            var pushPayload = await testPush.DisablePushAsync(iosPlatform, token);

            //Assert
            Assert.IsNotNull(pushPayload);
        }
    }
}
