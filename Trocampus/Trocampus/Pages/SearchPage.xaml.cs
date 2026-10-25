using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Trocampus.Pages;

public partial class SearchPage : ContentPage
{
    public SearchPage()
    {
        InitializeComponent();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RunSearchAsync();
    }

    private async void OnSearchCompleted(object? sender, EventArgs e) => await RunSearchAsync();
    private async void OnSearchClicked(object? sender, EventArgs e) => await RunSearchAsync();
    private async Task RunSearchAsync()
    {
        try
        {
            var api = App.Services.GetRequiredService<CampusApiService>();
            var zones = await api.GetZonesAsync();
            var zoneName = ZoneField.Value.Split('·').LastOrDefault()?.Trim();
            var zoneId = ZoneField.Value == "Toutes les zones" ? null : zones.FirstOrDefault(zone => zone.Name == zoneName)?.Id;
            int? radiusMeters = RadiusField.Value switch { "500 m" => 500, "1 km" => 1000, "2 km" => 2000, "5 km" => 5000, _ => null };
            var type = TypeField.Value switch { "Vente" => "sale", "Échange" => "exchange", _ => null };
            var results = await api.GetListingsAsync(QueryEntry.Text, zoneId, type, radiusMeters);
            SearchResults.Children.Clear();
            ResultsCountLabel.Text = $"{results.Count} résultat(s)";
            foreach (var listing in results)
            {
                var card = new Trocampus.Controls.ListingCard
                {
                    PhotoSource = "photo_manuels.png",
                    Title = listing.Title,
                    Meta = $"{listing.Owner} · {listing.Campus} · {listing.Zone}",
                    Offer = listing.ListingType == "exchange" ? $"Échange · {listing.WantedText ?? "proposition ouverte"}" : $"Vente · {(listing.PriceCents ?? 0) / 100m:0.00} €"
                };
                card.Tapped += (_, _) =>
                {
                    api.SelectListing(listing.Id);
                    _ = Nav.GoAsync("//listing-detail");
                };
                SearchResults.Children.Add(card);
            }
            if (results.Count == 0) SearchResults.Children.Add(new Label { Text = "Aucune annonce trouvée." });
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de lancer la recherche.", "OK");
        }
    }
    private async void OnUseLocationTapped(object? sender, TappedEventArgs e) =>
        await DisplayAlert("Recherche par zone", "La recherche utilise les zones du campus. La position GPS précise n’est pas encore enregistrée par le compte.", "OK");
    // La maquette n’a pas d’écran de détail d’annonce : route à créer plus tard.
    private async void OnListingTapped(object? sender, EventArgs e) =>
        await Nav.GoAsync("//listing-detail");
    private async void OnAllListingsTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//listings");
}