using Microsoft.Maui.Media;

namespace MauiApp1234
{
    public interface ITtsService
    {
        Task SpeakAsync(string text);
        Task StopSpeakingAsync();
        bool IsEnabled { get; set; }
    }

    public class TtsService : ITtsService
    {
        public bool IsEnabled
        {
            get => Preferences.Default.Get("TtsEnabled", false);
            set => Preferences.Default.Set("TtsEnabled", value);
        }

        public async Task SpeakAsync(string text)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(text))
                return;

            var options = new SpeechOptions
            {
                Volume = 1.0f,
                Pitch = 1.0f
            };

            await TextToSpeech.Default.SpeakAsync(text, options);
        }

        public async Task StopSpeakingAsync()
        {
            await TextToSpeech.Default.SpeakAsync(string.Empty);
        }
    }
}