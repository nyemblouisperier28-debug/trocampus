using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class SearchPage : ContentPage
{
    public SearchPage()
    {
        InitializeComponent();
    }
    private void OnSearchCompleted(object? sender, EventArgs e) => RunSearch();
    private void OnSearchClicked(object? sender, EventArgs e) => RunSearch();
    private void RunSearch()
    {
        // À brancher sur votre source de données : QueryEntry.Text, zone, rayon, type, filtre.
    }
    private void OnUseLocationTapped(object? sender, TappedEventArgs e)
    {
        // À brancher sur Geolocation pour remplacer la zone par la position actuelle.
    }
    // La maquette n’a pas d’écran de détail d’annonce : route à créer plus tard.
    private async void OnListingTapped(object? sender, EventArgs e) =>
        await Nav.GoAsync("//listing-detail");
    private async void OnAllListingsTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//listings");
}