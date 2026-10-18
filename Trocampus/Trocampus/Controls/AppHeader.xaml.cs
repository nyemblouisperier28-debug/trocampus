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
    }
}