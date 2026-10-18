namespace Trocampus.Controls;

public partial class BottomTabBar : ContentView
{
    // Valeurs possibles : "home", "search", "exchanges", "profile"
    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab), typeof(string), typeof(BottomTabBar), "home",
        propertyChanged: (b, _, _) => ((BottomTabBar)b).Refresh());
    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }
    public BottomTabBar()
    {
        InitializeComponent();
        Refresh();
    }
    private void Refresh()
    {
        HomeTab.IsActive = ActiveTab == "home";
        SearchTab.IsActive = ActiveTab == "search";
        ExchangesTab.IsActive = ActiveTab == "exchanges";
        ProfileTab.IsActive = ActiveTab == "profile";
    }
}