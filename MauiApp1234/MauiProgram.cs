using Microsoft.Extensions.Logging;
using MySqlConnector;
namespace MauiApp1234
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Outfit-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Outfit-Bold.ttf", "OpenSansBold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
