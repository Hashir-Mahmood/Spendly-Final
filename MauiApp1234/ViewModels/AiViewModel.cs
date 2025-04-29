using MauiApp1234.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace MauiApp1234.ViewModels;

public class AiViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ChatMessage> Messages { get; set; } = new();

    private string _currentMessage = string.Empty;
    public string CurrentMessage
    {
        get => _currentMessage;
        set
        {
            _currentMessage = value;
            OnPropertyChanged(nameof(CurrentMessage));
            OnPropertyChanged(nameof(CanSend));
        }
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(CurrentMessage);
    public ICommand SendCommand { get; }

    public bool IsTyping
    {
        get => _isTyping;
        set
        {
            _isTyping = value;
            OnPropertyChanged(nameof(IsTyping));
        }
    }

    private bool _isTyping;
    private readonly HttpClient _httpClient;
    private const string OpenAIApiKey = "YOUR_OPENAI_API_KEY"; // Replace with your actual OpenAI API key

    public AiViewModel()
    {
        SendCommand = new Command(async () => await SendMessageAsync());
        _httpClient = new HttpClient();
    }

    private async Task SendMessageAsync()
    {
        var userMessage = new ChatMessage
        {
            Content = CurrentMessage,
            IsUser = true
        };

        Messages.Add(userMessage);
        var prompt = CurrentMessage;
        CurrentMessage = string.Empty;
        OnPropertyChanged(nameof(CurrentMessage));
        OnPropertyChanged(nameof(CanSend));
        IsTyping = true;

        try
        {
            var request = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAIApiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var options = new JsonDocumentOptions { AllowTrailingCommas = true };
                var result = JsonDocument.Parse(responseString, options);

                try
                {
                    // Access the response content properly
                    var aiContent = result.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    if (!string.IsNullOrEmpty(aiContent))
                    {
                        Messages.Add(new ChatMessage
                        {
                            Content = aiContent,
                            IsUser = false
                        });
                    }
                    else
                    {
                        Messages.Add(new ChatMessage
                        {
                            Content = "Empty response received from AI",
                            IsUser = false
                        });
                    }
                }
                catch (KeyNotFoundException ex)
                {
                    // This specific catch helps diagnose the "arg key not found" error
                    Messages.Add(new ChatMessage
                    {
                        Content = $"JSON parsing error: Could not find expected key. Response structure: {responseString}",
                        IsUser = false
                    });
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Messages.Add(new ChatMessage
                {
                    Content = $"API Error: {response.StatusCode}. {errorContent}",
                    IsUser = false
                });
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Content = $"Error: {ex.Message}",
                IsUser = false
            });
        }
        finally
        {
            IsTyping = false;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}