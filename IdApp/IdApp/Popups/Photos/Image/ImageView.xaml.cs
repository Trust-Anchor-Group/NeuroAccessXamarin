﻿using System;
using System.Linq;
using IdApp.Extensions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Popups.Photos.Image
{
    /// <summary>
    /// A generic UI component to display an image.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImageView
    {
        private const uint durationInMs = 300;

        /// <summary>
        /// Creates a new instance of the <see cref="ImageView"/> class.
        /// </summary>
        public ImageView()
        {
			this.InitializeComponent();
			this.ContentViewModel = new ImageViewModel();
        }

        /// <summary>
        /// Shows the attachments photos in the current view.
        /// </summary>
        /// <param name="attachments">The attachments to show.</param>
        public void ShowPhotos(Attachment[] attachments)
        {
            if (attachments is null || attachments.Length <= 0)
                return;

            Attachment[] imageAttachments = attachments.GetImageAttachments().ToArray();
            if (imageAttachments.Length <= 0)
                return;

            this.IsVisible = true;
			this.GetContentViewModel<ImageViewModel>().LoadPhotos(attachments);
            Device.BeginInvokeOnMainThread(async () =>
            {
                await this.PhotoViewer.FadeTo(1d, durationInMs, Easing.SinIn);
            });
        }

        /// <summary>
        /// Hides the photos from view.
        /// </summary>
        public void HidePhotos()
        {
            this.PhotoViewer.Opacity = 0;
			this.GetContentViewModel<ImageViewModel>().ClearPhotos();
            this.IsVisible = false;
        }

        private void CloseIcon_Tapped(object Sender, EventArgs e)
        {
            this.HidePhotos();
        }

        /// <summary>
        /// Gets if photos are showing or not.
        /// </summary>
        /// <returns>If photos are showing</returns>
        public bool PhotosAreShowing()
        {
            return this.PhotoViewer.Opacity > 0 && this.IsVisible;
        }
    }
}
