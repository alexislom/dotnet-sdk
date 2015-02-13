﻿using System;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Gms.Gcm;

namespace KinveyXamarinAndroid
{
	public abstract class KinveyGCMService : IntentService
	{
		public KinveyGCMService ()
		{}

		static PowerManager.WakeLock sWakeLock;
		static object LOCK = new object();
		private const string MESSAGE_FROM_GCM = "msg";


		public static void RunIntentInService(Context context, Intent intent)
		{
			lock (LOCK)
			{
				if (sWakeLock == null)
				{
					var pm = PowerManager.FromContext(context);
					sWakeLock = pm.NewWakeLock(
						WakeLockFlags.Partial, "My WakeLock Tag");
				}
			}

			sWakeLock.Acquire();
			intent.SetClass(context, typeof(KinveyGCMService));
			context.StartService(intent);
		}

		protected override void OnHandleIntent(Intent intent)
		{
			try
			{
				Context context = this.ApplicationContext;
				string action = intent.Action;

				if (action.Equals("com.google.android.c2dm.intent.REGISTRATION"))
				{
					onRegistered(intent.GetStringExtra("REGISTERED"));
				}
				else if (action.Equals("com.google.android.c2dm.intent.RECEIVE"))
				{
					onMessage(intent.GetStringExtra (MESSAGE_FROM_GCM));
				}
				else if (action.Equals(GoogleCloudMessaging.MessageTypeDeleted))
				{
					onDelete(intent.GetIntExtra("DELETED", 0));

				}
				else if (action.Equals(GoogleCloudMessaging.MessageTypeSendError))
				{
					onError(intent.GetStringExtra("ERROR"));
				}
			}
			finally
			{
				lock (LOCK)
				{
					//Sanity check for null as this is a public method
					if (sWakeLock != null)
						sWakeLock.Release();
				}
			}
		}

		public abstract void onMessage (string message); 

		public abstract void onError (string error);

		public abstract void onDelete (int deleted);

		public abstract void onRegistered (string gcmID);

		public abstract void onUnregistered (string oldID);

	}
}

