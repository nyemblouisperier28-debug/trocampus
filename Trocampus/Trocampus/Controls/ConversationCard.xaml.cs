using Trocampus.Helpers;

namespace Trocampus.Controls;

public partial class ConversationCard : ContentView
{
    public static readonly BindableProperty InitialProperty = BindableProperty.Create(
        nameof(Initial), typeof(string), typeof(ConversationCard), string.Empty);
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(ConversationCard), string.Empty);
    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
        nameof(Subtitle), typeof(string), typeof(ConversationCard), string.Empty);
    public static readonly BindableProperty PreviewProperty = BindableProperty.Create(
        nameof(Preview), typeof(string), typeof(ConversationCard), string.Empty);
    public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(
        nameof(BadgeText), typeof(string), typeof(ConversationCard), string.Empty,
        propertyChanged: (b, _, n) =>
            ((ConversationCard)b).HasBadge = !string.IsNullOrEmpty((string)n));
    public static readonly BindableProperty HasBadgeProperty = BindableProperty.Create(
        nameof(HasBadge), typeof(bool), typeof(ConversationCard), false);
    public static readonly BindableProperty IsHighlightedProperty = BindableProperty.Create(
        nameof(IsHighlighted), typeof(bool), typeof(ConversationCard), false,
        propertyChanged: (b, _, _) => ((ConversationCard)b).ApplyStyle());
    public string Initial
    {
        get => (string)GetValue(InitialProperty);
        set => SetValue(InitialProperty, value);
    }
    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }
    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
    public string Preview
    {
        get => (string)GetValue(PreviewProperty);
        set => SetValue(PreviewProperty, value);
    }
    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }
    public bool HasBadge
    {
        get => (bool)GetValue(HasBadgeProperty);
        private set => SetValue(HasBadgeProperty, value);
    }
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }
    public event EventHandler? Tapped;
    public ConversationCard()
    {
        InitializeComponent();
        Card.BindingContext = this;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        Card.GestureRecognizers.Add(tap);
        ApplyStyle();
    }
    // Fiche mise en avant : fond teinté sans bordure. Sinon : fond blanc + bordure.
    private void ApplyStyle()
    {
        Card.BackgroundColor = IsHighlighted ? Res.Get("TcSurfaceTint") : Colors.White;
        Card.StrokeThickness = IsHighlighted ? 0 : 1;
        Card.Stroke = new SolidColorBrush(Res.Get("TcBorder"));
    }
}