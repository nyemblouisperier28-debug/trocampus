using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class ListingDetailPage : ContentPage
{
    public ListingDetailPage() => InitializeComponent();

    private async void OnBackTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//listings");

    private async void OnProposeClicked(object? sender, EventArgs e) =>
        await Nav.GoAsync("//offer-detail");

    private async void OnMessageClicked(object? sender, EventArgs e) =>
        await Nav.GoAsync("//messages");
}
