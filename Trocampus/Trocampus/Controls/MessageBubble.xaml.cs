using Trocampus.Helpers;

namespace Trocampus.Controls;

public partial class MessageBubble : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(MessageBubble), string.Empty);
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(
        nameof(Caption), typeof(string), typeof(MessageBubble), string.Empty);
    public static readonly BindableProperty IsOutgoingProperty = BindableProperty.Create(
        nameof(IsOutgoing), typeof(bool), typeof(MessageBubble), false,
        propertyChanged: (b, _, _) => ((MessageBubble)b).ApplyStyle());
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }
    public bool IsOutgoing
    {
        get => (bool)GetValue(IsOutgoingProperty);
        set => SetValue(IsOutgoingProperty, value);
    }
    public MessageBubble()
    {
        InitializeComponent();
        Root.BindingContext = this;
        ApplyStyle();
    }
    // Message envoyé : à droite, fond teinté sans bordure. Message reçu : à gauche, blanc + bordure.
    private void ApplyStyle()
    {
        var alignment = IsOutgoing ? LayoutOptions.End : LayoutOptions.Start;
        Bubble.HorizontalOptions = alignment;
        CaptionLabel.HorizontalOptions = alignment;
        Bubble.BackgroundColor = IsOutgoing ? Res.Get("TcSurfaceTint") : Colors.White;
        Bubble.StrokeThickness = IsOutgoing ? 0 : 1;
        Bubble.Stroke = new SolidColorBrush(Res.Get("TcBorder"));
    }
}
      