﻿using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.RegisterIdentity
{
    /// <summary>
    /// A view to display the 'register identity' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegisterIdentityView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="RegisterIdentityView"/> class.
        /// </summary>
        public RegisterIdentityView()
        {
			this.InitializeComponent();
        }

		private void RegionEntry_Focused(object Sender, FocusEventArgs e)
		{
			if (this.ViewModel is RegisterIdentityViewModel Model && Model.ShowOrganization)
				this.RegistrationLayout.ScrollToAsync(this.OrgCountryPicker, ScrollToPosition.MakeVisible, true);
			else
	            this.RegistrationLayout.ScrollToAsync(this.RegisterButton, ScrollToPosition.MakeVisible, true);
		}

		private void OrgRegionEntry_Focused(object Sender, FocusEventArgs e)
		{
			this.RegistrationLayout.ScrollToAsync(this.RegisterButton, ScrollToPosition.MakeVisible, true);
		}
	}
}
