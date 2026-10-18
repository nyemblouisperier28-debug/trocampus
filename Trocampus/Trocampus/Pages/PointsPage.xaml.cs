using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class PointsPage : ContentPage
{
    public PointsPage()
    {
        InitializeComponent();
    }
    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Nav.GoAsync("//profile");
}