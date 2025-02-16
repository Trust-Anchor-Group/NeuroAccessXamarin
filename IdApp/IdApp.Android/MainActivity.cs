﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Views;
using IdApp.Android.Nfc;
using IdApp.Nfc;
using IdApp.Services.Nfc;
using System;
using System.Collections.Generic;

namespace IdApp.Android
{
	[Activity(Label = "@string/app_name", Icon = "@mipmap/icon", Theme = "@style/LaunchTheme", MainLauncher = true,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.Locale,
		ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
	[IntentFilter(new string[] { NfcAdapter.ActionNdefDiscovered },
		Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "*/*")]
	[IntentFilter(new string[] { Intent.ActionView },
		Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
		DataSchemes = new string[] { "iotid", "tagsign", "obinfo" })]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		private static NfcAdapter nfcAdapter = null;
		private static Context configurationContext = null;

		protected override void OnCreate(Bundle SavedInstanceState)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.SetTheme(Resource.Style.MainTheme);

			base.OnCreate(SavedInstanceState);

			this.Init(SavedInstanceState);
		}

		public override Resources Resources
		{
			get
			{
				if (configurationContext is null)
				{
					Configuration config = new();
					config.SetToDefaults();
					configurationContext = this.CreateConfigurationContext(config);
				}

				return configurationContext.Resources;
			}
		}

		private void Init(Bundle SavedInstanceState)
		{
			SecureDisplay.SetMainWindow(this.Window, true);

			this.Window.SetFlags(
				WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure,
				WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Secure);

			nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

			Xamarin.Essentials.Platform.Init(this, SavedInstanceState);
			ZXing.Net.Mobile.Forms.Android.Platform.Init();
			Rg.Plugins.Popup.Popup.Init(this);
			FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);

			global::Xamarin.Forms.Forms.Init(this, SavedInstanceState);

			try
			{
				// This must be called after Xamarin.Forms.Forms.Init.
				FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageViewHandler();

				FFImageLoading.Config.Configuration Configuration = FFImageLoading.Config.Configuration.Default;
				Configuration.DiskCacheDuration = TimeSpan.FromDays(7);
				//Configuration.DownloadCache = new AesDownloadCache(Configuration);
				FFImageLoading.ImageService.Instance.Initialize(Configuration);

				// Uncomment this to debug loading images from neuron (ensures that they are not loaded from cache).
				// FFImageLoading.ImageService.Instance.InvalidateCacheAsync(FFImageLoading.Cache.CacheType.Disk);
			}
			catch (Exception)
			{
				// TODO
			}

			this.LoadApplication(new App(this.GetType().Assembly));
		}

		public override void OnRequestPermissionsResult(int RequestCode, string[] Permissions, [GeneratedEnum] Permission[] GrantResults)
		{
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(RequestCode, Permissions, GrantResults);
			base.OnRequestPermissionsResult(RequestCode, Permissions, GrantResults);
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (nfcAdapter is not null)
			{
				Intent Intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

				PendingIntent PendingIntent = PendingIntent.GetActivity(this, 0, Intent, PendingIntentFlags.Mutable);
				nfcAdapter.EnableForegroundDispatch(this, PendingIntent, null, null);
			}
		}

		protected override void OnPause()
		{
			base.OnPause();
			nfcAdapter?.DisableForegroundDispatch(this);
		}

		protected override async void OnNewIntent(Intent Intent)
		{
			try
			{
				switch (Intent.Action)
				{
					case Intent.ActionView:
						string Url = Intent?.Data?.ToString();
						App.OpenUrlSync(Url);
						break;

					case NfcAdapter.ActionTagDiscovered:
					case NfcAdapter.ActionNdefDiscovered:
					case NfcAdapter.ActionTechDiscovered:
						Tag Tag = Intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;

						if (Tag is not null)
						{
							byte[] ID = Tag.GetId();
							string[] TechList = Tag.GetTechList();
							List<INfcInterface> Interfaces = new();

							foreach (string Tech in TechList)
							{
								switch (Tech)
								{
									case "android.nfc.tech.IsoDep":
										Interfaces.Add(new IsoDepInterface(Tag));
										break;

									case "android.nfc.tech.MifareClassic":
										Interfaces.Add(new MifareClassicInterface(Tag));
										break;

									case "android.nfc.tech.MifareUltralight":
										Interfaces.Add(new MifareUltralightInterface(Tag));
										break;

									case "android.nfc.tech.Ndef":
										Interfaces.Add(new NdefInterface(Tag));
										break;

									case "android.nfc.tech.NdefFormatable":
										Interfaces.Add(new NdefFormatableInterface(Tag));
										break;

									case "android.nfc.tech.NfcA":
										Interfaces.Add(new NfcAInterface(Tag));
										break;

									case "android.nfc.tech.NfcB":
										Interfaces.Add(new NfcBInterface(Tag));
										break;

									case "android.nfc.tech.NfcBarcode":
										Interfaces.Add(new NfcBarcodeInterface(Tag));
										break;

									case "android.nfc.tech.NfcF":
										Interfaces.Add(new NfcFInterface(Tag));
										break;

									case "android.nfc.tech.NfcV":
										Interfaces.Add(new NfcVInterface(Tag));
										break;
								}
							}

							INfcService Service = App.Instantiate<INfcService>();
							await Service.TagDetected(new NfcTag(ID, Interfaces.ToArray()));
						}
						break;
				}
			}
			catch (Exception ex)
			{
				Waher.Events.Log.Critical(ex);
				// TODO: Handle read & connection errors.
			}
		}

		public override void OnBackPressed()
		{
			Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed);
		}
	}
}
