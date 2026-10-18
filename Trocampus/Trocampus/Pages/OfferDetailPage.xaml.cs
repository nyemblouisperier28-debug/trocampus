using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class OfferDetailPage : ContentPage
{
	public OfferDetailPage() => InitializeComponent();

	private async void OnBackTapped(object? sender, TappedEventArgs e) =>
		await Nav.GoAsync("//offers");

	private async void OnMessagesTapped(object? sender, TappedEventArgs e) =>
		await Nav.GoAsync("//messages");

	private async void OnAcceptClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offer-accepted");

	private async void OnRefuseClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offer-refused");
}