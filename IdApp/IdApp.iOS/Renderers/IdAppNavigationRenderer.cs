﻿using System;
using System.ComponentModel;
using Foundation;
using IdApp.Pages;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NavigationPage), typeof(IdApp.iOS.Renderers.IdAppNavigationRenderer))]
namespace IdApp.iOS.Renderers
{
	public class IdAppNavigationRenderer : NavigationRenderer
	{
		private bool disposed = false;

		// We want status bar style to be the same regardless of navigation bar text color and iOS theme. However, the default Xamarin.Forms
		// renderer updates status bar style in various places in a way which is undesirable, see https://github.com/xamarin/Xamarin.Forms/issues/4343.
		private UIStatusBarStyle originalStatusBarStyle = UIStatusBarStyle.Default;

		public override void ViewDidLoad()
		{
			this.InteractivePopGestureRecognizer.Enabled = false;
			this.originalStatusBarStyle = UIApplication.SharedApplication.StatusBarStyle;

			base.ViewDidLoad();
			this.Element.PropertyChanged += this.OnElementPropertyChanged;

			UIApplication.SharedApplication.StatusBarStyle = this.originalStatusBarStyle;
		}

		[Export("navigationBar:shouldPopItem:")]
		[Xamarin.Forms.Internals.Preserve(Conditional = true)]
		public bool ShouldPopItem(UINavigationBar navigationBar, UINavigationItem item)
		{
			try
			{
				Page CurrentPage = App.Current.MainPage;
				if (CurrentPage is NavigationPage NavigationPage)
				{
					CurrentPage = NavigationPage.CurrentPage;
				}

				return CurrentPage is not ContentBasePage ContentBasePage || !ContentBasePage.OnToolbarBackButtonPressed();
			}
			catch (Exception Exception)
			{
				// Xamarin.Forms Shell code does a dispatch, so we'd better do the same.
				CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(async () =>
				{
					await App.Current.MainPage.DisplayAlert("IdAppNavigationPageRenderer.ShouldPopItem exception", Exception.Message, "OK");
				});

				return false;
			}
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);
			UIApplication.SharedApplication.StatusBarStyle = this.originalStatusBarStyle;
		}

		public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
		{
			base.TraitCollectionDidChange(previousTraitCollection);
			UIApplication.SharedApplication.StatusBarStyle = this.originalStatusBarStyle;
		}

		protected override void Dispose(bool Disposing)
		{
			if (this.disposed)
				return;

			this.disposed = true;

			if (Disposing)
			{
				this.Element.PropertyChanged -= this.OnElementPropertyChanged;
			}

			base.Dispose(Disposing);
		}

		private void OnElementPropertyChanged(object Sender, PropertyChangedEventArgs Args)
		{
			if (Args.PropertyName == NavigationPage.BarTextColorProperty.PropertyName)
			{
				UIApplication.SharedApplication.StatusBarStyle = this.originalStatusBarStyle;
			}
		}
	}
}
