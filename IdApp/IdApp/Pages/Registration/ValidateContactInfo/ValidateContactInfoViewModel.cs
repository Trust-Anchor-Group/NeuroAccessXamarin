﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;
using IdApp.Services.Tag;
using Xamarin.CommunityToolkit.Helpers;
using Rg.Plugins.Popup.Services;

namespace IdApp.Pages.Registration.ValidateContactInfo
{
	/// <summary>
	/// The view model to bind to when showing Step 1 of the registration flow: choosing an operator.
	/// </summary>
	public class ValidateContactInfoViewModel : RegistrationStepViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ValidateContactInfoViewModel"/> class.
		/// </summary>
		public ValidateContactInfoViewModel()
			: base(RegistrationStep.ValidateContactInfo)
		{
			// supersede the send and verify pairs
			this.SendAndVerifyEMailCodeCommand = new Command(async () => await this.SendAndVerifyEMailCode(), this.SendAndVerifyEMailCodeCanExecute);
			this.SendAndVerifyPhoneNrCodeCommand = new Command(async () => await this.SendAndVerifyPhoneNrCode(), this.SendAndVerifyPhoneNrCodeCanExecute);

			// are kept for backward compatibility
			this.SendEMailCodeCommand = new Command(async () => await this.SendEMailCode(), this.SendEMailCodeCanExecute);
			this.VerifyEMailCodeCommand = new Command(async () => await this.VerifyEMailCode(), this.VerifyEMailCodeCanExecute);

			this.SendPhoneNrCodeCommand = new Command(async () => await this.SendPhoneNrCode(), this.SendPhoneNrCodeCanExecute);
			this.VerifyPhoneNrCodeCommand = new Command(async () => await this.VerifyPhoneNrCode(), this.VerifyPhoneNrCodeCanExecute);

			this.Title = LocalizationResourceManager.Current["ContactInformation"];
			this.Purposes = new ObservableCollection<string>();
		}

		/// <summary>
		/// Override this method to do view model specific setup when it's parent page/view appears on screen.
		/// </summary>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.SynchronizeIsRevalidatingWithTagProfile();
			this.TagProfile.Changed += this.TagProfile_Changed;

			this.Purposes.Clear();
			this.Purposes.Add(LocalizationResourceManager.Current["PurposePersonal"]);
			this.Purposes.Add(LocalizationResourceManager.Current["PurposeWork"]);
			this.Purposes.Add(LocalizationResourceManager.Current["PurposeEducational"]);
			this.Purposes.Add(LocalizationResourceManager.Current["PurposeExperimental"]);

			if (string.IsNullOrEmpty(this.TagProfile.PhoneNumber))
			{
				try
				{
					object Result = await InternetContent.PostAsync(
						new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
						new KeyValuePair<string, string>("Accept", "application/json"));

					if (Result is Dictionary<string, object> Response &&
						Response.TryGetValue("PhoneCode", out object Obj) && Obj is string PhoneCode)
					{
						this.PhoneNumber = PhoneCode;
					}
					else
						this.PhoneNumber = "+";
				}
				catch (Exception ex)
				{
					this.PhoneNumber = "+";
					this.LogService.LogException(ex);
				}
			}
			else
				this.PhoneNumber = this.TagProfile.PhoneNumber;

			this.EMail = this.TagProfile.EMail;

			this.EvaluateAllCommands();
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;
			return base.OnDispose();
		}

		/// <inheritdoc />
		public override Task DoAssignProperties()
		{
			this.EMailVerificationCode = String.Empty;
			this.PhoneNrVerificationCode = String.Empty;
			this.EMailValidated = false;
			this.PhoneNrValidated = false;

			if (!string.IsNullOrEmpty(this.TagProfile.PhoneNumber))
			{
				this.PhoneNumber = this.TagProfile.PhoneNumber;
				this.EMail = this.TagProfile.EMail;
			}

			this.EvaluateAllCommands();

			return base.DoAssignProperties();
		}

		/// <inheritdoc/>
		protected override void OnStepCompleted(EventArgs e)
		{
			if (this.PhoneNrValidated && this.EMailValidated)
			{
				base.OnStepCompleted(e);
			}
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(
				this.SendAndVerifyEMailCodeCommand, this.SendAndVerifyPhoneNrCodeCommand,
				this.SendPhoneNrCodeCommand, this.VerifyPhoneNrCodeCommand,
				this.SendEMailCodeCommand, this.VerifyEMailCodeCommand);
		}

		private void TagProfile_Changed(object Sender, System.ComponentModel.PropertyChangedEventArgs Args)
		{
			if (Args.PropertyName == nameof(this.TagProfile.LegalIdentity))
				this.SynchronizeIsRevalidatingWithTagProfile();
		}

		private void SynchronizeIsRevalidatingWithTagProfile()
		{
			this.IsRevalidating = this.TagProfile.LegalIdentity is not null;
		}

		#region Properties

		/// <summary>
		/// A BindableProperty for <see cref="IsRevalidating"/> property.
		/// </summary>
		public static readonly BindableProperty IsRevalidatingProperty =
			BindableProperty.Create(nameof(IsRevalidating), typeof(bool), typeof(ValidateContactInfoViewModel), false,
				propertyChanged: (Bindable, OldValue, NewValue) =>
				{
					ValidateContactInfoViewModel ViewModel = (ValidateContactInfoViewModel)Bindable;

					if ((bool)NewValue)
					{
						ViewModel.PurposeRequired = false;
						ViewModel.ValidationAllowed = true;
					}
					else
					{
						ViewModel.PurposeRequired = true;
						ViewModel.ValidationAllowed = false;
					}
				});

		/// <summary>
		/// Gets or sets a value indicating if we are validating contact information for the first time or after revoking an identity.
		/// </summary>
		public bool IsRevalidating
		{
			get => (bool)this.GetValue(IsRevalidatingProperty);
			set => this.SetValue(IsRevalidatingProperty, value);
		}

		/// <summary>
		/// Holds the list of purposes to display.
		/// </summary>
		public ObservableCollection<string> Purposes { get; }

		/// <summary>
		/// See <see cref="PurposeNr"/>
		/// </summary>
		public static readonly BindableProperty PurposeNrProperty =
			BindableProperty.Create(nameof(PurposeNr), typeof(int), typeof(ValidateContactInfoViewModel), -1,
				propertyChanged: (Bindable, OldValue, NewValue) =>
				{
					if ((int)NewValue > -1)
						((ValidateContactInfoViewModel)Bindable).ValidationAllowed = true;
				});

		/// <summary>
		/// Purpose number (zero-based)
		/// </summary>
		public int PurposeNr
		{
			get => (int)this.GetValue(PurposeNrProperty);
			set
			{
				this.SetValue(PurposeNrProperty, value);
				this.IsPersonalPurpose = value == (int)PurposeUse.Personal;
				this.IsWorkPurpose = value == (int)PurposeUse.Work;
				this.IsEducationalPurpose = value == (int)PurposeUse.Educational;
				this.IsExperimentalPurpose = value == (int)PurposeUse.Experimental;
			}
		}

		/// <summary>
		/// See <see cref="IsPersonalPurpose"/>
		/// </summary>
		public static readonly BindableProperty IsPersonalPurposeProperty =
			BindableProperty.Create(nameof(IsPersonalPurpose), typeof(bool), typeof(ValidateContactInfoViewModel), false);

		/// <summary>
		/// If purpose is personal use.
		/// </summary>
		public bool IsPersonalPurpose
		{
			get => (bool)this.GetValue(IsPersonalPurposeProperty);
			set => this.SetValue(IsPersonalPurposeProperty, value);
		}

		/// <summary>
		/// See <see cref="IsWorkPurpose"/>
		/// </summary>
		public static readonly BindableProperty IsWorkPurposeProperty =
			BindableProperty.Create(nameof(IsWorkPurpose), typeof(bool), typeof(ValidateContactInfoViewModel), false);

		/// <summary>
		/// If purpose is personal use.
		/// </summary>
		public bool IsWorkPurpose
		{
			get => (bool)this.GetValue(IsWorkPurposeProperty);
			set => this.SetValue(IsWorkPurposeProperty, value);
		}

		/// <summary>
		/// See <see cref="IsEducationalPurpose"/>
		/// </summary>
		public static readonly BindableProperty IsEducationalPurposeProperty =
			BindableProperty.Create(nameof(IsEducationalPurpose), typeof(bool), typeof(ValidateContactInfoViewModel), false);

		/// <summary>
		/// If purpose is personal use.
		/// </summary>
		public bool IsEducationalPurpose
		{
			get => (bool)this.GetValue(IsEducationalPurposeProperty);
			set => this.SetValue(IsEducationalPurposeProperty, value);
		}

		/// <summary>
		/// See <see cref="IsExperimentalPurpose"/>
		/// </summary>
		public static readonly BindableProperty IsExperimentalPurposeProperty =
			BindableProperty.Create(nameof(IsExperimentalPurpose), typeof(bool), typeof(ValidateContactInfoViewModel), false);

		/// <summary>
		/// If purpose is personal use.
		/// </summary>
		public bool IsExperimentalPurpose
		{
			get => (bool)this.GetValue(IsExperimentalPurposeProperty);
			set => this.SetValue(IsExperimentalPurposeProperty, value);
		}

		/// <summary>
		/// A BindableProperty for <see cref="PurposeRequired"/> property.
		/// </summary>
		public static readonly BindableProperty PurposeRequiredProperty =
			BindableProperty.Create(nameof(PurposeRequired), typeof(bool), typeof(ValidateContactInfoViewModel), true);

		/// <summary>
		/// Gets or sets a value indicating if the user needs to provide a purpose.
		/// </summary>
		public bool PurposeRequired
		{
			get => (bool)this.GetValue(PurposeRequiredProperty);
			set => this.SetValue(PurposeRequiredProperty, value);
		}

		/// <summary>
		/// A BindableProperty for <see cref="ValidationAllowed"/> property.
		/// </summary>
		public static readonly BindableProperty ValidationAllowedProperty =
			BindableProperty.Create(nameof(ValidationAllowed), typeof(bool), typeof(ValidateContactInfoViewModel), false);

		/// <summary>
		/// Gets or sets a value indicating if the user is allowed to validate their contact information at the moment
		/// (or needs to provide a purpose first).
		/// </summary>
		public bool ValidationAllowed
		{
			get => (bool)this.GetValue(ValidationAllowedProperty);
			set => this.SetValue(ValidationAllowedProperty, value);
		}

		/// <summary>
		/// See <see cref="CountEmailSeconds"/>
		/// </summary>
		public static readonly BindableProperty CountEmailSecondsProperty =
			BindableProperty.Create(nameof(CountEmailSeconds), typeof(int), typeof(ValidateContactInfoViewModel), default);

		/// <summary>
		/// how long the email button will stay disabled
		/// </summary>
		public int CountEmailSeconds
		{
			get => (int)this.GetValue(CountEmailSecondsProperty);
			set
			{
				this.SetValue(CountEmailSecondsProperty, value);
				this.OnPropertyChanged(nameof(this.EmailButtonEnabled));
				this.OnPropertyChanged(nameof(this.EmailButtonLabel));
			}
		}

		/// <summary>
		/// See <see cref="CountPhoneSeconds"/>
		/// </summary>
		public static readonly BindableProperty CountPhoneSecondsProperty =
			BindableProperty.Create(nameof(CountPhoneSeconds), typeof(int), typeof(ValidateContactInfoViewModel), default);

		/// <summary>
		/// how long the phone button will stay disabled
		/// </summary>
		public int CountPhoneSeconds
		{
			get => (int)this.GetValue(CountPhoneSecondsProperty);
			set
			{
				this.SetValue(CountPhoneSecondsProperty, value);
				this.OnPropertyChanged(nameof(this.PhoneButtonEnabled));
				this.OnPropertyChanged(nameof(this.PhoneButtonLabel));
			}
		}

		/// <summary>
		/// See <see cref="EMail"/>
		/// </summary>
		public static readonly BindableProperty EMailProperty =
			BindableProperty.Create(nameof(EMail), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// e-mail
		/// </summary>
		public string EMail
		{
			get => (string)this.GetValue(EMailProperty);
			set
			{
				this.SetValue(EMailProperty, value?.Trim() ?? string.Empty);
				this.OnPropertyChanged(nameof(this.EmailButtonEnabled));
			}
		}

		/// <summary>
		/// See <see cref="PhoneNumber"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberProperty =
			BindableProperty.Create(nameof(PhoneNumber), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNumber
		{
			get => (string)this.GetValue(PhoneNumberProperty);
			set
			{
				this.SetValue(PhoneNumberProperty, value);
				this.OnPropertyChanged(nameof(this.PhoneButtonEnabled));
			}
		}

		/// <summary>
		/// See <see cref="EMailValidated"/>
		/// </summary>
		public static readonly BindableProperty EMailValidatedProperty =
			BindableProperty.Create(nameof(EMailValidated), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If e-Mail is valid or not
		/// </summary>
		public bool EMailValidated
		{
			get => (bool)this.GetValue(EMailValidatedProperty);
			set
			{
				this.SetValue(EMailValidatedProperty, value);
				this.OnPropertyChanged(nameof(this.VerifyEmailCodeButtonEnabled));
				this.OnPropertyChanged(nameof(this.VerifyEmailCodeButtonLabel));
			}
		}

		/// <summary>
		/// See <see cref="PhoneNrValidated"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrValidatedProperty =
			BindableProperty.Create(nameof(PhoneNrValidated), typeof(bool), typeof(ValidateContactInfoViewModel), default(bool));

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool PhoneNrValidated
		{
			get => (bool)this.GetValue(PhoneNrValidatedProperty);
			set
			{
				this.SetValue(PhoneNrValidatedProperty, value);
				this.OnPropertyChanged(nameof(this.VerifyPhoneCodeButtonEnabled));
				this.OnPropertyChanged(nameof(this.VerifyPhoneCodeButtonLabel));
			}
		}

		/// <summary>
		/// See <see cref="EMailVerificationCode"/>
		/// </summary>
		public static readonly BindableProperty EMailVerificationCodeProperty =
			BindableProperty.Create(nameof(EMailVerificationCode), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// E-mail verification code
		/// </summary>
		public string EMailVerificationCode
		{
			get => (string)this.GetValue(EMailVerificationCodeProperty);
			set
			{
				this.SetValue(EMailVerificationCodeProperty, value);
				this.OnPropertyChanged(nameof(this.VerifyEmailCodeButtonEnabled));
			}
		}

		/// <summary>
		/// See <see cref="PhoneNrVerificationCode"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrVerificationCodeProperty =
			BindableProperty.Create(nameof(PhoneNrVerificationCode), typeof(string), typeof(ValidateContactInfoViewModel), default(string));

		/// <summary>
		/// Phone number verification code
		/// </summary>
		public string PhoneNrVerificationCode
		{
			get => (string)this.GetValue(PhoneNrVerificationCodeProperty);
			set
			{
				this.SetValue(PhoneNrVerificationCodeProperty, value);
				this.OnPropertyChanged(nameof(this.VerifyPhoneCodeButtonEnabled));
			}
		}

		/// <summary>
		/// If send e-mail code button is disabled or not
		/// </summary>
		public bool EmailButtonEnabled => (this.CountEmailSeconds == 0) && this.IsEMailAdress(this.EMail);

		/// <summary>
		/// If send code Phone button is disabled or not
		/// </summary>
		public bool PhoneButtonEnabled => (this.CountPhoneSeconds == 0) && this.IsInternationalPhoneNumberFormat(this.PhoneNumber);

		/// <summary>
		/// The label of the SendEMailCodeButton
		/// </summary>
		public string EmailButtonLabel
		{
			get
			{
				if (this.CountEmailSeconds > 0)
				{
					return string.Format(LocalizationResourceManager.Current["DisabledFor"], this.CountEmailSeconds);
				}

				return LocalizationResourceManager.Current["SendCode"];
			}
		}

		/// <summary>
		/// The label of the SendPhoneNrCodeButton
		/// </summary>
		public string PhoneButtonLabel
		{
			get
			{
				if (this.CountPhoneSeconds > 0)
				{
					return string.Format(LocalizationResourceManager.Current["DisabledFor"], this.CountPhoneSeconds);
				}

				return LocalizationResourceManager.Current["SendCode"];
			}
		}

		/// <summary>
		/// If verify e-mail code button is enabled.
		/// </summary>
		public bool VerifyEmailCodeButtonEnabled => !this.EMailValidated && this.IsVerificationCode(this.EMailVerificationCode);

		/// <summary>
		/// If verify phone number code button is enabled.
		/// </summary>
		public bool VerifyPhoneCodeButtonEnabled => !this.PhoneNrValidated && this.IsVerificationCode(this.PhoneNrVerificationCode);

		/// <summary>
		/// The label of the VerifyEMailCodeButton
		/// </summary>
		public string VerifyEmailCodeButtonLabel => this.EMailValidated ? LocalizationResourceManager.Current["VerifiedButton"] : LocalizationResourceManager.Current["VerifyCode"];

		/// <summary>
		/// The label of the VerifyPhoneNrCodeButton
		/// </summary>
		public string VerifyPhoneCodeButtonLabel => this.PhoneNrValidated ? LocalizationResourceManager.Current["VerifiedButton"] : LocalizationResourceManager.Current["VerifyCode"];

		/// <summary>
		/// The command to bind to for sending and verification a code to the provided e-mail address.
		/// </summary>
		public ICommand SendAndVerifyEMailCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending a code to the provided e-mail address.
		/// </summary>
		public ICommand SendEMailCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending an e-mail code verification request.
		/// </summary>
		public ICommand VerifyEMailCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending and verification a code to the provided phone number.
		/// </summary>
		public ICommand SendPhoneNrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending a code to the provided phone number.
		/// </summary>
		public ICommand SendAndVerifyPhoneNrCodeCommand { get; }

		/// <summary>
		/// The command to bind to for sending a phone message code verification request.
		/// </summary>
		public ICommand VerifyPhoneNrCodeCommand { get; }

		#endregion

		#region Commands

		#region E-Mail

		private async Task SendAndVerifyEMailCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.EMailValidated = false;
			this.SetIsBusy(this.SendAndVerifyEMailCodeCommand, this.SendEMailCodeCommand, this.VerifyEMailCodeCommand);

			try
			{
				object SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EMail }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (SendResult is Dictionary<string, object> SentResponse &&
					SentResponse.TryGetValue("Status", out object SentObj) && SentObj is bool SentStatus && SentStatus)
				{
					this.StartTimer("email");

					Popups.VerifyCode.VerifyCodePage Page = new(LocalizationResourceManager.Current["SendEmailWarning"]);
					await PopupNavigation.Instance.PushAsync(Page);
					string Code = await Page.Result;

					if (!string.IsNullOrEmpty(Code))
					{
						object VerifyResult = await InternetContent.PostAsync(
							new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
							new Dictionary<string, object>()
							{
						{ "EMail", this.EMail },
						{ "Code", int.Parse(Code) }
							}, new KeyValuePair<string, string>("Accept", "application/json"));

						this.EMailVerificationCode = string.Empty;

						if (VerifyResult is Dictionary<string, object> VerifyResponse &&
							VerifyResponse.TryGetValue("Status", out object VerifyObj) && VerifyObj is bool VerifyStatus && VerifyStatus)
						{
							this.EMailValidated = true;

							this.TagProfile.SetEMail(this.EMail);
							this.OnStepCompleted(EventArgs.Empty);
						}
						else
						{
							this.EMailValidated = false;

							await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
								LocalizationResourceManager.Current["UnableToVerifyCode"], LocalizationResourceManager.Current["Ok"]);
						}
					}
				}
				else
				{
					this.EMailValidated = false;

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SomethingWentWrongWhenSendingEmailCode"]);
				}
			}
			catch (Exception ex)
			{
				this.EMailValidated = false;

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendAndVerifyEMailCodeCommand, this.SendEMailCodeCommand, this.VerifyEMailCodeCommand);
			}
		}

		private async Task SendEMailCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.EMailValidated = false;
			this.SetIsBusy(this.SendEMailCodeCommand);

			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EMail }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status &&
					Status)
				{
					this.StartTimer("email");

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"],
						LocalizationResourceManager.Current["SendEmailWarning"]);
				}
				else
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SomethingWentWrongWhenSendingEmailCode"]);
				}
			}
			catch (Exception ex)
			{
				this.EMailValidated = false;

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendEMailCodeCommand);
			}
		}

		private async Task VerifyEMailCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.SetIsBusy(this.VerifyEMailCodeCommand);

			try
			{
				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "EMail", this.EMail },
						{ "Code", int.Parse(this.EMailVerificationCode) }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				this.EMailVerificationCode = string.Empty;

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status)
				{
					this.EMailValidated = true;

					this.TagProfile.SetEMail(this.EMail);
					this.OnStepCompleted(EventArgs.Empty);
				}
				else
				{
					this.EMailValidated = false;

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
						LocalizationResourceManager.Current["UnableToVerifyCode"], LocalizationResourceManager.Current["Ok"]);
				}
			}
			catch (Exception ex)
			{
				this.EMailValidated = false;

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message,
					LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.VerifyEMailCodeCommand);
			}
		}

		#endregion

		#region Phone Numbers

		private async Task SendAndVerifyPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.PhoneNrValidated = false;
			this.SetIsBusy(this.SendAndVerifyPhoneNrCodeCommand, this.SendPhoneNrCodeCommand, this.VerifyPhoneNrCodeCommand);

			try
			{
				string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);

				object SendResult = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (SendResult is Dictionary<string, object> SendResponse &&
					SendResponse.TryGetValue("Status", out object SendObj) && SendObj is bool SendStatus && SendStatus
					&& SendResponse.TryGetValue("IsTemporary", out SendObj) && SendObj is bool SentIsTemporary)
				{
					if (!string.IsNullOrEmpty(this.TagProfile.PhoneNumber) && !this.TagProfile.TestOtpTimestamp.HasValue && SentIsTemporary)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SwitchingToTestPhoneNumberNotAllowed"]);
					}
					else
					{
						this.StartTimer("phone");

						Popups.VerifyCode.VerifyCodePage Page = new(LocalizationResourceManager.Current["SendPhoneNumberWarning"]);
						await PopupNavigation.Instance.PushAsync(Page);
						string Code = await Page.Result;

						if (!string.IsNullOrEmpty(Code))
						{
							bool IsTest = this.PurposeRequired ? this.IsEducationalPurpose || this.IsExperimentalPurpose : this.TagProfile.IsTest;
							PurposeUse Purpose = this.PurposeRequired ? (PurposeUse)this.PurposeNr : this.TagProfile.Purpose;

							object VerifyResult = await InternetContent.PostAsync(
								new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
								new Dictionary<string, object>()
								{
									{ "Nr", TrimmedNumber },
									{ "Code", int.Parse(Code) },
									{ "Test", IsTest }
								}, new KeyValuePair<string, string>("Accept", "application/json"));

							this.PhoneNrVerificationCode = string.Empty;

							if (VerifyResult is Dictionary<string, object> VerifyResponse &&
								VerifyResponse.TryGetValue("Status", out object VerifyObj) && VerifyObj is bool VerifyStatus && VerifyStatus &&
								VerifyResponse.TryGetValue("Domain", out VerifyObj) && VerifyObj is string VerifyDomain &&
								VerifyResponse.TryGetValue("Key", out VerifyObj) && VerifyObj is string VerifyKey &&
								VerifyResponse.TryGetValue("Secret", out VerifyObj) && VerifyObj is string VerifySecret &&
								VerifyResponse.TryGetValue("Temporary", out VerifyObj) && VerifyObj is bool VerifyIsTemporary)
							{
								this.PhoneNrValidated = true;

								this.TagProfile.SetPhone(TrimmedNumber);
								this.TagProfile.SetPurpose(IsTest, Purpose);
								this.TagProfile.SetTestOtpTimestamp(VerifyIsTemporary ? DateTime.Now : null);

								if (this.IsRevalidating)
									await this.TagProfile.RevalidateContactInfo();
								else
								{
									bool DefaultConnectivity;
									try
									{
										(string HostName, int PortNumber, bool IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(VerifyDomain);
										DefaultConnectivity = HostName == VerifyDomain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
									}
									catch (Exception)
									{
										DefaultConnectivity = false;
									}

									await this.TagProfile.SetDomain(VerifyDomain, DefaultConnectivity, VerifyKey, VerifySecret);
								}

								this.OnStepCompleted(EventArgs.Empty);
							}
							else
							{
								this.PhoneNrValidated = false;

								await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
									LocalizationResourceManager.Current["UnableToVerifyCode"], LocalizationResourceManager.Current["Ok"]);
							}
						}
					}
				}
				else
				{
					this.PhoneNrValidated = false;

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SomethingWentWrongWhenSendingPhoneCode"]);
				}
			}
			catch (Exception ex)
			{
				this.PhoneNrValidated = false;

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendAndVerifyPhoneNrCodeCommand, this.SendPhoneNrCodeCommand, this.VerifyPhoneNrCodeCommand);
			}
		}

		private async Task SendPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.PhoneNrValidated = false;
			this.SetIsBusy(this.SendPhoneNrCodeCommand);

			try
			{
				string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);

				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status
					&& Response.TryGetValue("IsTemporary", out Obj) && Obj is bool IsTemporary)
				{
					if (!string.IsNullOrEmpty(this.TagProfile.PhoneNumber) && !this.TagProfile.TestOtpTimestamp.HasValue && IsTemporary)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SwitchingToTestPhoneNumberNotAllowed"]);
					}
					else
					{
						this.StartTimer("phone");

						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"],
							LocalizationResourceManager.Current["SendPhoneNumberWarning"]);
					}
				}
				else
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SomethingWentWrongWhenSendingPhoneCode"]);
				}
			}
			catch (Exception ex)
			{
				this.PhoneNrValidated = false;

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendPhoneNrCodeCommand);
			}
		}

		private async Task VerifyPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.SetIsBusy(this.VerifyPhoneNrCodeCommand);

			try
			{
				string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);

				bool IsTest = this.PurposeRequired ? this.IsEducationalPurpose || this.IsExperimentalPurpose : this.TagProfile.IsTest;
				PurposeUse Purpose = this.PurposeRequired ? (PurposeUse)this.PurposeNr : this.TagProfile.Purpose;

				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber },
						{ "Code", int.Parse(this.PhoneNrVerificationCode) },
						{ "Test", IsTest }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				this.PhoneNrVerificationCode = string.Empty;

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status &&
					Response.TryGetValue("Domain", out Obj) && Obj is string Domain &&
					Response.TryGetValue("Key", out Obj) && Obj is string Key &&
					Response.TryGetValue("Secret", out Obj) && Obj is string Secret &&
					Response.TryGetValue("Temporary", out Obj) && Obj is bool IsTemporary)
				{
					this.PhoneNrValidated = true;

					this.TagProfile.SetPhone(TrimmedNumber);
					this.TagProfile.SetPurpose(IsTest, Purpose);
					this.TagProfile.SetTestOtpTimestamp(IsTemporary ? DateTime.Now : null);

					if (this.IsRevalidating)
						await this.TagProfile.RevalidateContactInfo();
					else
					{
						bool DefaultConnectivity;
						try
						{
							(string HostName, int PortNumber, bool IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(Domain);
							DefaultConnectivity = HostName == Domain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
						}
						catch (Exception)
						{
							DefaultConnectivity = false;
						}

						await this.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
					}

					this.OnStepCompleted(EventArgs.Empty);
				}
				else
				{
					this.PhoneNrValidated = false;

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToVerifyCode"], LocalizationResourceManager.Current["Ok"]);
				}
			}
			catch (Exception ex)
			{
				this.PhoneNrValidated = false;

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.VerifyPhoneNrCodeCommand);
			}
		}

		#endregion

		#endregion

		#region CanExecute

		private bool SendAndVerifyEMailCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.EmailButtonEnabled;

		}

		private bool SendAndVerifyPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.PhoneButtonEnabled;

		}

		private bool SendEMailCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.EmailButtonEnabled;
		}

		private bool SendPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.PhoneButtonEnabled;
		}

		private bool VerifyEMailCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.VerifyEmailCodeButtonEnabled;
		}

		private bool VerifyPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.VerifyPhoneCodeButtonEnabled;
		}

		#endregion

		#region Syntax

		private string TrimPhoneNumber(string PhoneNr)
		{
			return PhoneNr.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
		}

		private bool IsInternationalPhoneNumberFormat(string PhoneNr)
		{
			if (string.IsNullOrEmpty(PhoneNr))
				return false;

			string phoneNumber = this.TrimPhoneNumber(PhoneNr);
			return internationalPhoneNr.IsMatch(phoneNumber);
		}

		private bool IsVerificationCode(string Code)
		{
			return !string.IsNullOrEmpty(Code) && verificationCode.IsMatch(Code);
		}


		private bool IsEMailAdress(string EMailAddress)
		{
			if (string.IsNullOrEmpty(EMailAddress))
				return false;

			return emailAddress.IsMatch(EMailAddress);
		}

		private void StartTimer(string type)
		{
			if (type == "email")
			{
				this.CountEmailSeconds = 30;

				Device.StartTimer(TimeSpan.FromSeconds(1), () =>
				{
					if (this.CountEmailSeconds > 0)
					{
						this.CountEmailSeconds--;
						return true;
					}
					else
					{
						return false;
					}
				});
			}
			else
			{
				this.CountPhoneSeconds = 30;

				Device.StartTimer(TimeSpan.FromSeconds(1), () =>
				{
					if (this.CountPhoneSeconds > 0)
					{
						this.CountPhoneSeconds--;
						return true;
					}
					else
					{
						return false;
					}
				});
			}
		}

		private static readonly Regex internationalPhoneNr = new(@"^\+[1-9]\d{4,}$", RegexOptions.Singleline);
		private static readonly Regex verificationCode = new(@"^[1-9]\d{5}$", RegexOptions.Singleline);
		private static readonly Regex emailAddress = new(@"^[\w\d](\w|\d|[_\.-][\w\d])*@(\w|\d|[\.-][\w\d]+)+$", RegexOptions.Singleline);

		#endregion
	}
}
