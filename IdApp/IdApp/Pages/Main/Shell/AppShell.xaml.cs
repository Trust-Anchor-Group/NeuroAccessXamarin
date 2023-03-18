using System;
using System.Text;
using System.Threading.Tasks;
using IdApp.Pages.Contracts.PetitionSignature;
using IdApp.Pages.Identity.PetitionIdentity;
using IdApp.Pages.Identity.TransferIdentity;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Pages.Main.Calculator;
using IdApp.Pages.Main.ScanQrCode;
using IdApp.Pages.Main.Security;
using IdApp.Services.Contracts;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.UI;
using IdApp.Services.UI.QR;
using IdApp.Services.Xmpp;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Shell
{
	/// <summary>
	/// The Xamarin Forms Shell implementation of the Neuro-Access App.
	/// </summary>
	public partial class AppShell : ShellBasePage
	{
		/// <summary>
		/// Create a new instance of the <see cref="AppShell"/> class.
		/// </summary>
		public AppShell()
		{
			this.ViewModel = new AppShellViewModel();
			this.InitializeComponent();
			SetTabBarIsVisible(this, false);
			this.RegisterRoutes();
		}

		/// <summary>
		/// Current XMPP Service
		/// </summary>
		public IXmppService XmppService => App.Instantiate<IXmppService>();

		/// <summary>
		/// Current Network Service
		/// </summary>
		public INetworkService NetworkService => App.Instantiate<INetworkService>();

		/// <summary>
		/// Current Navigation Service
		/// </summary>
		public INavigationService NavigationService => App.Instantiate<INavigationService>();

		/// <summary>
		/// Current Log Service
		/// </summary>
		public ILogService LogService => App.Instantiate<ILogService>();

		/// <summary>
		/// Current UI Dispatcher Service
		/// </summary>
		public IUiSerializer UiSerializer => App.Instantiate<IUiSerializer>();

		/// <summary>
		/// Current Contract Orchestrator Service
		/// </summary>
		public IContractOrchestratorService ContractOrchestratorService => App.Instantiate<IContractOrchestratorService>();

		private void RegisterRoutes()
		{
			// General:
			Routing.RegisterRoute(nameof(ScanQrCodePage), typeof(ScanQrCodePage));
			Routing.RegisterRoute(nameof(CalculatorPage), typeof(CalculatorPage));
			Routing.RegisterRoute(nameof(SecurityPage), typeof(SecurityPage));

			// Identity:
			Routing.RegisterRoute(nameof(ViewIdentityPage), typeof(ViewIdentityPage));
			Routing.RegisterRoute(nameof(PetitionIdentityPage), typeof(PetitionIdentityPage));
			Routing.RegisterRoute(nameof(TransferIdentityPage), typeof(TransferIdentityPage));

			// Contracts:
			Routing.RegisterRoute(nameof(PetitionSignaturePage), typeof(PetitionSignaturePage));
		}

		private async Task GoToPage(string route)
		{
			// Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
			// Therefore we have to close it manually here.

			Current.FlyoutIsPresented = false;

			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.

			if (this.GetViewModel<AppShellViewModel>().IsConnected)
				await this.NavigationService?.GoToAsync(route);
		}

		private async Task GoToPage<TArgs>(string route, TArgs e)
			where TArgs : NavigationArgs, new()
		{
			// Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
			// Therefore we have to close it manually here.

			Current.FlyoutIsPresented = false;

			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.

			if (this.GetViewModel<AppShellViewModel>().IsConnected)
				await this.NavigationService.GoToAsync<TArgs>(route, e);
		}

		private async void ViewIdentityMenuItem_Clicked(object Sender, EventArgs e)
		{
			if (!await App.VerifyPin())
				return;

			await this.GoToPage(nameof(ViewIdentityPage));
		}

		internal async void ScanQrCodeMenuItem_Clicked(object Sender, EventArgs e)
		{
			await QrCode.ScanQrCodeAndHandleResult();
		}

		private async void Calculator_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(CalculatorPage), new CalculatorNavigationArgs(null));
		}

		private async void Security_Clicked(object Sender, EventArgs e)
		{
			await this.GoToPage(nameof(SecurityPage), new NavigationArgs());
		}

		private void ExitMenuItem_Clicked(object Sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			// Break the call chain by 'posting' to the main thread, allowing the fly out menu to hide before initiating the login/out.
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				await App.Stop();
			});
		}

		private void AboutMenuItem_Clicked(object Sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;

			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				StringBuilder sb = new();

				sb.AppendLine("Name: " + AppInfo.Name);
				sb.AppendLine("Version: " + AppInfo.VersionString + "." + AppInfo.BuildString);
				sb.AppendLine("Runtime: " + this.GetType().Assembly.ImageRuntimeVersion);
				sb.AppendLine("Manufacturer: " + DeviceInfo.Manufacturer);
				sb.AppendLine("Phone: " + DeviceInfo.Model);
				sb.AppendLine("Platform: " + DeviceInfo.Platform + " " + DeviceInfo.VersionString);

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["About"], sb.ToString());
			});
		}
	}
}
