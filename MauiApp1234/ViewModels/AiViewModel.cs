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

    private string _currentMessage;
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

    private const string OpenAIApiKey = "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";

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
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OpenAIApiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonDocument.Parse(responseString);
            var aiContent = result.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            Messages.Add(new ChatMessage
            {
                Content = aiContent,
                IsUser = false
            });
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
