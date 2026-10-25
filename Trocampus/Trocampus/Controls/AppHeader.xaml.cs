namespace Trocampus.Controls;

public partial class AppHeader : ContentView
{
    public static readonly BindableProperty InitialProperty = BindableProperty.Create(
        nameof(Initial), typeof(string), typeof(AppHeader), "L");
    public string Initial
    {
        get => (string)GetValue(InitialProperty);
        set => SetValue(InitialProperty, value);
    }
    public AppHeader()
    {
        InitializeComponent();
        Root.BindingContext = this;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        try
        {
            var session = await App.Services.GetRequiredService<Trocampus.Services.AuthSession>()
                .GetSessionAsync();
            if (!string.IsNullOrWhiteSpace(session?.FirstName))
                Initial = session.FirstName[..1].ToUpperInvariant();
        }
        catch (Exception)
        {
            Initial = "?";
        }
    }
}