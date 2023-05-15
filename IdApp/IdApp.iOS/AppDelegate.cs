using CoreGraphics;
using Foundation;
using IdApp.Helpers;
using System;
using UIKit;
using Xamarin.Forms;

namespace IdApp.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
	{
		private NSObject onKeyboardShowObserver;
		private NSObject onKeyboardHideObserver;

		//
		// This method is invoked when the application has loaded and is ready to run. In this
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication Application, NSDictionary Options)
		{
			UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.LightContent;

			Rg.Plugins.Popup.Popup.Init();
			ZXing.Net.Mobile.Forms.iOS.Platform.Init();
			FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
			Xamarin.Forms.Forms.Init();

			// This must be called after Xamarin.Forms.Forms.Init.
			FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageSourceHandler();

			FFImageLoading.Config.Configuration Configuration = FFImageLoading.Config.Configuration.Default;
			Configuration.DiskCacheDuration = TimeSpan.FromDays(7);
			//Configuration.DownloadCache = new AesDownloadCache(Configuration);
			FFImageLoading.ImageService.Instance.Initialize(Configuration);

			// Uncomment this to debug loading images from neuron (ensures that they are not loaded from cache).
			// FFImageLoading.ImageService.Instance.InvalidateCacheAsync(FFImageLoading.Cache.CacheType.Disk);

			this.LoadApplication(new App(this.GetType().Assembly));

			this.RegisterKeyBoardObserver();

			return base.FinishedLaunching(Application, Options);
		}

		public override void WillTerminate(UIApplication application)
		{
			if (this.onKeyboardShowObserver is null)
			{
				this.onKeyboardShowObserver.Dispose();
				this.onKeyboardShowObserver = null;
			}

			if (this.onKeyboardHideObserver is null)
			{
				this.onKeyboardHideObserver.Dispose();
				this.onKeyboardHideObserver = null;
			}
		}

		public override void WillEnterForeground(UIApplication Application)
		{
			base.WillEnterForeground(Application);
		}

		/// <summary>
		/// Method is called when an URL with a registered schema is being opened.
		/// </summary>
		/// <param name="app">Application</param>
		/// <param name="url">URL</param>
		/// <param name="options">Options</param>
		/// <returns>If URL is handled.</returns>
		public override bool OpenUrl(UIApplication Application, NSUrl Url, NSDictionary Options)
		{
			App.OpenUrlSync(Url.AbsoluteString);
			return true;
		}

		private void RegisterKeyBoardObserver()
		{
			this.onKeyboardShowObserver ??= UIKeyboard.Notifications.ObserveWillShow((object Sender, UIKeyboardEventArgs args) =>
			{
				NSValue Result = (NSValue)args.Notification.UserInfo.ObjectForKey(new NSString(UIKeyboard.FrameEndUserInfoKey));
				CGSize keyboardSize = Result.RectangleFValue.Size;

				MessagingCenter.Send<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears,
					new KeyboardAppearEventArgs { KeyboardSize = (float)keyboardSize.Height });
			});

			this.onKeyboardHideObserver ??= UIKeyboard.Notifications.ObserveWillHide((object Sender, UIKeyboardEventArgs args) =>
			{
				MessagingCenter.Send<object>(this, Constants.MessagingCenter.KeyboardDisappears);
			});
		}
    }
}
