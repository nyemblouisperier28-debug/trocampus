using Trocampus.Helpers;
using Trocampus.Models;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;
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
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadListingsAsync();
    }

    private async Task LoadListingsAsync()
    {
        try
        {
            var api = App.Services.GetRequiredService<CampusApiService>();
            var listings = await api.GetListingsAsync();
            ListingsResults.Children.Clear();
            ListingCountLabel.Text = $"Objets disponibles · {listings.Count}";
            foreach (var listing in listings)
            {
                var card = new Trocampus.Controls.ListingCard
                {
                    PhotoSource = "photo_manuels.png",
                    Title = listing.Title,
                    Meta = $"{listing.Owner} · {listing.Campus} · {listing.Zone}",
                    Offer = listing.ListingType == "exchange"
                        ? $"Échange · {listing.WantedText ?? "proposition ouverte"}"
                        : $"Vente · {(listing.PriceCents is null ? "" : $"{listing.PriceCents / 100m:0.00} €")}{(listing.PricePoints is null ? "" : $" · {listing.PricePoints} points")}".Trim()
                };
                card.Tapped += (_, _) =>
                {
                    api.SelectListing(listing.Id);
                    _ = Nav.GoAsync("//listing-detail");
                };
                ListingsResults.Children.Add(card);
            }
            if (listings.Count == 0)
                ListingsResults.Children.Add(new Label { Text = "Aucune annonce ne correspond pour le moment.", Style = (Style)Resources["TcMeta12"] });
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de charger les annonces.", "OK");
        }
    }
}