namespace Trocampus;

public partial class App : Application
{
    public static IServiceProvider Services { get; set; } = default!;

    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        MainPage = new AppShell();
    }
}