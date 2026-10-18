using CommunityToolkit.Maui.ApplicationModel;
using Trocampus.Helpers;
namespace Trocampus.Controls;

public enum StatusKind
{
    Success,
    Warning,
    Danger
}
public partial class StatusBadge : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(StatusBadge), string.Empty,
        propertyChanged: (b, _, n) => ((StatusBadge)b).Caption.Text = (string)n);
    public static readonly BindableProperty KindProperty = BindableProperty.Create(
        nameof(Kind), typeof(StatusKind), typeof(StatusBadge), StatusKind.Success,
        propertyChanged: (b, _, _) => ((StatusBadge)b).UpdateColors());
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public StatusKind Kind
    {
        get => (StatusKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }
    public StatusBadge()
    {
        InitializeComponent();
        UpdateColors();
    }
    private void UpdateColors()
    {
        var (background, foreground) = Kind switch
        {
            StatusKind.Warning => ("TcWarningBg", "TcWarningText"),
            StatusKind.Danger => ("TcDangerBg", "TcDangerText"),
            _ => ("TcSurfaceTint", "Primary")
        };
        Badge.BackgroundColor = Res.Get(background);
        Caption.TextColor = Res.Get(foreground);
    }
}