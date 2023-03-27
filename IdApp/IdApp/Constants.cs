using System;

namespace IdApp
{
    /// <summary>
    /// A set of never changing property constants and helpful values.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// A generic "no value available" string.
        /// </summary>
        public const string NotAvailableValue = "-";

		/// <summary>
		/// A maximum number of pixels to render for images, downscaling them if necessary.
		/// </summary>
		public const int MaxRenderedImageDimensionInPixels = 800;

        /// <summary>
        /// Authentication constants
        /// </summary>
        public static class Authentication
        {
            /// <summary>
            /// Minimum length for PIN Code
            /// </summary>
            public const int MinPinLength = 8;

			/// <summary>
			/// Minimum number of symbols from at least two character classes (digits, letters, other) in a PIN.
			/// </summary>
			public const int MinPinSymbolsFromDifferentClasses = 2;

			/// <summary>
			/// Maximum number of identical symbols in a PIN.
			/// </summary>
			public const int MaxPinIdenticalSymbols = 2;

			/// <summary>
			/// Maximum number of sequenced symbols in a PIN.
			/// </summary>
			public const int MaxPinSequencedSymbols = 2;
		}

        /// <summary>
        /// Language Codes
        /// </summary>
        public static class LanguageCodes
        {
            /// <summary>
            /// The default language code.
            /// </summary>
            public const string Default = "en-US";
        }

        /// <summary>
        /// IoT Schemes
        /// </summary>
        public static class UriSchemes
        {
			/// <summary>
			/// The IoT ID URI Scheme (iotid)
			/// </summary>
			public const string UriSchemeIotId = "iotid";

            /// <summary>
            /// TAG Signature (Quick-Login) URI Scheme (tagsign)
            /// </summary>
            public const string UriSchemeTagSign = "tagsign";

            /// <summary>
            /// Onboarding URI Scheme (obinfo)
            /// </summary>
            public const string UriSchemeOnboarding = "obinfo";

            /// <summary>
            /// Gets the predefined scheme from an IoT Code
            /// </summary>
            /// <param name="Url">The URL to parse.</param>
            /// <returns>URI Scheme</returns>
            public static string GetScheme(string Url)
            {
                if (string.IsNullOrWhiteSpace(Url))
                    return null;

                int i = Url.IndexOf(':');
                if (i < 0)
                    return null;

                Url = Url[..i].ToLowerInvariant();

				return Url switch
				{
					UriSchemeIotId or
                    UriSchemeTagSign or
                    UriSchemeOnboarding => Url,

					_ => null,
				};
			}

            /// <summary>
            /// Checks if the specified code starts with the IoT ID scheme.
            /// </summary>
            /// <param name="Url">The URL to check.</param>
            /// <returns>If URI is an ID scheme</returns>
            public static bool StartsWithIdScheme(string Url)
            {
                return !string.IsNullOrWhiteSpace(Url) &&
                       Url.StartsWith(UriSchemeIotId + ":", StringComparison.InvariantCultureIgnoreCase);
            }

            /// <summary>
            /// Generates a IoT ID Uri form the specified id.
            /// </summary>
            /// <param name="id">The Id to use when generating the Uri.</param>
            /// <returns>Identity URI</returns>
            public static string CreateIdUri(string id)
            {
                return UriSchemeIotId + ":" + id;
            }

            /// <summary>
            /// Removes the URI Schema from an URL.
            /// </summary>
            /// <param name="Url">The URL to parse and extract the URI schema from.</param>
            /// <returns>URI, without schema</returns>
            public static string RemoveScheme(string Url)
            {
                string Scheme = GetScheme(Url);
                if (string.IsNullOrEmpty(Scheme))
                    return null;

                return Url[(Scheme.Length + 1)..];
            }
        }

        /// <summary>
        /// MIME Types
        /// </summary>
        public static class MimeTypes
        {
            /// <summary>
            /// The JPEG MIME type.
            /// </summary>
            public const string Jpeg = "image/jpeg";

            /// <summary>
            /// The PNG MIME type.
            /// </summary>
            public const string Png = "image/png";
        }

        /// <summary>
        /// Domain names.
        /// </summary>
        public static class Domains
		{
            /// <summary>
            /// Neuro-Access domain.
            /// </summary>
            public const string IdDomain = "id.tagroot.io";

            /// <summary>
            /// Neuro-Access onboarding domain.
            /// </summary>
            public const string OnboardingDomain = "onboarding.id.tagroot.io";
        }

        /// <summary>
        /// XMPP Protocol Properties.
        /// </summary>
        public static class XmppProperties
        {
            /// <summary>
            /// First name
            /// </summary>
            public const string FirstName = "FIRST";

            /// <summary>
            /// Middle name
            /// </summary>
            public const string MiddleName = "MIDDLE";

            /// <summary>
            /// Last name
            /// </summary>
            public const string LastName = "LAST";

            /// <summary>
            /// /Personal number
            /// </summary>
            public const string PersonalNumber = "PNR";

            /// <summary>
            /// Address line 1
            /// </summary>
            public const string Address = "ADDR";

            /// <summary>
            /// Address line 2
            /// </summary>
            public const string Address2 = "ADDR2";

            /// <summary>
            /// Area
            /// </summary>
            public const string Area = "AREA";

            /// <summary>
            /// City
            /// </summary>
            public const string City = "CITY";

            /// <summary>
            /// Zip Code
            /// </summary>
            public const string ZipCode = "ZIP";

            /// <summary>
            ///  Region
            /// </summary>
            public const string Region = "REGION";

            /// <summary>
            /// Country
            /// </summary>
            public const string Country = "COUNTRY";

            /// <summary>
            /// Device ID
            /// </summary>
            public const string DeviceId = "DEVICE_ID";

            /// <summary>
            /// Jabber ID
            /// </summary>
            public const string Jid = "JID";

            /// <summary>
            /// Phone number
            /// </summary>
            public const string Phone = "PHONE";

			/// <summary>
			/// e-Mail address
			/// </summary>
			public const string EMail = "EMAIL";
		}

		/// <summary>
		/// Timer Intervals
		/// </summary>
		public static class Intervals
        {
            /// <summary>
            /// Auto Save interval
            /// </summary>
            public static readonly TimeSpan AutoSave = TimeSpan.FromSeconds(1);

            /// <summary>
            /// Reconnect interval
            /// </summary>
            public static readonly TimeSpan Reconnect = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Timer Timeout Values
        /// </summary>
        public static class Timeouts
        {
            /// <summary>
            /// Generic request timeout
            /// </summary>
            public static readonly TimeSpan GenericRequest = TimeSpan.FromSeconds(30);

            /// <summary>
            /// Database timeout
            /// </summary>
            public static readonly TimeSpan Database = TimeSpan.FromSeconds(10);

            /// <summary>
            /// XMPP Connect timeout
            /// </summary>
            public static readonly TimeSpan XmppConnect = TimeSpan.FromSeconds(10);

            /// <summary>
            /// XMPP Init timeout
            /// </summary>
            public static readonly TimeSpan XmppInit = TimeSpan.FromSeconds(1);

            /// <summary>
            /// Upload file timeout
            /// </summary>
            public static readonly TimeSpan UploadFile = TimeSpan.FromSeconds(30);

            /// <summary>
            /// Download file timeout
            /// </summary>
            public static readonly TimeSpan DownloadFile = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// MessagingCenter events
        /// </summary>
        public static class MessagingCenter
        {
			/// <summary>
			/// Keyboard appears event
			/// </summary>
			public const string KeyboardAppears = "KeyboardAppears";

            /// <summary>
            /// Keyboard disappears event
            /// </summary>
            public const string KeyboardDisappears = "KeyboardDisappears";
        }

		/// <summary>
		/// Names of Effects.
		/// </summary>
		public static class Effects
		{
			/// <summary>
			/// ResolutionGroupName used for resolving Effects.
			/// </summary>
			public const string ResolutionGroupName = "com.tag.NeuroAccess";

			/// <summary>
			/// PasswordMaskTogglerEffect.
			/// </summary>
			public const string PasswordMaskTogglerEffect = "PasswordMaskTogglerEffect";
		}

		/// <summary>
		/// Constants for PIN
		/// </summary>
		public static class Pin
		{

			/// <summary>
			/// Possible time of inactivity
			/// </summary>
			public const int PossibleInactivityInMinutes = 5;

            /// <summary>
			/// Maximum pin enetring attempts
			/// </summary>
			public const int MaxPinAttempts = 3;

            /// <summary>
			/// First Block in days after 3 attempts
			/// </summary>
			public const int FirstBlockInDays = 1;

            /// <summary>
            /// Second Block in days after 3 attempts
            /// </summary>
            public const int SecondBlockInDays = 7;

            /// <summary>
            /// Key for pin attempt counter
            /// </summary>
            public const string CurrentPinAttemptCounter = "CurrentPinAttemptCounter";

            /// <summary>
            /// Log Object ID
            /// </summary>
            public const string LogAuditorObjectID = "LogAuditorObjectID";

            /// <summary>
            /// Endpoint for LogAuditor
            /// </summary>
            public const string RemoteEndpoint = "local";

            /// <summary>
            /// Protocol for LogAuditor
            /// </summary>
            public const string Protocol = "local";

            /// <summary>
            /// Reason for LogAuditor
            /// </summary>
            public const string Reason = "pinEnteringFailure";
        }

		/// <summary>
		/// References to external resources
		/// </summary>
		public static class References
		{
			/// <summary>
			/// Resource where Android App can be downloaded.
			/// </summary>
			public const string AndroidApp = "https://play.google.com/store/apps/details?id=com.tag.NeuroAccess";

			/// <summary>
			/// Resource where iPhone App can be downloaded.
			/// </summary>
			public const string IPhoneApp = "https://apps.apple.com/se/app/trust-anchor-access/id1580610247";
		}

	}
}
