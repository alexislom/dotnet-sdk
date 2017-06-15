﻿using System;

using CoreGraphics;
using UIKit;

namespace DemoSeedCache
{
public partial class LoginViewController : UIViewController
{
	UIApplicationDelegate appDel = UIApplication.SharedApplication.Delegate as UIApplicationDelegate;
	public UIButton button;
	UITextField usernameField;
	UITextField passwordField;
//UIColor backgroundColor = UIColor.FromRGB(17, 162, 209);
UIColor backgroundColor = UIColor.FromRGB(226, 236, 238);
UIColor buttonColor = UIColor.FromRGB(92, 229, 0);
	
	public LoginViewController()
	{
	}

	//protected MyViewController(IntPtr handle) : base(handle)
	//{
	//	// Note: this .ctor should not contain any initialization logic.
	//}

	public override void ViewDidLoad()
	{
		base.ViewDidLoad();
		// Perform any additional setup after loading the view, typically from a nib.

			View.BackgroundColor = backgroundColor;
		Title = "Demo Seed Cache";

		nfloat h = 31.0f;
		nfloat w = View.Bounds.Width;

		usernameField = new UITextField
		{
			Placeholder = "Username",
			BorderStyle = UITextBorderStyle.RoundedRect,
			Frame = new CGRect(10, 82, w - 20, h)
		};
		View.AddSubview(usernameField);

		passwordField = new UITextField
		{
			SecureTextEntry = true,
			Placeholder = "Password",
			BorderStyle = UITextBorderStyle.RoundedRect,
			Frame = new CGRect(10, 122, w - 20, h)
		};
		View.AddSubview(passwordField);

		button = UIButton.FromType(UIButtonType.System);
		button.Frame = new CGRect(10, 200, w - 20, 44);
		button.SetTitle("Login", UIControlState.Normal);
		button.SetTitleColor(UIColor.Black, UIControlState.Normal);
			button.BackgroundColor = buttonColor;//UIColor.LightGray;

		//var user = new UIViewController();
		//user.View.BackgroundColor = UIColor.Magenta;

		button.TouchUpInside += async (sender, e) => {

			AppDelegate myAppDel = (appDel.Self as DemoSeedCache.AppDelegate);

			await myAppDel.Login(usernameField.Text, passwordField.Text);

			//this.NavigationController.PushViewController(user, true);
		};

		View.AddSubview(button);
	}

	public override void DidReceiveMemoryWarning()
	{
		base.DidReceiveMemoryWarning();
		// Release any cached data, images, etc that aren't in use.
	}
}

public partial class DataViewController : UIViewController
{
UIColor backgroundColor = UIColor.FromRGB(226, 236, 238);
UIColor buttonColor = UIColor.FromRGB(92, 229, 0);
	public override void ViewDidLoad()
	{
		base.ViewDidLoad();

		Title = "Seeding cache";
		View.BackgroundColor = backgroundColor;
		nfloat h = 31.0f;
		nfloat w = View.Bounds.Width;

		AppDelegate myAppDel = (UIApplication.SharedApplication.Delegate as DemoSeedCache.AppDelegate);

		UITextField IDView = new UITextField
		{
			Text = "User ID: " + myAppDel.UserID,
			TextAlignment = UITextAlignment.Center,
			Frame = new CGRect(10, 82, w - 20, h),
			BackgroundColor = UIColor.White
		};
		View.AddSubview(IDView);

		UITextField AccessTokenView = new UITextField
		{
			Text = "Access Token: " + myAppDel.AccessToken,
			TextAlignment = UITextAlignment.Center,
			Frame = new CGRect(10, 122, w - 20, h),
			BackgroundColor = UIColor.White
		};
		View.AddSubview(AccessTokenView);

		UIButton button;
		button = UIButton.FromType(UIButtonType.System);
		button.Frame = new CGRect(10, 162, w - 20, 44);
		button.SetTitle("Logout", UIControlState.Normal);
		button.SetTitleColor(UIColor.Black, UIControlState.Normal);
		button.BackgroundColor = buttonColor;//UIColor.FromRGB(17,162,209);//UIColor.LightGray;

		var user = new UIViewController();
		user.View.BackgroundColor = UIColor.Orange;

		button.TouchUpInside += (sender, e) => {

			//AppDelegate myAppDel = (UIApplication.SharedApplication.Delegate as testiosapp.AppDelegate);

			myAppDel.Logout();

			//this.NavigationController.PushViewController(user, true);
		};
		View.AddSubview(button);
	}

	public override void DidReceiveMemoryWarning()
	{
		base.DidReceiveMemoryWarning();
	}
}
}
