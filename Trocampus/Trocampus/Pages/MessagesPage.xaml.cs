using Trocampus.Helpers;
using Trocampus.Services;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
namespace Trocampus.Pages;

public partial class MessagesPage : ContentPage
{
    private long? activeConversationId;
    public MessagesPage()
    {
        InitializeComponent();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadConversationsAsync();
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            var api = App.Services.GetRequiredService<CampusApiService>();
            var conversations = await api.GetConversationsAsync();
            ConversationsList.Children.Clear();
            foreach (var conversation in conversations)
            {
                var row = new Button
                {
                    Text = $"{conversation.OtherParty} · {conversation.ListingTitle}\n{conversation.LastMessage ?? "Aucun message"} · {conversation.UnreadCount} non lu(s)",
                    Style = (Style)Resources["TcSecondaryButton"],
                    HorizontalOptions = LayoutOptions.Fill
                };
                row.Clicked += async (_, _) => await ShowConversationAsync(conversation.Id, conversation.OtherParty);
                ConversationsList.Children.Add(row);
            }
            if (conversations.Count == 0) ConversationsList.Children.Add(new Label { Text = "Aucune conversation pour le moment.", Style = (Style)Resources["TcMeta12"] });
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Serveur indisponible", "Impossible de charger les conversations.", "OK");
        }
    }

    private async Task ShowConversationAsync(long conversationId, string otherParty)
    {
        try
        {
            var api = App.Services.GetRequiredService<CampusApiService>();
            var messages = await api.GetMessagesAsync(conversationId);
            activeConversationId = conversationId;
            MessagesList.Children.Clear();
            MessagesList.Children.Add(new Label { Text = $"Conversation avec {otherParty}", Style = (Style)Resources["TcSectionTitle"] });
            foreach (var message in messages ?? [])
                MessagesList.Children.Add(new Label { Text = $"{message.Sender}: {message.Body}", Style = (Style)Resources["TcBody14"] });
            MessagesList.IsVisible = true;
            MessageComposer.IsVisible = true;
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Messages indisponibles", "Impossible de charger cette conversation.", "OK");
        }
    }

    private async void OnSendMessageClicked(object? sender, EventArgs e)
    {
        if (activeConversationId is not long conversationId || string.IsNullOrWhiteSpace(MessageEntry.Text)) return;
        try
        {
            await App.Services.GetRequiredService<CampusApiService>().SendMessageAsync(conversationId, MessageEntry.Text.Trim());
            MessageEntry.Text = string.Empty;
            await LoadConversationsAsync();
            await ShowConversationAsync(conversationId, "la conversation");
        }
        catch (HttpRequestException)
        {
            await DisplayAlert("Envoi impossible", "Le message n’a pas pu être envoyé.", "OK");
        }
    }
    private async void OnAppointmentTapped(object? sender, TappedEventArgs e) =>
        await Nav.GoAsync("//exchanges");
}