using Trocampus.Helpers;
namespace Trocampus.Pages;

public partial class MessagesPage : ContentPage
{
    public MessagesPage()
    {
        InitializeComponent();
    }
    private void OnSendClicked(object? sender, EventArgs e) => SendMessage();
    private void OnSendTapped(object? sender, TappedEventArgs e) => SendMessage();
    private void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageEntry.Text))
            return;
        // À brancher sur votre service de messagerie.
        MessageEntry.Text = string.Empty;
    }
    private async void OnAppointmentTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//exchanges");
}