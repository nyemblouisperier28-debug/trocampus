namespace Trocampus.Controls;

public partial class ExchangeResultCard : ContentView
{
    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date), typeof(string), typeof(ExchangeResultCard), string.Empty);
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ExchangeResultCard), string.Empty);
    public static readonly BindableProperty PartnerProperty = BindableProperty.Create(
        nameof(Partner), typeof(string), typeof(ExchangeResultCard), string.Empty);
    public static readonly BindableProperty NoteProperty = BindableProperty.Create(
        nameof(Note), typeof(string), typeof(ExchangeResultCard), string.Empty,
        propertyChanged: (b, _, n) =>
            ((ExchangeResultCard)b).HasNote = !string.IsNullOrEmpty((string)n));
    public static readonly BindableProperty HasNoteProperty = BindableProperty.Create(
        nameof(HasNote), typeof(bool), typeof(ExchangeResultCard), false);
    public static readonly BindableProperty IsSuccessProperty = BindableProperty.Create(
        nameof(IsSuccess), typeof(bool), typeof(ExchangeResultCard), true,
        propertyChanged: (b, _, n) =>
        {
            var card = (ExchangeResultCard)b;
            card.ResultText = (bool)n ? "Réussi" : "Échoué";
            card.ResultKind = (bool)n ? StatusKind.Success : StatusKind.Danger;
        });
    public static readonly BindableProperty ResultTextProperty = BindableProperty.Create(
        nameof(ResultText), typeof(string), typeof(ExchangeResultCard), "Réussi");
    public static readonly BindableProperty ResultKindProperty = BindableProperty.Create(
        nameof(ResultKind), typeof(StatusKind), typeof(ExchangeResultCard), StatusKind.Success);
    public string Date
    {
        get => (string)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string Partner
    {
        get => (string)GetValue(PartnerProperty);
        set => SetValue(PartnerProperty, value);
    }
    public string Note
    {
        get => (string)GetValue(NoteProperty);
        set => SetValue(NoteProperty, value);
    }
    public bool HasNote
    {
        get => (bool)GetValue(HasNoteProperty);
        private set => SetValue(HasNoteProperty, value);
    }
    public bool IsSuccess
    {
        get => (bool)GetValue(IsSuccessProperty);
        set => SetValue(IsSuccessProperty, value);
    }
    public string ResultText
    {
        get => (string)GetValue(ResultTextProperty);
        private set => SetValue(ResultTextProperty, value);
    }
    public StatusKind ResultKind
    {
        get => (StatusKind)GetValue(ResultKindProperty);
        private set => SetValue(ResultKindProperty, value);
    }
    public ExchangeResultCard()
    {
        InitializeComponent();
        Card.BindingContext = this;
    }
}