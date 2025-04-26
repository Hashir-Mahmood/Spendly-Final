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
        // Get or set whether TTS is enabled. Stored in Preferences to persist across sessions.
        public bool IsEnabled
        {
            get => Preferences.Get("TtsEnabled", false); // Default is false if no preference is set
            set => Preferences.Set("TtsEnabled", value); // Save the setting when changed
        }

        // Speak the given text asynchronously with optional speech options.
        public async Task SpeakAsync(string text)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(text)) // Ensure TTS is enabled and text is valid
                return;

            var options = new SpeechOptions
            {
                Volume = 1.0f, // Default volume (you could expose this as a setting in your app)
                Pitch = 1.0f   // Default pitch (this too could be made dynamic)
            };

            // Ensure TTS is available and ready
            try
            {
                await TextToSpeech.SpeakAsync(text, options);
            }
            catch (Exception ex)
            {
                // Handle any potential errors, such as TTS being unavailable
                Console.WriteLine($"Error during speech: {ex.Message}");
            }
        }

        // Stop any current TTS speech asynchronously.
        public async Task StopSpeakingAsync()
        {
            try
            {
                await TextToSpeech.SpeakAsync(string.Empty); // Empty string can stop the current speech
            }
            catch (Exception ex)
            {
                // Log any errors during stop operation
                Console.WriteLine($"Error stopping speech: {ex.Message}");
            }
        }
    }
}
