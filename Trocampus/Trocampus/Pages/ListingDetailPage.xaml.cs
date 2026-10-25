using Trocampus.Helpers;
using Trocampus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Trocampus.Pages;

public partial class ListingDetailPage : ContentPage
{
    public ListingDetailPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var api = App.Services.GetRequiredService<CampusApiService>();
        if (api.CurrentListingId is not long listingId) return;
        try
        {
            var listing = (await api.GetListingsAsync()).FirstOrDefault(item => item.Id == listingId);
            if (listing is null) return;
            ListingTitleLabel.Text = listing.Title;
            ListingMetaLabel.Text = $"{listing.ListingType} · {listing.Condition ?? "état non renseigné"} · {listing.Campus} · {listing.Zone}";
            ListingDescriptionLabel.Text = listing.Description ?? listing.WantedText ?? "Aucune description.";
            ListingOwnerLabel.Text = listing.Owner;
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de charger cette annonce.", "OK");
        }
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//listings");

    private async void OnProposeClicked(object? sender, EventArgs e)
    {
        var api = App.Services.GetRequiredService<CampusApiService>();
        if (api.CurrentListingId is not long listingId) return;
        var text = await DisplayPromptAsync("Proposer un échange", "Nombre de points à proposer", "Envoyer", "Annuler", keyboard: Keyboard.Numeric, initialValue: "0");
        if (!int.TryParse(text, out var points) || points < 0) return;
        try
        {
            await api.CreateOfferAsync(listingId, points, "Proposition envoyée depuis l’application");
            await DisplayAlert("Proposition envoyée", "Le propriétaire peut maintenant l’accepter ou la refuser.", "OK");
            await Nav.GoAsync("//offers");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Proposition impossible", "La proposition n’a pas pu être enregistrée.", "OK");
        }
    }

    private async void OnMessageClicked(object? sender, EventArgs e) =>
        await Nav.GoAsync("//messages");
}
