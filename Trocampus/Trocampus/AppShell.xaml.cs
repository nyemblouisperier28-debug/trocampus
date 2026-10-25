using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;

namespace Trocampus
{
    public partial class AppShell : Shell
    {
        private bool _redirectingToLogin;

        public AppShell()
        {
            InitializeComponent();
            Navigating += OnNavigating;
        }

        private async void OnNavigating(object? sender, ShellNavigatingEventArgs e)
        {
            if (_redirectingToLogin)
                return;

            var route = e.Target.Location.OriginalString
                .Split('?', 2)[0]
                .TrimEnd('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            if (route is null || route is "login" or "register" or "forgot")
                return;

            var session = await App.Services.GetRequiredService<Trocampus.Services.AuthSession>().GetSessionAsync();
            if (session is not null)
                return;

            e.Cancel();
            _redirectingToLogin = true;
            await GoToAsync("//login");
            _redirectingToLogin = false;
        }
        public static async Task DisplaySnackbarAsync(string message)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#FF3300"),
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
                CornerRadius = new CornerRadius(0),
                Font = Font.SystemFontOfSize(18),
                ActionButtonFont = Font.SystemFontOfSize(14)
            };

            var snackbar = Snackbar.Make(message, visualOptions: snackbarOptions);

            await snackbar.Show(cancellationTokenSource.Token);
        }

        public static async Task DisplayToastAsync(string message)
        {
            // Toast is currently not working in MCT on Windows
            if (OperatingSystem.IsWindows())
                return;

            var toast = Toast.Make(message, textSize: 18);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await toast.Show(cts.Token);
        }

        private void SfSegmentedControl_SelectionChanged(object? sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
        {
            Application.Current!.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark;
        }
    }
}
