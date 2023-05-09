﻿using IdApp.Services;
using System;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;

namespace IdApp.Links
{
	/// <summary>
	/// Opens eDaler links.
	/// </summary>
	public class EDalerLink : ILinkOpener
	{
		/// <summary>
		/// Opens eDaler links.
		/// </summary>
		public EDalerLink()
		{
		}

		/// <summary>
		/// How well the link opener supports a given link
		/// </summary>
		/// <param name="Link">Link that will be opened.</param>
		/// <returns>Support grade of opener for the given link.</returns>
		public Grade Supports(Uri Link)
		{
			return Link.Scheme.ToLower() == Constants.UriSchemes.UriSchemeEDaler ? Grade.Ok : Grade.NotAtAll;
		}

		/// <summary>
		/// Tries to open a link
		/// </summary>
		/// <param name="Link">Link to open</param>
		/// <returns>If the link was opened.</returns>
		public async Task<bool> TryOpenLink(Uri Link)
		{
			ServiceReferences Services = new();
			await Services.NeuroWalletOrchestratorService.OpenEDalerUri(Link.OriginalString);

			return true;
		}
	}
}
