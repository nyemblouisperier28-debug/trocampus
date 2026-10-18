using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class ListingsPage : ContentPage
{
    public ListingsPage()
    {
        InitializeComponent();
    }
    private async void OnSearchCompleted(object? sender, EventArgs e) => await Nav.GoAsync("//search");
    private async void OnPublishClicked(object? sender, EventArgs e) => await Nav.GoAsync("//publish");
    // La maquette n’a pas d’écran de détail d’annonce : route à créer plus tard.
    private async void OnListingTapped(object? sender, EventArgs e) => await Nav.GoAsync("//listing-detail");
    private async void OnMyListingsTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//offers");
}