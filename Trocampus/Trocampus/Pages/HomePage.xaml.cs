using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }
    private async void OnSearchCompleted(object? sender, EventArgs e) => await Nav.GoAsync("//search");
    private async void OnPublishClicked(object? sender, EventArgs e) => await Nav.GoAsync("//publish");
    private async void OnPointsTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//points");
    private async void OnListingsTapped(object? sender, EventArgs e) => await Nav.GoAsync("//listings");
    private async void OnOffersTapped(object? sender, EventArgs e) => await Nav.GoAsync("//offers");
    private async void OnMessagesTapped(object? sender, EventArgs e) => await Nav.GoAsync("//messages");
    private async void OnHistoryTapped(object? sender, EventArgs e) => await Nav.GoAsync("//history");
}