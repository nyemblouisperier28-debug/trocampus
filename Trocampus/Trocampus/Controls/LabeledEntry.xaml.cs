namespace Trocampus.Controls;

public partial class LabeledEntry : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(LabeledEntry), string.Empty);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(LabeledEntry), string.Empty);
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(LabeledEntry), string.Empty,
        BindingMode.TwoWay);
    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
        nameof(Keyboard), typeof(Keyboard), typeof(LabeledEntry), Keyboard.Default);
    public static readonly BindableProperty HintProperty = BindableProperty.Create(
        nameof(Hint), typeof(string), typeof(LabeledEntry), string.Empty,
        propertyChanged: (b, _, n) =>
            ((LabeledEntry)b).HasHint = !string.IsNullOrEmpty((string)n));
    public static readonly BindableProperty HasHintProperty = BindableProperty.Create(
        nameof(HasHint), typeof(bool), typeof(LabeledEntry), false);
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(LabeledEntry), false,
        propertyChanged: (b, _, n) => ((LabeledEntry)b).IsTextHidden = (bool)n);
    public static readonly BindableProperty IsTextHiddenProperty = BindableProperty.Create(
        nameof(IsTextHidden), typeof(bool), typeof(LabeledEntry), false);
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }
    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }
    public bool HasHint
    {
        get => (bool)GetValue(HasHintProperty);
        private set => SetValue(HasHintProperty, value);
    }
    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }
    public bool IsTextHidden
    {
        get => (bool)GetValue(IsTextHiddenProperty);
        private set => SetValue(IsTextHiddenProperty, value);
    }
    public LabeledEntry()
    {
        InitializeComponent();
        Root.BindingContext = this;
    }
    private void OnToggleEye(object? sender, TappedEventArgs e) => IsTextHidden = !IsTextHidden;
}