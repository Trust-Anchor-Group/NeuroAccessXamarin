﻿using Android.App;
using Android.Content;
using Android.Gms.Extensions;
using Android.Graphics;
using Firebase.Messaging;
using IdApp.DeviceSpecific;
using IdApp.Services;
using IdApp.Services.Push;
using IdApp.Services.Xmpp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;

[assembly: Xamarin.Forms.Dependency(typeof(IdApp.Android.Push.MessagingService))]
namespace IdApp.Android.Push
{
	[Service(Exported=false)]
	[IntentFilter(new[]
	{
		"com.google.firebase.MESSAGING_EVENT",
		"com.google.firebase.INSTANCE_ID_EVENT"
	})]
	public class MessagingService : FirebaseMessagingService, IGetPushNotificationToken
	{
		public override void OnMessageReceived(RemoteMessage Message)
		{
			try
			{
				if (!App.IsOnboarded)
					return;

				string Body = Message.Data["myBody"];
				string Title = Message.Data["myTitle"];
				string ChannelId = Message.Data["channelId"];

				switch (ChannelId)
				{
					case Constants.PushChannels.Messages:
						this.ShowMessageNotification(Title, Body, Message.Data);
						break;

					case Constants.PushChannels.Petitions:
						this.ShowPetitionNotification(Title, Body, Message.Data);
						break;

					case Constants.PushChannels.Identities:
						this.ShowIdentitiesNotification(Title, Body, Message.Data);
						break;

					case Constants.PushChannels.Contracts:
						this.ShowContractsNotification(Title, Body, Message.Data);
						break;

					case Constants.PushChannels.EDaler:
						this.ShowEDalerNotification(Title, Body, Message.Data);
						break;

					case Constants.PushChannels.Tokens:
						this.ShowTokenNotification(Title, Body, Message.Data);
						break;

					case Constants.PushChannels.Provisioning:
						this.ShowProvisioningNotification(Title, Body, Message.Data);
						break;

					default:
						break;
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		public void ShowMessageNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			string IsObject = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "isObject":
						IsObject = P.Value;
						break;
				}
			}

			if (!string.IsNullOrEmpty(IsObject) && bool.Parse(IsObject))
				Title = LocalizationResourceManager.Current["MessageReceived"];

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot|PendingIntentFlags.Immutable);
			Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationChatIcon);

			Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.Messages)
				.SetSmallIcon(Resource.Drawable.NotificationSmallIcon)
				.SetLargeIcon(LargeImage)
				.SetContentTitle(Title)
				.SetContentText(MessageBody)
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowPetitionNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			string FromJid =  string.Empty;
			string RosterName = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "fromJid":
						FromJid = P.Value;
						break;

					case "rosterName":
						RosterName = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);
			Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationPetitionIcon);

			Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.Petitions)
				.SetSmallIcon(Resource.Drawable.NotificationSmallIcon)
				.SetLargeIcon(LargeImage)
				.SetContentTitle(Title)
				.SetContentText((string.IsNullOrEmpty(RosterName) ? FromJid : RosterName) + ": " + MessageBody)
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowIdentitiesNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			string LegalId = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "legalId":
						LegalId = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(LegalId))
			{
				Content.AppendLine();
				Content.Append('(');
				Content.Append(LegalId);
				Content.Append(')');
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);
			Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationIdentitieIcon);

			Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.Identities)
				.SetSmallIcon(Resource.Drawable.NotificationSmallIcon)
				.SetLargeIcon(LargeImage)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowContractsNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			string LegalId = string.Empty;
			string ContractId = string.Empty;
			string Role = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "legalId":
						LegalId = P.Value;
						break;

					case "contractId":
						ContractId = P.Value;
						break;

					case "role":
						Role = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(Role))
			{
				Content.AppendLine();
				Content.Append(Role);
			}

			if (!string.IsNullOrEmpty(ContractId))
			{
				Content.AppendLine();
				Content.Append('(');
				Content.Append(ContractId);
				Content.Append(')');
			}

			if (!string.IsNullOrEmpty(LegalId))
			{
				Content.AppendLine();
				Content.Append('(');
				Content.Append(LegalId);
				Content.Append(')');
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);
			Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationContractIcon);

			Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.Contracts)
				.SetSmallIcon(Resource.Drawable.NotificationSmallIcon)
				.SetLargeIcon(LargeImage)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowEDalerNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			string Amount = string.Empty;
			string Currency = string.Empty;
			string Timestamp = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "amount":
						Amount = P.Value;
						break;

					case "currency":
						Currency = P.Value;
						break;

					case "timestamp":
						Timestamp = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(Amount))
			{
				Content.AppendLine();
				Content.Append(Amount);


				if (!string.IsNullOrEmpty(Currency))
				{
					Content.Append(' ');
					Content.Append(Currency);
				}

				if (!string.IsNullOrEmpty(Timestamp))
				{
					Content.Append(" (");
					Content.Append(Timestamp);
					Content.Append(')');
				}
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);
			Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationEDalerIcon);

			Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.EDaler)
				.SetSmallIcon(Resource.Drawable.NotificationSmallIcon)
				.SetLargeIcon(LargeImage)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public void ShowTokenNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			string Value = string.Empty;
			string Currency = string.Empty;

			foreach (KeyValuePair<string, string> P in Data)
			{
				switch (P.Key)
				{
					case "value":
						Value = P.Value;
						break;

					case "currency":
						Currency = P.Value;
						break;
				}
			}

			Intent Intent = new(this, typeof(MainActivity));
			Intent.AddFlags(ActivityFlags.ClearTop);

			foreach (string Key in Data.Keys)
				Intent.PutExtra(Key, Data[Key]);

			StringBuilder Content = new();
			Content.Append(MessageBody);

			if (!string.IsNullOrEmpty(Value))
			{
				Content.AppendLine();
				Content.Append(Value);


				if (!string.IsNullOrEmpty(Currency))
				{
					Content.Append(' ');
					Content.Append(Currency);
				}
			}

			PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);
			Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationTokenIcon);

			Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.Tokens)
				.SetSmallIcon(Resource.Drawable.NotificationSmallIcon)
				.SetLargeIcon(LargeImage)
				.SetContentTitle(Title)
				.SetContentText(Content.ToString())
				.SetAutoCancel(true)
				.SetContentIntent(PendingIntent);

			NotificationManager NotificationManager = NotificationManager.FromContext(this);
			NotificationManager.Notify(100, notificationBuilder.Build());
		}

		public async void ShowProvisioningNotification(string Title, string MessageBody, IDictionary<string, string> Data)
		{
			try
			{
				string RemoteJid = string.Empty;
				string Jid = string.Empty;
				string Key = string.Empty;
				string Query = string.Empty;

				foreach (KeyValuePair<string, string> P in Data)
				{
					switch (P.Key)
					{
						case "remoteJid":
							RemoteJid = P.Value;
							break;

						case "jid":
							Jid = P.Value;
							break;

						case "key":
							Key = P.Value;
							break;

						case "q":
							Query = P.Value;
							break;
					}
				}

				Intent Intent = new(this, typeof(MainActivity));
				Intent.AddFlags(ActivityFlags.ClearTop);

				foreach (string Key2 in Data.Keys)
					Intent.PutExtra(Key2, Data[Key2]);

				IServiceReferences ServiceReferences = App.Instantiate<IXmppService>();

				string ThingName = await ContactInfo.GetFriendlyName(Jid, ServiceReferences);

				if (string.IsNullOrWhiteSpace(MessageBody))
					MessageBody = await ContactInfo.GetFriendlyName(RemoteJid, ServiceReferences);

				MessageBody = Query switch
				{
					"canRead" => string.Format(LocalizationResourceManager.Current["ReadoutRequestText"], MessageBody, ThingName),
					"canControl" => string.Format(LocalizationResourceManager.Current["ControlRequestText"], MessageBody, ThingName),
					_ => string.Format(LocalizationResourceManager.Current["AccessRequestText"], MessageBody, ThingName),
				};

				PendingIntent PendingIntent = global::Android.App.PendingIntent.GetActivity(this, 100, Intent, PendingIntentFlags.OneShot);
				Bitmap LargeImage = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.NotificationPetitionIcon);

				Notification.Builder notificationBuilder = new Notification.Builder(this, Constants.PushChannels.Tokens)
					.SetSmallIcon(Resource.Drawable.NotificationPetitionIcon)
					.SetLargeIcon(LargeImage)
					.SetContentTitle(Title)
					.SetContentText(MessageBody)
					.SetAutoCancel(true)
					.SetContentIntent(PendingIntent);

				NotificationManager NotificationManager = NotificationManager.FromContext(this);
				NotificationManager.Notify(100, notificationBuilder.Build());
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		public override async void OnNewToken(string NewToken)
		{
			try
			{
				IPushNotificationService PushService = Types.Instantiate<IPushNotificationService>(true);

				if (PushService is not null)
				{
					TokenInformation TokenInformation = new()
					{
						Token = NewToken,
						ClientType = ClientType.Android,
						Service = PushMessagingService.Firebase
					};

					await PushService.CheckPushNotificationToken(TokenInformation);
				}
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		/// <summary>
		/// Gets a Push Notification token for the device.
		/// </summary>
		/// <returns>Token, Service used, and type of client.</returns>
		public async Task<TokenInformation> GetToken()
		{
			Java.Lang.Object Token = string.Empty;

			try
			{
				Token = await FirebaseMessaging.Instance.GetToken().AsAsync<Java.Lang.Object>();
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}

			TokenInformation TokenInformation = new()
			{
				Token = Token.ToString(),
				ClientType = ClientType.Android,
				Service = PushMessagingService.Firebase
			};

			return TokenInformation;
		}
	}
}
