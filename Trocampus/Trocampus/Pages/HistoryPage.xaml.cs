using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }
    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//exchanges");
    private async void OnAppointmentsTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//exchanges");
    private async void OnPointsTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//points");
}