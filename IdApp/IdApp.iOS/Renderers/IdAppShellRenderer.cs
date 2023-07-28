﻿using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Shell), typeof(IdApp.iOS.Renderers.IdAppShellRenderer))]
namespace IdApp.iOS.Renderers
{
	public class IdAppShellRenderer : ShellRenderer
	{
		protected override IShellFlyoutContentRenderer CreateShellFlyoutContentRenderer()
		{
			return new IdAppShellFlyoutContentRenderer(this);
		}

		protected override IShellSectionRenderer CreateShellSectionRenderer(ShellSection ShellSection)
		{
			return new IdAppShellSectionRenderer(this);
		}

		protected override IShellPageRendererTracker CreatePageRendererTracker()
		{
			return new IdAppShellPageRendererTracker(this);
		}
	}
}
