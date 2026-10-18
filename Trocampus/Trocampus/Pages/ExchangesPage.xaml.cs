using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class ExchangesPage : ContentPage
{
	public ExchangesPage() => InitializeComponent();

	private async void OnMessagesClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//messages");

	private async void OnHistoryClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//history");

	private async void OnOffersClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offers");
}