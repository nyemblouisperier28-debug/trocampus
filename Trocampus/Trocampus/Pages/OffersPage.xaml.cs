using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class OffersPage : ContentPage
{
	public OffersPage() => InitializeComponent();

	private async void OnOfferTapped(object? sender, EventArgs e) =>
		await Nav.GoAsync("//offer-detail");

	private async void OnListingsClicked(object? sender, EventArgs e) =>
		await Nav.GoAsync("//listings");
}