using Trocampus.Helpers;
namespace Trocampus.Controls;

public partial class TransactionCard : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(TransactionCard), string.Empty);
    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date), typeof(string), typeof(TransactionCard), string.Empty);
    public static readonly BindableProperty AmountProperty = BindableProperty.Create(
        nameof(Amount), typeof(string), typeof(TransactionCard), string.Empty);
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(string), typeof(TransactionCard), string.Empty);
    // Vrai : montant en vert (gain). Faux : montant en couleur de texte (dépense).
    public static readonly BindableProperty IsCreditProperty = BindableProperty.Create(
        nameof(IsCredit), typeof(bool), typeof(TransactionCard), true,
        propertyChanged: (b, _, _) => ((TransactionCard)b).UpdateAmountColor());
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string Date
    {
        get => (string)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }
    public string Amount
    {
        get => (string)GetValue(AmountProperty);
        set => SetValue(AmountProperty, value);
    }
    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
    public bool IsCredit
    {
        get => (bool)GetValue(IsCreditProperty);
        set => SetValue(IsCreditProperty, value);
    }
    public TransactionCard()
    {
        InitializeComponent();
        Card.BindingContext = this;
        UpdateAmountColor();
    }
    private void UpdateAmountColor()
    {
        AmountLabel.TextColor = Res.Get(IsCredit ? "Primary" : "TcText");
    }
}