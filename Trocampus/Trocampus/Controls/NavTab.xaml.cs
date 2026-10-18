using Trocampus.Helpers;

namespace Trocampus.Controls;

public partial class NavTab : ContentView
{
    public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
        nameof(IconSource), typeof(ImageSource), typeof(NavTab), null,
        propertyChanged: (b, _, n) => ((NavTab)b).Icon.Source = (ImageSource)n);
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(NavTab), string.Empty,
        propertyChanged: (b, _, n) => ((NavTab)b).Caption.Text = (string)n);
    public static readonly BindableProperty RouteProperty = BindableProperty.Create(
        nameof(Route), typeof(string), typeof(NavTab), string.Empty);
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(NavTab), false,
        propertyChanged: (b, _, _) => ((NavTab)b).UpdateState());
    public ImageSource IconSource
    {
        get => (ImageSource)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public string Route
    {
        get => (string)GetValue(RouteProperty);
        set => SetValue(RouteProperty, value);
    }
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
    public NavTab()
    {
        InitializeComponent();
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (!string.IsNullOrEmpty(Route))
                await Nav.GoAsync($"//{Route}");
        };
        GestureRecognizers.Add(tap);
        UpdateState();
    }
    private void UpdateState()
    {
        var color = (Color)Application.Current!.Resources[IsActive ? "Primary" : "TcMuted"];
        Tint.TintColor = color;
        Caption.TextColor = color;
        Caption.FontFamily = IsActive ? "InterSemiBold" : "InterRegular";
    }
}