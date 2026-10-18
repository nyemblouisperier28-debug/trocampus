using Trocampus.Helpers;

namespace Trocampus.Pages;

public partial class OfferRefusedPage : ContentPage
{
	public OfferRefusedPage() => InitializeComponent();

	private async void OnOffersClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offers");

	private async void OnListingsClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//listings");
}