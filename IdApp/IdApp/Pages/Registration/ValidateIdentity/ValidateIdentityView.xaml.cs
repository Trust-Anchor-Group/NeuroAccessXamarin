﻿using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Registration.ValidateIdentity
{
    /// <summary>
    /// A view to display the 'validate identity' during the registration process.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ValidateIdentityView
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ValidateIdentityView"/> class.
        /// </summary>
        public ValidateIdentityView()
        {
			this.BindingContext = new ValidateIdentityViewModel();

			this.InitializeComponent();
        }

		private void Image_Tapped(object Sender, System.EventArgs e)
		{
            Attachment[] attachments = this.GetContentViewModel<ValidateIdentityViewModel>().LegalIdentity?.Attachments;
            this.PhotoViewer.ShowPhotos(attachments);
        }
    }
}
