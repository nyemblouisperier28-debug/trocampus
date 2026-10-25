using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Trocampus.Services;
#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif
namespace Trocampus;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<AuthSession>();
        builder.Services.AddSingleton(_ => new HttpClient
        {
    #if ANDROID
                BaseAddress = new Uri("http://10.0.2.2:8081/")
    #else
                BaseAddress = new Uri("http://localhost:8081/")
    #endif
        });
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<CampusApiService>();
    #if MAUI_DEVFLOW
        builder.AddMauiDevFlowAgent();
    #endif
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "InterRegular");
                fonts.AddFont("OpenSans-Regular.ttf", "InterMedium");
                fonts.AddFont("OpenSans-Semibold.ttf", "InterSemiBold");
            });
        // Entry sans bordure native : le cadre est dessiné par le Border du LabeledEntry.
        EntryHandler.Mapper.AppendToMapping("TrocampusBorderless", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.Background = null;
            handler.PlatformView.SetPadding(0, 0, 0, 0);
#elif IOS || MACCATALYST
            handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
        });
#if DEBUG
        builder.Logging.AddDebug();
#endif
        var app = builder.Build();
        App.Services = app.Services;
        return app;
    }
}