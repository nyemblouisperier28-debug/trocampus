namespace Trocampus.Controls;

public partial class SelectField : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(SelectField), string.Empty);
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(SelectField), string.Empty, BindingMode.TwoWay);
    // Options séparées par des virgules : Options="Plus récents,Distance"
    public static readonly BindableProperty OptionsProperty = BindableProperty.Create(
        nameof(Options), typeof(string), typeof(SelectField), string.Empty);
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public string Options
    {
        get => (string)GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }
    public event EventHandler<string>? ValueChanged;
    public SelectField()
    {
        InitializeComponent();
        Root.BindingContext = this;
    }
    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        var choices = Options.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null || choices.Length == 0)
            return;
        var choice = await page.DisplayActionSheet(Title, "Annuler", null, choices);
        if (string.IsNullOrEmpty(choice) || choice == "Annuler")
            return;
        Value = choice;
        ValueChanged?.Invoke(this, choice);
    }
}