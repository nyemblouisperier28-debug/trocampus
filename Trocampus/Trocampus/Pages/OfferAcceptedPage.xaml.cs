using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class OfferAcceptedPage : ContentPage
{
	public OfferAcceptedPage() => InitializeComponent();

	private async void OnExchangesClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//exchanges");

	private async void OnMessagesClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//messages");
}