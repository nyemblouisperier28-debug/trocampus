namespace Trocampus.Controls;

public partial class ListingCard : ContentView
{
    public static readonly BindableProperty PhotoSourceProperty = BindableProperty.Create(
        nameof(PhotoSource), typeof(ImageSource), typeof(ListingCard), null);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ListingCard), string.Empty);
    public static readonly BindableProperty MetaProperty = BindableProperty.Create(
        nameof(Meta), typeof(string), typeof(ListingCard), string.Empty);
    public static readonly BindableProperty OfferProperty = BindableProperty.Create(
        nameof(Offer), typeof(string), typeof(ListingCard), string.Empty);
    public ImageSource PhotoSource
    {
        get => (ImageSource)GetValue(PhotoSourceProperty);
        set => SetValue(PhotoSourceProperty, value);
    }
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string Meta
    {
        get => (string)GetValue(MetaProperty);
        set => SetValue(MetaProperty, value);
    }
    public string Offer
    {
        get => (string)GetValue(OfferProperty);
        set => SetValue(OfferProperty, value);
    }
    public event EventHandler? Tapped;
    public ListingCard()
    {
        InitializeComponent();
        Card.BindingContext = this;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        Card.GestureRecognizers.Add(tap);
    }
}