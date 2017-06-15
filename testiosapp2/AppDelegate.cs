﻿using Foundation;
using UIKit;

using Kinvey;
using KinveyXamariniOS;
using SQLite.Net.Platform.XamarinIOS;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;

namespace testiosapp2
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations

		public override UIWindow Window
		{
			get;
			set;
		}

		Client myClient;
		testiosapp2.LoginViewController vc;
		public string UserID { get { return myClient.ActiveUser.Id; } }
		public string AccessToken { get { return myClient.ActiveUser.AccessToken; } }
		public string FilePath { get; set; }

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			// Override point for customization after application launch.
			// If not required for your application you can safely delete this method

			BuildClient();

			return true;
		}

		public void BuildClient()
		{
			//string appKey = "kid_r12RGpW6", appSecret = "b1b7768429344a2085e75e2d48b39d19"; // SSO-TEST
			//string appKey = "kid_ZkPDb_34T", appSecret = "c3752d5079f34353ab89d07229efaf63"; // MIC-SAML-TEST
			//string appKey = "kid_Zy0JOYPKkZ", appSecret = "d83de70e64d540e49acd6cfce31415df"; // UNITTESTFRAMEWORK
			//string appKey = "kid_byWWRXzJCe", appSecret = "4a58018febe945fea5ba76c08ce1e870"; // VINAY 1ST APP
			string appKey = "kid_B15Lb5Pl-", appSecret = "b7b92c030f2d446999d66ad2f3c50c88";

			//FilePath = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User)[0].ToString();
			FilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			System.Diagnostics.Debug.WriteLine("\t\t VRG FILEPATH: " + FilePath);
			Client.Builder cb = new Client.Builder(appKey, appSecret)
				.setFilePath(FilePath)
				.setOfflinePlatform(new SQLitePlatformIOS())
				//.setCredentialStore(new IOSNativeCredentialStore())
				//.SetSSOGroupKey("KinveyOrg")
				//.setBaseURL("https://alm-kcs.ngrok.io")
				.setBaseURL("https://v3yk1n-kcs.kinvey.com")
				.setLogger(delegate (string msg) { Console.WriteLine(msg); });

			myClient = cb.Build();

			//myClient.MICHostName = "https://alm-auth.ngrok.io"; // SSO-TEST
			//myClient.MICApiVersion = "v3"; // SSO-TEST

			// create a new window instance based on the screen size
			Window = new UIWindow(UIScreen.MainScreen.Bounds);

			//if (true)
			if (myClient.IsUserLoggedIn())
			{
				var alreadyLoggedInController = new testiosapp2.DataViewController();
				var navController = new UINavigationController(alreadyLoggedInController);
				Window.RootViewController = navController;
			}
			else
			{
				vc = new testiosapp2.LoginViewController();
				var navController = new UINavigationController(vc);
				Window.RootViewController = navController;
			}

			// make the window visible
			Window.MakeKeyAndVisible();
		}

		public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
		{
			return myClient.ActiveUser.OnOAuthCallbackRecieved(url);
		}

		public async Task<User> Login(string user, string pass)
		{
			//			Dictionary<string, JToken> attr = new Dictionary<string, JToken> ();
			//			email.Add ("email", "gob@bluth.com");
			//			attr.Add ("my_field", "blah blah");
			//			var newuser = await myClient. ().CreateAsync ("George Michael Bluth", "cousin", attr);
			//			var newuser2 = await myClient.CurrentUser.CreateAsync ("Tobias Funke", "actor");
			//			Dictionary<string, JToken> last_name = new Dictionary<string, JToken>();
			//			last_name.Add("last_name", "Bluth");
			//			await myClient.CurrentUser.CreateAsync ("Lindsay Bluth", "me", last_name);
			//			await myClient.CurrentUser.CreateAsync ("Maeby Bluth", "Surely", last_name);

			try
			{
				//string redirectURI = "kinveyAuthDemo://";
				//await User.LoginWithAuthorizationCodeAPIAsync(user, pass, redirectURI, myClient);

				//await User.LoginAsync(user, pass);
				await User.LoginAsync("ccalato", "ccalato");

				//					myClient.CurrentUser.LoginWithAuthorizationCodeLoginPage("kinveyAuthDemo://", new KinveyMICDelegate<User>{
				//						onSuccess = (loggedInUser) => { user = loggedInUser; },
				//						onError = (e) => { Console.WriteLine("Error with MIC Login"); },
				//						onReadyToRender = (url) => { UIApplication.SharedApplication.OpenUrl(new NSUrl(url)); }
				//					});


				//string str = "Finished Launching.";
				//Console.WriteLine("VRG : " + str);
				//Console.WriteLine("VRG: Logged in as: " + myClient.ActiveUser.Id);

				//var alert = UIAlertController.Create("UserID: " + myClient.ActiveUser.Id, "AccessToken: " + myClient.ActiveUser.AccessToken, UIAlertControllerStyle.Alert);
				//if (alert.PopoverPresentationController != null)
				//	alert.PopoverPresentationController.but.BarButtonItem = cvc.button;
				//alert.PresentViewController(alert, animated: true, completionHandler: null);
				//alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Cancel, null));
				//vc.PresentViewController(alert, true, null);
				var alreadyLoggedInController = new testiosapp2.DataViewController();
				var navController = new UINavigationController(alreadyLoggedInController);
				Window.RootViewController = navController;

				//await ManipulateData();
				await LawsonFileTest();
			}
			catch (KinveyException e)
			{
				//Console.WriteLine("VRG (exception caught) Exception Request ID -> " + e.RequestID);
				Console.WriteLine("VRG (exception caught) Exception Error -> " + e.Error);
				Console.WriteLine("VRG (exception caught) Exception Description -> " + e.Description);
				Console.WriteLine("VRG (exception caught) Exception Debug -> " + e.Debug);
			}

			return myClient.ActiveUser;
		}

		public void Logout()
		{
			myClient?.ActiveUser?.Logout();
			var logInController = new testiosapp2.LoginViewController();
			var navController = new UINavigationController(logInController);
			Window.RootViewController = navController;
		}

		private async Task LawsonFileTest()
		{
			// upload
			var sw = new System.Diagnostics.Stopwatch();
			//sw.Start();
			//FileMetaData fileMetaData = new FileMetaData();
			//fileMetaData.fileName = "kinveyOffline.sqlite";
			//bool publicAccess = true;
			//fileMetaData._public = publicAccess;
			//byte[] content = System.IO.File.ReadAllBytes("/Users/vinay/Desktop/LawsonTest/kinveyOffline.sqlite.gz");
			//int contentSize = (content.Length) * sizeof(byte);
			//fileMetaData.size = contentSize;
			////System.IO.MemoryStream streamContent = new System.IO.MemoryStream(content);
			//FileMetaData uploadFMD = await Client.SharedClient.File().uploadAsync(fileMetaData, content);
			//sw.Stop();
			//System.Diagnostics.Debug.WriteLine("\t\tVRG Gzip SQLite Upload Time(ms): " + sw.ElapsedMilliseconds);
			//sw.Reset();

			// download
			//FileMetaData uploadMetaData = new FileMetaData();
			//uploadMetaData.fileName = "kinveyOffline.sqlite";
			//uploadMetaData._public = true;

			//byte[] DLcontent = System.IO.File.ReadAllBytes("/Users/vinay/Desktop/LawsonTest/");
			//int DLcontentSize = (content.Length) * sizeof(byte);
			//uploadMetaData.size = contentSize;
			//FileMetaData uploadFMD = await Client.SharedClient.File().uploadAsync(uploadMetaData, DLcontent);

			sw.Start();
			FileMetaData downloadMetaData = new FileMetaData();
			downloadMetaData = await Client.SharedClient.File().downloadMetadataAsync("12345");
			//downloadMetaData.id = uploadFMD.id;
			byte[] downloadContent = new byte[downloadMetaData.size];

			downloadContent = await Client.SharedClient.File().downloadAsync(downloadMetaData, downloadContent);
			//System.IO.File.WriteAllBytes("/Users/vinay/Desktop/LawsonTest/DLkinveyOffline.sqlite.gz", content);
			string fullPath = Path.Combine(FilePath, "kinveyOffline.sqlite.gz");
			System.IO.File.WriteAllBytes(fullPath, downloadContent);
			sw.Stop();
			System.Diagnostics.Debug.WriteLine("\t\tVRG Gzip SQLite Download Time(ms): " + sw.ElapsedMilliseconds);
			sw.Reset();

			// Uncompress file
			sw.Start();
			//var gzip = new System.IO.Compression.GZipStream(new System.IO.FileStream("/Users/vinay/Desktop/LawsonTest/DLkinveyOffline.sqlite.gz",System.IO.FileMode.Open),
			//                                                System.IO.Compression.CompressionMode.Decompress);
			var gzip = new System.IO.Compression.GZipStream(new System.IO.FileStream(fullPath, System.IO.FileMode.Open),
															System.IO.Compression.CompressionMode.Decompress);

			string decompressedFile = Path.Combine(FilePath, "kinveyOffline.sqlite");
			System.IO.FileStream decompressedFileStream = System.IO.File.Create(decompressedFile);
			//System.IO.FileStream decompressedFileStream = System.IO.File.Create("/Users/vinay/Desktop/LawsonTest/DLkinveyOffline.sqlite");
			try
			{
				gzip.CopyTo(decompressedFileStream);
			}
			catch (Exception e)
			{
				string msg = e.Message;
			}
			sw.Stop();
			System.Diagnostics.Debug.WriteLine("\t\tVRG Gzip SQLite Uncompress Time(ms): " + sw.ElapsedMilliseconds);
			sw.Reset();

			var store = DataStore<hierarchy>.Collection("hierarchycache", DataStoreType.SYNC);
			var findquery = store.Where(x => (x.SAPCustomerNumber.Equals("0010005607"))).OrderBy(x => x.ID);
			//var cacheHits = new List<hierarchy>();
			var localHits = await store.FindAsync(findquery);
			//                                        , new KinveyDelegate<List<hierarchy>>
			//{
			//	onError = (err) => System.Diagnostics.Debug.WriteLine(err.Message),
			//	onSuccess = (cacheResults) => cacheHits.AddRange(cacheResults)
			//});

		}

		private async Task LawsonTest()
		{
			List<string> customerList = new List<string> { "SBOOK", "MA00056", "MA00452", "MA20313", "MA00040", "MA00405", "MA09200", "MA09208", "MA20128", "MA20404", "MA20280" };

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();


            var store = DataStore<hierarchy>.Collection("hierarchycache", DataStoreType.SYNC);
			store.AutoPagination = true;

            foreach (var cust in customerList)
			{
				var query = store.Where(x => (x.SAPCustomerNumber.Equals(cust))).OrderBy(x => x.ID);
				await store.PullAsync(query, -1, true);
            }

            //// Construct single predicate
            //Expression<Func<hierarchy, bool>> predicate = null; 
            //foreach (var cust in customerList) {
            //    if (predicate == null)
            //    {
            //        predicate = (x => x.SAPCustomerNumber.Equals(cust));
            //    }
            //    else { 
            //        predicate = predicate.Or(x => x.SAPCustomerNumber.Equals(cust));
            //    }                
            //}
            //var query = store.Where(predicate).OrderBy(x => x.ID);
            //await store.PullAsync(query, -1, true);

            sw.Stop();
			System.Diagnostics.Debug.WriteLine("Total time: " + sw.Elapsed);
		}

		private async Task<DataStore<Book>> ManipulateData()
		{
			DataStore<Book> store = DataStore<Book>.Collection("Book", DataStoreType.NETWORK);
			try
			{

				List<Book> listBooks = new List<Book>();
				listBooks = await store.FindAsync();

			}
			catch (Exception e)
			{
				Console.Write(e);
			}
			return store;
		}

		public override void OnResignActivation(UIApplication application)
		{
			// Invoked when the application is about to move from active to inactive state.
			// This can occur for certain types of temporary interruptions (such as an incoming phone call or SMS message) 
			// or when the user quits the application and it begins the transition to the background state.
			// Games should use this method to pause the game.
		}

		public override void DidEnterBackground(UIApplication application)
		{
			// Use this method to release shared resources, save user data, invalidate timers and store the application state.
			// If your application supports background exection this method is called instead of WillTerminate when the user quits.
		}

		public override void WillEnterForeground(UIApplication application)
		{
			// Called as part of the transiton from background to active state.
			// Here you can undo many of the changes made on entering the background.
		}

		public override void OnActivated(UIApplication application)
		{
			// Restart any tasks that were paused (or not yet started) while the application was inactive. 
			// If the application was previously in the background, optionally refresh the user interface.
		}

		public override void WillTerminate(UIApplication application)
		{
			// Called when the application is about to terminate. Save data, if needed. See also DidEnterBackground.
		}
	}
}
