using IdApp.Extensions;
using IdApp.Services;
using IdApp.Pages.Identity.TransferIdentity;
using IdApp.Services.UI.Photos;
using IdApp.Services.Data.Countries;
using IdApp.Pages.Main.Security;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP;
using System.ComponentModel;
using System;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;
using Waher.Persistence;
using Xamarin.Essentials;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Text;
using Waher.Content.Xml;
using System.Xml;

namespace IdApp.Pages.Identity.ViewIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public class ViewIdentityViewModel : QrXmppViewModel
	{
		private readonly PhotosLoader photosLoader;
		private LegalIdentity requestorIdentity;
		private string requestorFullJid;
		private string signatoryIdentityId;
		private string petitionId;
		private string purpose;
		private byte[] contentToSign;

		/// <summary>
		/// Creates an instance of the <see cref="ViewIdentityViewModel"/> class.
		/// </summary>
		public ViewIdentityViewModel()
			: base()
		{
			this.Photos = new ObservableCollection<Photo>();
			this.photosLoader = new PhotosLoader(this.Photos);

			this.ApproveCommand = new Command(async _ => await this.Approve(), _ => this.IsConnected);
			this.RejectCommand = new Command(async _ => await this.Reject(), _ => this.IsConnected);
			this.RevokeCommand = new Command(async _ => await this.Revoke(), _ => this.IsConnected);
			this.TransferCommand = new Command(async _ => await this.Transfer(), _ => this.IsConnected);
			this.CompromiseCommand = new Command(async _ => await this.Compromise(), _ => this.IsConnected);
			this.ChangePinCommand = new Command(async _ => await this.ChangePin(), _ => this.IsConnected);
			this.CopyCommand = new Command(Item => this.CopyToClipboard(Item));
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ViewIdentityNavigationArgs args))
			{
				this.LegalIdentity = args.Identity ?? this.TagProfile.LegalIdentity;
				this.requestorIdentity = args.RequestorIdentity;
				this.requestorFullJid = args.RequestorFullJid;
				this.signatoryIdentityId = args.SignatoryIdentityId;
				this.petitionId = args.PetitionId;
				this.purpose = args.Purpose;
				this.contentToSign = args.ContentToSign;
			}

			if (this.LegalIdentity is null)
			{
				this.LegalIdentity = this.TagProfile.LegalIdentity;
				this.requestorIdentity = null;
				this.requestorFullJid = null;
				this.signatoryIdentityId = null;
				this.petitionId = null;
				this.purpose = null;
				this.contentToSign = null;
				this.IsPersonal = true;
			}
			else
				this.IsPersonal = false;

			this.AssignProperties();

			if (this.ThirdParty)
			{
				ContactInfo Info = await ContactInfo.FindByBareJid(this.BareJid);

				if ((Info is not null) &&
					(Info.LegalIdentity is null ||
					(Info.LegalId != this.LegalId &&
					Info.LegalIdentity.Created < this.LegalIdentity.Created &&
					this.LegalIdentity.State == IdentityState.Approved)))
				{
					Info.LegalId = this.LegalId;
					Info.LegalIdentity = this.LegalIdentity;
					Info.FriendlyName = ContactInfo.GetFriendlyName(this.LegalIdentity);

					await Database.Update(Info);
					await Database.Provider.Flush();
				}
			}

			this.EvaluateAllCommands();

			this.TagProfile.Changed += this.TagProfile_Changed;
			this.XmppService.LegalIdentityChanged += this.SmartContracts_LegalIdentityChanged;
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			this.photosLoader.CancelLoadPhotos();

			this.TagProfile.Changed -= this.TagProfile_Changed;
			this.XmppService.LegalIdentityChanged -= this.SmartContracts_LegalIdentityChanged;

			this.LegalIdentity = null;

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// Holds a list of photos associated with this identity.
		/// </summary>
		public ObservableCollection<Photo> Photos { get; }

		/// <summary>
		/// The command to bind to for approving an identity
		/// </summary>
		public ICommand ApproveCommand { get; }

		/// <summary>
		/// The command to bind to for rejecting an identity
		/// </summary>
		public ICommand RejectCommand { get; }

		/// <summary>
		/// The command to bind to for changing PIN.
		/// </summary>
		public ICommand ChangePinCommand { get; }

		/// <summary>
		/// The command to bind to for flagging an identity as compromised.
		/// </summary>
		public ICommand CompromiseCommand { get; }

		/// <summary>
		/// The command to bind to for revoking an identity
		/// </summary>
		public ICommand RevokeCommand { get; }

		/// <summary>
		/// The command to bind to for transferring an identity
		/// </summary>
		public ICommand TransferCommand { get; }

		/// <summary>
		/// The command for copying data to clipboard.
		/// </summary>
		public ICommand CopyCommand { get; }

		#endregion

		private void AssignProperties()
		{
			this.Created = this.LegalIdentity?.Created ?? DateTime.MinValue;
			this.Updated = this.LegalIdentity?.Updated.GetDateOrNullIfMinValue();
			this.LegalId = this.LegalIdentity?.Id;

			if (this.requestorIdentity is not null)
				this.BareJid = this.requestorIdentity.GetJid(Constants.NotAvailableValue);
			else if (this.LegalIdentity is not null)
				this.BareJid = this.LegalIdentity.GetJid(Constants.NotAvailableValue);
			else
				this.BareJid = Constants.NotAvailableValue;

			if (this.LegalIdentity?.ClientPubKey is not null)
				this.PublicKey = Convert.ToBase64String(this.LegalIdentity.ClientPubKey);
			else
				this.PublicKey = string.Empty;

			this.State = this.LegalIdentity?.State ?? IdentityState.Rejected;
			this.From = this.LegalIdentity?.From.GetDateOrNullIfMinValue();
			this.To = this.LegalIdentity?.To.GetDateOrNullIfMinValue();

			if (this.LegalIdentity is not null)
			{
				this.FirstName = this.LegalIdentity[Constants.XmppProperties.FirstName];
				this.MiddleNames = this.LegalIdentity[Constants.XmppProperties.MiddleName];
				this.LastNames = this.LegalIdentity[Constants.XmppProperties.LastName];
				this.PersonalNumber = this.LegalIdentity[Constants.XmppProperties.PersonalNumber];
				this.Address = this.LegalIdentity[Constants.XmppProperties.Address];
				this.Address2 = this.LegalIdentity[Constants.XmppProperties.Address2];
				this.ZipCode = this.LegalIdentity[Constants.XmppProperties.ZipCode];
				this.Area = this.LegalIdentity[Constants.XmppProperties.Area];
				this.City = this.LegalIdentity[Constants.XmppProperties.City];
				this.Region = this.LegalIdentity[Constants.XmppProperties.Region];
				this.CountryCode = this.LegalIdentity[Constants.XmppProperties.Country];
				this.PhoneNr = this.LegalIdentity[Constants.XmppProperties.Phone];
				this.EMail = this.LegalIdentity[Constants.XmppProperties.EMail];
			}
			else
			{
				this.FirstName = string.Empty;
				this.MiddleNames = string.Empty;
				this.LastNames = string.Empty;
				this.PersonalNumber = string.Empty;
				this.Address = string.Empty;
				this.Address2 = string.Empty;
				this.ZipCode = string.Empty;
				this.Area = string.Empty;
				this.City = string.Empty;
				this.Region = string.Empty;
				this.CountryCode = string.Empty;
				this.PhoneNr = string.Empty;
				this.EMail = string.Empty;
			}

			this.Country = ISO_3166_1.ToName(this.CountryCode);
			this.IsApproved = this.LegalIdentity?.State == IdentityState.Approved;
			this.IsCreated = this.LegalIdentity?.State == IdentityState.Created;

			this.IsForReview = this.requestorIdentity is not null;
			this.IsNotForReview = !this.IsForReview;
			this.ThirdParty = (this.LegalIdentity is not null) && !this.IsPersonal;

			this.IsForReviewFirstName = !string.IsNullOrWhiteSpace(this.FirstName) && this.IsForReview;
			this.IsForReviewMiddleNames = !string.IsNullOrWhiteSpace(this.MiddleNames) && this.IsForReview;
			this.IsForReviewLastNames = !string.IsNullOrWhiteSpace(this.LastNames) && this.IsForReview;
			this.IsForReviewPersonalNumber = !string.IsNullOrWhiteSpace(this.PersonalNumber) && this.IsForReview;
			this.IsForReviewAddress = !string.IsNullOrWhiteSpace(this.Address) && this.IsForReview;
			this.IsForReviewAddress2 = !string.IsNullOrWhiteSpace(this.Address2) && this.IsForReview;
			this.IsForReviewCity = !string.IsNullOrWhiteSpace(this.City) && this.IsForReview;
			this.IsForReviewZipCode = !string.IsNullOrWhiteSpace(this.ZipCode) && this.IsForReview;
			this.IsForReviewArea = !string.IsNullOrWhiteSpace(this.Area) && this.IsForReview;
			this.IsForReviewRegion = !string.IsNullOrWhiteSpace(this.Region) && this.IsForReview;
			this.IsForReviewCountry = !string.IsNullOrWhiteSpace(this.Country) && this.IsForReview;

			// QR
			if (this.LegalIdentity is null)
				this.RemoveQrCode();
			else
				this.GenerateQrCode(Constants.UriSchemes.CreateIdUri(this.LegalIdentity.Id));

			if (this.IsConnected)
				this.ReloadPhotos();
		}

		/// <summary>
		/// Copies Item to clipboard
		/// </summary>
		private async void CopyToClipboard(object Item)
		{
			try
			{
				if (Item is string Label)
				{
					if (Label == this.LegalId)
					{
						await Clipboard.SetTextAsync(Constants.UriSchemes.UriSchemeIotId + ":" + this.LegalId);
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["IdCopiedSuccessfully"]);
					}
					else
					{
						await Clipboard.SetTextAsync(Label);
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"], LocalizationResourceManager.Current["TagValueCopiedToClipboard"]);
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.ApproveCommand, this.RejectCommand, this.RevokeCommand, this.TransferCommand,
				this.ChangePinCommand, this.CompromiseCommand);
		}

		/// <inheritdoc/>
		protected override Task XmppService_ConnectionStateChanged(object Sender, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				this.SetConnectionStateAndText(NewState);
				this.EvaluateAllCommands();
				if (this.IsConnected)
				{
					await Task.Delay(Constants.Timeouts.XmppInit);
					this.ReloadPhotos();
				}
			});

			return Task.CompletedTask;
		}

		private async void ReloadPhotos()
		{
			try
			{
				this.photosLoader.CancelLoadPhotos();

				Attachment[] Attachments;

				if (this.requestorIdentity?.Attachments is not null)
					Attachments = this.requestorIdentity.Attachments;
				else
					Attachments = this.LegalIdentity?.Attachments;

				if (Attachments is not null)
				{
					Photo First = await this.photosLoader.LoadPhotos(Attachments, SignWith.LatestApprovedIdOrCurrentKeys);

					this.FirstPhotoSource = First?.Source;
					this.FirstPhotoRotation = First?.Rotation ?? 0;
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
		}

		private Task SmartContracts_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				if (this.LegalIdentity?.Id == e.Identity.Id)
				{
					this.LegalIdentity = e.Identity;
					this.AssignProperties();
				}
			});

			return Task.CompletedTask;
		}

		#region Properties

		/// <summary>
		/// See <see cref="Created"/>
		/// </summary>
		public static readonly BindableProperty CreatedProperty =
			BindableProperty.Create(nameof(Created), typeof(DateTime), typeof(ViewIdentityViewModel), default(DateTime));

		/// <summary>
		/// Created time stamp of the identity
		/// </summary>
		public DateTime Created
		{
			get => (DateTime)this.GetValue(CreatedProperty);
			set => this.SetValue(CreatedProperty, value);
		}

		/// <summary>
		/// See <see cref="Updated"/>
		/// </summary>
		public static readonly BindableProperty UpdatedProperty =
			BindableProperty.Create(nameof(Updated), typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

		/// <summary>
		/// Updated time stamp of the identity
		/// </summary>
		public DateTime? Updated
		{
			get { return (DateTime?)this.GetValue(UpdatedProperty); }
			set => this.SetValue(UpdatedProperty, value);
		}

		/// <summary>
		/// See <see cref="LegalId"/>
		/// </summary>
		public static readonly BindableProperty LegalIdProperty =
			BindableProperty.Create(nameof(LegalId), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Legal id of the identity
		/// </summary>
		public string LegalId
		{
			get => (string)this.GetValue(LegalIdProperty);
			set => this.SetValue(LegalIdProperty, value);
		}

		/// <summary>
		/// The full legal identity of the identity
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// See <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create(nameof(BareJid), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Bare Jid of the identity
		/// </summary>
		public string BareJid
		{
			get => (string)this.GetValue(BareJidProperty);
			set => this.SetValue(BareJidProperty, value);
		}

		/// <summary>
		/// See <see cref="PublicKey"/>
		/// </summary>
		public static readonly BindableProperty PublicKeyProperty =
			BindableProperty.Create(nameof(PublicKey), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Public key of the identity's signature.
		/// </summary>
		public string PublicKey
		{
			get => (string)this.GetValue(PublicKeyProperty);
			set => this.SetValue(PublicKeyProperty, value);
		}

		/// <summary>
		/// See <see cref="State"/>
		/// </summary>
		public static readonly BindableProperty StateProperty =
			BindableProperty.Create(nameof(State), typeof(IdentityState), typeof(ViewIdentityViewModel), default(IdentityState));

		/// <summary>
		/// Current state of the identity
		/// </summary>
		public IdentityState State
		{
			get => (IdentityState)this.GetValue(StateProperty);
			set => this.SetValue(StateProperty, value);
		}

		/// <summary>
		/// See <see cref="From"/>
		/// </summary>
		public static readonly BindableProperty FromProperty =
			BindableProperty.Create(nameof(From), typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

		/// <summary>
		/// From date (validity range) of the identity
		/// </summary>
		public DateTime? From
		{
			get { return (DateTime?)this.GetValue(FromProperty); }
			set => this.SetValue(FromProperty, value);
		}

		/// <summary>
		/// See <see cref="To"/>
		/// </summary>
		public static readonly BindableProperty ToProperty =
			BindableProperty.Create(nameof(To), typeof(DateTime?), typeof(ViewIdentityViewModel), default(DateTime?));

		/// <summary>
		/// To date (validity range) of the identity
		/// </summary>
		public DateTime? To
		{
			get { return (DateTime?)this.GetValue(ToProperty); }
			set => this.SetValue(ToProperty, value);
		}

		/// <summary>
		/// See <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create(nameof(FirstName), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// First name of the identity
		/// </summary>
		public string FirstName
		{
			get => (string)this.GetValue(FirstNameProperty);
			set => this.SetValue(FirstNameProperty, value);
		}

		/// <summary>
		/// See <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Middle name(s) of the identity
		/// </summary>
		public string MiddleNames
		{
			get => (string)this.GetValue(MiddleNamesProperty);
			set => this.SetValue(MiddleNamesProperty, value);
		}

		/// <summary>
		/// See <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create(nameof(LastNames), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Last name(s) of the identity
		/// </summary>
		public string LastNames
		{
			get => (string)this.GetValue(LastNamesProperty);
			set => this.SetValue(LastNamesProperty, value);
		}

		/// <summary>
		/// See <see cref="PersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberProperty =
			BindableProperty.Create(nameof(PersonalNumber), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Personal number of the identity
		/// </summary>
		public string PersonalNumber
		{
			get => (string)this.GetValue(PersonalNumberProperty);
			set => this.SetValue(PersonalNumberProperty, value);
		}

		/// <summary>
		/// See <see cref="Address"/>
		/// </summary>
		public static readonly BindableProperty AddressProperty =
			BindableProperty.Create(nameof(Address), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Address of the identity
		/// </summary>
		public string Address
		{
			get => (string)this.GetValue(AddressProperty);
			set => this.SetValue(AddressProperty, value);
		}

		/// <summary>
		/// See <see cref="Address2"/>
		/// </summary>
		public static readonly BindableProperty Address2Property =
			BindableProperty.Create(nameof(Address2), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Address (line 2) of the identity
		/// </summary>
		public string Address2
		{
			get => (string)this.GetValue(Address2Property);
			set => this.SetValue(Address2Property, value);
		}

		/// <summary>
		/// See <see cref="ZipCode"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeProperty =
			BindableProperty.Create(nameof(ZipCode), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Zip code of the identity
		/// </summary>
		public string ZipCode
		{
			get => (string)this.GetValue(ZipCodeProperty);
			set => this.SetValue(ZipCodeProperty, value);
		}

		/// <summary>
		/// See <see cref="Area"/>
		/// </summary>
		public static readonly BindableProperty AreaProperty =
			BindableProperty.Create(nameof(Area), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Area of the identity
		/// </summary>
		public string Area
		{
			get => (string)this.GetValue(AreaProperty);
			set => this.SetValue(AreaProperty, value);
		}

		/// <summary>
		/// See <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create(nameof(City), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// City of the identity
		/// </summary>
		public string City
		{
			get => (string)this.GetValue(CityProperty);
			set => this.SetValue(CityProperty, value);
		}

		/// <summary>
		/// See <see cref="Region"/>
		/// </summary>
		public static readonly BindableProperty RegionProperty =
			BindableProperty.Create(nameof(Region), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Region of the identity
		/// </summary>
		public string Region
		{
			get => (string)this.GetValue(RegionProperty);
			set => this.SetValue(RegionProperty, value);
		}

		/// <summary>
		/// See <see cref="Country"/>
		/// </summary>
		public static readonly BindableProperty CountryProperty =
			BindableProperty.Create(nameof(Country), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country of the identity
		/// </summary>
		public string Country
		{
			get => (string)this.GetValue(CountryProperty);
			set => this.SetValue(CountryProperty, value);
		}

		/// <summary>
		/// See <see cref="CountryCode"/>
		/// </summary>
		public static readonly BindableProperty CountryCodeProperty =
			BindableProperty.Create(nameof(CountryCode), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country code of the identity
		/// </summary>
		public string CountryCode
		{
			get => (string)this.GetValue(CountryCodeProperty);
			set => this.SetValue(CountryCodeProperty, value);
		}

		/// <summary>
		/// See <see cref="PhoneNr"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrProperty =
			BindableProperty.Create(nameof(PhoneNr), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country code of the identity
		/// </summary>
		public string PhoneNr
		{
			get => (string)this.GetValue(PhoneNrProperty);
			set => this.SetValue(PhoneNrProperty, value);
		}

		/// <summary>
		/// See <see cref="EMail"/>
		/// </summary>
		public static readonly BindableProperty EMailProperty =
			BindableProperty.Create(nameof(EMail), typeof(string), typeof(ViewIdentityViewModel), default(string));

		/// <summary>
		/// Country code of the identity
		/// </summary>
		public string EMail
		{
			get => (string)this.GetValue(EMailProperty);
			set => this.SetValue(EMailProperty, value);
		}

		/// <summary>
		/// See <see cref="IsApproved"/>
		/// </summary>
		public static readonly BindableProperty IsApprovedProperty =
			BindableProperty.Create(nameof(IsApproved), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is approved or not.
		/// </summary>
		public bool IsApproved
		{
			get => (bool)this.GetValue(IsApprovedProperty);
			set => this.SetValue(IsApprovedProperty, value);
		}

		/// <summary>
		/// See <see cref="IsCreated"/>
		/// </summary>
		public static readonly BindableProperty IsCreatedProperty =
			BindableProperty.Create(nameof(IsCreated), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets created state of the identity, i.e. if it has been created or not.
		/// </summary>
		public bool IsCreated
		{
			get => (bool)this.GetValue(IsCreatedProperty);
			set => this.SetValue(IsCreatedProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReview"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewProperty =
			BindableProperty.Create(nameof(IsForReview), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsNotForReview"/>.
		/// </summary>
		public bool IsForReview
		{
			get => (bool)this.GetValue(IsForReviewProperty);
			set => this.SetValue(IsForReviewProperty, value);
		}

		/// <summary>
		/// See <see cref="IsNotForReview"/>
		/// </summary>
		public static readonly BindableProperty IsNotForReviewProperty =
			BindableProperty.Create(nameof(IsNotForReview), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool IsNotForReview
		{
			get => (bool)this.GetValue(IsNotForReviewProperty);
			set => this.SetValue(IsNotForReviewProperty, value);
		}

		/// <summary>
		/// See <see cref="ThirdParty"/>
		/// </summary>
		public static readonly BindableProperty ThirdPartyProperty =
			BindableProperty.Create(nameof(ThirdParty), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is for review or not. This property has its inverse in <see cref="IsForReview"/>.
		/// </summary>
		public bool ThirdParty
		{
			get => (bool)this.GetValue(ThirdPartyProperty);
			set => this.SetValue(ThirdPartyProperty, value);
		}

		/// <summary>
		/// See <see cref="IsPersonal"/>
		/// </summary>
		public static readonly BindableProperty IsPersonalProperty =
			BindableProperty.Create(nameof(IsPersonal), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the identity is a personal identity.
		/// </summary>
		public bool IsPersonal
		{
			get => (bool)this.GetValue(IsPersonalProperty);
			set => this.SetValue(IsPersonalProperty, value);
		}

		/// <summary>
		/// See <see cref="FirstNameIsChecked"/>
		/// </summary>
		public static readonly BindableProperty FirstNameIsCheckedProperty =
			BindableProperty.Create(nameof(FirstNameIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="FirstName"/> property is checked (when being reviewed)
		/// </summary>
		public bool FirstNameIsChecked
		{
			get => (bool)this.GetValue(FirstNameIsCheckedProperty);
			set => this.SetValue(FirstNameIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="MiddleNamesIsChecked"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesIsCheckedProperty =
			BindableProperty.Create(nameof(MiddleNamesIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="MiddleNames"/> property is checked (when being reviewed)
		/// </summary>
		public bool MiddleNamesIsChecked
		{
			get => (bool)this.GetValue(MiddleNamesIsCheckedProperty);
			set => this.SetValue(MiddleNamesIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="LastNamesIsChecked"/>
		/// </summary>
		public static readonly BindableProperty LastNamesIsCheckedProperty =
			BindableProperty.Create(nameof(LastNamesIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="LastNames"/> property is checked (when being reviewed)
		/// </summary>
		public bool LastNamesIsChecked
		{
			get => (bool)this.GetValue(LastNamesIsCheckedProperty);
			set => this.SetValue(LastNamesIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="PersonalNumberIsChecked"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberIsCheckedProperty =
			BindableProperty.Create(nameof(PersonalNumberIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="PersonalNumber"/> property is checked (when being reviewed)
		/// </summary>
		public bool PersonalNumberIsChecked
		{
			get => (bool)this.GetValue(PersonalNumberIsCheckedProperty);
			set => this.SetValue(PersonalNumberIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="AddressIsChecked"/>
		/// </summary>
		public static readonly BindableProperty AddressIsCheckedProperty =
			BindableProperty.Create(nameof(AddressIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address"/> property is checked (when being reviewed)
		/// </summary>
		public bool AddressIsChecked
		{
			get => (bool)this.GetValue(AddressIsCheckedProperty);
			set => this.SetValue(AddressIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="AddressIsChecked"/>
		/// </summary>
		public static readonly BindableProperty Address2IsCheckedProperty =
			BindableProperty.Create(nameof(Address2IsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address2"/> property is checked (when being reviewed)
		/// </summary>
		public bool Address2IsChecked
		{
			get => (bool)this.GetValue(Address2IsCheckedProperty);
			set => this.SetValue(Address2IsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="ZipCodeIsChecked"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeIsCheckedProperty =
			BindableProperty.Create(nameof(ZipCodeIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="ZipCode"/> property is checked (when being reviewed)
		/// </summary>
		public bool ZipCodeIsChecked
		{
			get => (bool)this.GetValue(ZipCodeIsCheckedProperty);
			set => this.SetValue(ZipCodeIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="AreaIsChecked"/>
		/// </summary>
		public static readonly BindableProperty AreaIsCheckedProperty =
			BindableProperty.Create(nameof(AreaIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Area"/> property is checked (when being reviewed)
		/// </summary>
		public bool AreaIsChecked
		{
			get => (bool)this.GetValue(AreaIsCheckedProperty);
			set => this.SetValue(AreaIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="CityIsChecked"/>
		/// </summary>
		public static readonly BindableProperty CityIsCheckedProperty =
			BindableProperty.Create(nameof(CityIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="City"/> property is checked (when being reviewed)
		/// </summary>
		public bool CityIsChecked
		{
			get => (bool)this.GetValue(CityIsCheckedProperty);
			set => this.SetValue(CityIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="RegionIsChecked"/>
		/// </summary>
		public static readonly BindableProperty RegionIsCheckedProperty =
			BindableProperty.Create(nameof(RegionIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Region"/> property is checked (when being reviewed)
		/// </summary>
		public bool RegionIsChecked
		{
			get => (bool)this.GetValue(RegionIsCheckedProperty);
			set => this.SetValue(RegionIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="CountryCodeIsChecked"/>
		/// </summary>
		public static readonly BindableProperty CountryCodeIsCheckedProperty =
			BindableProperty.Create(nameof(CountryCodeIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="CountryCode"/> property is checked (when being reviewed)
		/// </summary>
		public bool CountryCodeIsChecked
		{
			get => (bool)this.GetValue(CountryCodeIsCheckedProperty);
			set => this.SetValue(CountryCodeIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="CarefulReviewIsChecked"/>
		/// </summary>
		public static readonly BindableProperty CarefulReviewIsCheckedProperty =
			BindableProperty.Create(nameof(CarefulReviewIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the Careful Review property is checked (when being reviewed)
		/// </summary>
		public bool CarefulReviewIsChecked
		{
			get => (bool)this.GetValue(CarefulReviewIsCheckedProperty);
			set => this.SetValue(CarefulReviewIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="ApprovePiiIsChecked"/>
		/// </summary>
		public static readonly BindableProperty ApprovePiiIsCheckedProperty =
			BindableProperty.Create(nameof(ApprovePiiIsChecked), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the ApprovePii property is checked (when being reviewed)
		/// </summary>
		public bool ApprovePiiIsChecked
		{
			get => (bool)this.GetValue(ApprovePiiIsCheckedProperty);
			set => this.SetValue(ApprovePiiIsCheckedProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewFirstName"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewFirstNameProperty =
			BindableProperty.Create(nameof(IsForReviewFirstName), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="FirstName"/> property is for review.
		/// </summary>
		public bool IsForReviewFirstName
		{
			get => (bool)this.GetValue(IsForReviewFirstNameProperty);
			set => this.SetValue(IsForReviewFirstNameProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewMiddleNames"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewMiddleNamesProperty =
			BindableProperty.Create(nameof(IsForReviewMiddleNames), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="MiddleNames"/> property is for review.
		/// </summary>
		public bool IsForReviewMiddleNames
		{
			get => (bool)this.GetValue(IsForReviewMiddleNamesProperty);
			set => this.SetValue(IsForReviewMiddleNamesProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewLastNames"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewLastNamesProperty =
			BindableProperty.Create(nameof(IsForReviewLastNames), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="LastNames"/> property is for review.
		/// </summary>
		public bool IsForReviewLastNames
		{
			get => (bool)this.GetValue(IsForReviewLastNamesProperty);
			set => this.SetValue(IsForReviewLastNamesProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewPersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewPersonalNumberProperty =
			BindableProperty.Create(nameof(IsForReviewPersonalNumber), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="PersonalNumber"/> property is for review.
		/// </summary>
		public bool IsForReviewPersonalNumber
		{
			get => (bool)this.GetValue(IsForReviewPersonalNumberProperty);
			set => this.SetValue(IsForReviewPersonalNumberProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewAddress"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewAddressProperty =
			BindableProperty.Create(nameof(IsForReviewAddress), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address"/> property is for review.
		/// </summary>
		public bool IsForReviewAddress
		{
			get => (bool)this.GetValue(IsForReviewAddressProperty);
			set => this.SetValue(IsForReviewAddressProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewAddress2"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewAddress2Property =
			BindableProperty.Create(nameof(IsForReviewAddress2), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Address2"/> property is for review.
		/// </summary>
		public bool IsForReviewAddress2
		{
			get => (bool)this.GetValue(IsForReviewAddress2Property);
			set => this.SetValue(IsForReviewAddress2Property, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewCity"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewCityProperty =
			BindableProperty.Create(nameof(IsForReviewCity), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="City"/> property is for review.
		/// </summary>
		public bool IsForReviewCity
		{
			get => (bool)this.GetValue(IsForReviewCityProperty);
			set => this.SetValue(IsForReviewCityProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewZipCode"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewZipCodeProperty =
			BindableProperty.Create(nameof(IsForReviewZipCode), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="ZipCode"/> property is for review.
		/// </summary>
		public bool IsForReviewZipCode
		{
			get => (bool)this.GetValue(IsForReviewZipCodeProperty);
			set => this.SetValue(IsForReviewZipCodeProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewArea"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewAreaProperty =
			BindableProperty.Create(nameof(IsForReviewArea), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Area"/> property is for review.
		/// </summary>
		public bool IsForReviewArea
		{
			get => (bool)this.GetValue(IsForReviewAreaProperty);
			set => this.SetValue(IsForReviewAreaProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewRegion"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewRegionProperty =
			BindableProperty.Create(nameof(IsForReviewRegion), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Region"/> property is for review.
		/// </summary>
		public bool IsForReviewRegion
		{
			get => (bool)this.GetValue(IsForReviewRegionProperty);
			set => this.SetValue(IsForReviewRegionProperty, value);
		}

		/// <summary>
		/// See <see cref="IsForReviewCountry"/>
		/// </summary>
		public static readonly BindableProperty IsForReviewCountryProperty =
			BindableProperty.Create(nameof(IsForReviewCountry), typeof(bool), typeof(ViewIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the <see cref="Country"/> property is for review.
		/// </summary>
		public bool IsForReviewCountry
		{
			get => (bool)this.GetValue(IsForReviewCountryProperty);
			set => this.SetValue(IsForReviewCountryProperty, value);
		}

		/// <summary>
		/// See <see cref="FirstPhotoSource"/>
		/// </summary>
		public static readonly BindableProperty FirstPhotoSourceProperty =
			BindableProperty.Create(nameof(FirstPhotoSource), typeof(ImageSource), typeof(ViewIdentityViewModel), default(ImageSource));

		/// <summary>
		/// Image source of the first photo in the identity.
		/// </summary>
		public ImageSource FirstPhotoSource
		{
			get => (ImageSource)this.GetValue(FirstPhotoSourceProperty);
			set => this.SetValue(FirstPhotoSourceProperty, value);
		}

		/// <summary>
		/// See <see cref="FirstPhotoRotation"/>
		/// </summary>
		public static readonly BindableProperty FirstPhotoRotationProperty =
			BindableProperty.Create(nameof(FirstPhotoRotation), typeof(int), typeof(ViewIdentityViewModel), default(int));

		/// <summary>
		/// Rotation of the first photo in the identity.
		/// </summary>
		public int FirstPhotoRotation
		{
			get => (int)this.GetValue(FirstPhotoRotationProperty);
			set => this.SetValue(FirstPhotoRotationProperty, value);
		}

		#endregion

		private async Task Approve()
		{
			if (this.requestorIdentity is null)
				return;

			try
			{
				if ((!string.IsNullOrEmpty(this.FirstName) && !this.FirstNameIsChecked) ||
					(!string.IsNullOrEmpty(this.MiddleNames) && !this.MiddleNamesIsChecked) ||
					(!string.IsNullOrEmpty(this.LastNames) && !this.LastNamesIsChecked) ||
					(!string.IsNullOrEmpty(this.PersonalNumber) && !this.PersonalNumberIsChecked) ||
					(!string.IsNullOrEmpty(this.Address) && !this.AddressIsChecked) ||
					(!string.IsNullOrEmpty(this.Address2) && !this.Address2IsChecked) ||
					(!string.IsNullOrEmpty(this.ZipCode) && !this.ZipCodeIsChecked) ||
					(!string.IsNullOrEmpty(this.Area) && !this.AreaIsChecked) ||
					(!string.IsNullOrEmpty(this.City) && !this.CityIsChecked) ||
					(!string.IsNullOrEmpty(this.Region) && !this.RegionIsChecked) ||
					(!string.IsNullOrEmpty(this.CountryCode) && !this.CountryCodeIsChecked))
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Incomplete"], LocalizationResourceManager.Current["PleaseReviewAndCheckAllCheckboxes"]);
					return;
				}

				if (!this.CarefulReviewIsChecked)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Incomplete"], LocalizationResourceManager.Current["YouNeedToCheckCarefullyReviewed"]);
					return;
				}

				if (!this.ApprovePiiIsChecked)
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Incomplete"], LocalizationResourceManager.Current["YouNeedToApproveToAssociate"]);
					return;
				}

				if (!await App.VerifyPin())
					return;

				(bool Succeeded1, byte[] Signature) = await this.NetworkService.TryRequest(() =>
					this.XmppService.Sign(this.contentToSign, SignWith.LatestApprovedId));

				if (!Succeeded1)
					return;

				bool Succeeded2 = await this.NetworkService.TryRequest(() =>
				{
					return this.XmppService.SendPetitionSignatureResponse(this.signatoryIdentityId, this.contentToSign,
						Signature, this.petitionId, this.requestorFullJid, true);
				});

				if (Succeeded2)
					await this.NavigationService.GoBackAsync();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Reject()
		{
			if (this.requestorIdentity is null)
				return;

			try
			{
				bool Succeeded = await this.NetworkService.TryRequest(() =>
				{
					return this.XmppService.SendPetitionSignatureResponse(this.signatoryIdentityId, this.contentToSign,
						new byte[0], this.petitionId, this.requestorFullJid, false);
				});

				if (Succeeded)
					await this.NavigationService.GoBackAsync();
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Revoke()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				if (!await this.AreYouSure(LocalizationResourceManager.Current["AreYouSureYouWantToRevokeYourLegalIdentity"]))
					return;

				(bool succeeded, LegalIdentity revokedIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.ObsoleteLegalIdentity(this.LegalIdentity.Id));
				if (succeeded)
				{
					this.LegalIdentity = revokedIdentity;
					await this.TagProfile.RevokeLegalIdentity(revokedIdentity);

					await App.Current.SetRegistrationPageAsync();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task<bool> AreYouSure(string Message)
		{
			if (!await App.VerifyPin())
				return false;

			return await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Confirm"], Message, LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["No"]);
		}

		private async Task Compromise()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				if (!await this.AreYouSure(LocalizationResourceManager.Current["AreYouSureYouWantToReportYourLegalIdentityAsCompromized"]))
					return;

				(bool succeeded, LegalIdentity compromisedIdentity) = await this.NetworkService.TryRequest(() => this.XmppService.CompromiseLegalIdentity(this.LegalIdentity.Id));

				if (succeeded)
				{
					this.LegalIdentity = compromisedIdentity;
					await this.TagProfile.RevokeLegalIdentity(compromisedIdentity);

					await App.Current.SetRegistrationPageAsync();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task Transfer()
		{
			if (!this.IsPersonal)
				return;

			try
			{
				string Pin = await App.InputPin();
				if (Pin is null)
					return;

				if (!await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["Confirm"], LocalizationResourceManager.Current["AreYouSureYouWantToTransferYourLegalIdentity"], LocalizationResourceManager.Current["Yes"], LocalizationResourceManager.Current["No"]))
					return;

				this.IsBusy = true;
				this.EvaluateAllCommands();
				try
				{
					StringBuilder Xml = new();
					XmlWriterSettings Settings = XML.WriterSettings(false, true);

					using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
					{
						Output.WriteStartElement("Transfer", ContractsClient.NamespaceOnboarding);

						await this.XmppService.ExportSigningKeys(Output);

						Output.WriteStartElement("Pin");
						Output.WriteAttributeString("pin", Pin);
						Output.WriteEndElement();

						Output.WriteStartElement("Account", ContractsClient.NamespaceOnboarding);
						Output.WriteAttributeString("domain", this.TagProfile.Domain);
						Output.WriteAttributeString("userName", this.TagProfile.Account);
						Output.WriteAttributeString("password", this.TagProfile.PasswordHash);

						if (!string.IsNullOrEmpty(this.TagProfile.PasswordHashMethod))
							Output.WriteAttributeString("passwordMethod", this.TagProfile.PasswordHashMethod);

						Output.WriteEndElement();
						Output.WriteEndElement();
					}

					using RandomNumberGenerator Rnd = RandomNumberGenerator.Create();
					byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
					byte[] Key = new byte[16];
					byte[] IV = new byte[16];

					Rnd.GetBytes(Key);
					Rnd.GetBytes(IV);

					using Aes Aes = Aes.Create();
					Aes.BlockSize = 128;
					Aes.KeySize = 256;
					Aes.Mode = CipherMode.CBC;
					Aes.Padding = PaddingMode.PKCS7;

					using ICryptoTransform Transform = Aes.CreateEncryptor(Key, IV);
					byte[] Encrypted = Transform.TransformFinalBlock(Data, 0, Data.Length);

					Xml.Clear();

					using (XmlWriter Output = XmlWriter.Create(Xml, Settings))
					{
						Output.WriteStartElement("Info", ContractsClient.NamespaceOnboarding);
						Output.WriteAttributeString("base64", Convert.ToBase64String(Encrypted));
						Output.WriteAttributeString("once", "true");
						Output.WriteAttributeString("expires", XML.Encode(DateTime.UtcNow.AddMinutes(1)));
						Output.WriteEndElement();
					}

					XmlElement Response = await this.XmppService.IqSetAsync(Constants.Domains.OnboardingDomain, Xml.ToString());

					foreach (XmlNode N in Response.ChildNodes)
					{
						if (N is XmlElement Info && Info.LocalName == "Code" && Info.NamespaceURI == ContractsClient.NamespaceOnboarding)
						{
							string Code = XML.Attribute(Info, "code");
							string Url = "obinfo:" + Constants.Domains.IdDomain + ":" + Code + ":" +
								Convert.ToBase64String(Key) + ":" + Convert.ToBase64String(IV);

							await this.XmppService.AddTransferCode(Code);
							await this.NavigationService.GoToAsync(nameof(TransferIdentityPage), new TransferIdentityNavigationArgs(Url));
							return;
						}
					}

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnexpectedResponse"]);
				}
				finally
				{
					this.IsBusy = false;
					this.EvaluateAllCommands();
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task ChangePin()
		{
			if (!this.IsPersonal)
				return;

			await SecurityViewModel.ChangePin(this);
		}

		#region ILinkableView

		/// <summary>
		/// Title of the current view
		/// </summary>
		public override Task<string> Title => Task.FromResult<string>(ContactInfo.GetFriendlyName(this.LegalIdentity));

		#endregion


	}
}
