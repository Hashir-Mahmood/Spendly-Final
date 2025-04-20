using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace MauiApp1234.Pages.AI
{
    public partial class Ai : ContentPage
    {
        public Ai()
        {
            InitializeComponent();
            BindingContext = new AiChatViewModel(Navigation);
        }
    }

    public class AiChatViewModel : INotifyPropertyChanged
    {
        private readonly INavigation _navigation;
        private string _currentMessage;

        public ObservableCollection<ChatMessage> Messages { get; }
        public ICommand SendCommand { get; }
        public ICommand BackCommand { get; }

        public string CurrentMessage
        {
            get => _currentMessage;
            set
            {
                if (_currentMessage != value)
                {
                    _currentMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AiChatViewModel(INavigation navigation)
        {
            _navigation = navigation;
            _currentMessage = string.Empty;
            Messages = new ObservableCollection<ChatMessage>();
            SendCommand = new Command(SendMessage);
            BackCommand = new Command(GoBack);

            // Example messages for demonstration
            Messages.Add(new ChatMessage { Content = "Hello!", IsUser = true });
            Messages.Add(new ChatMessage { Content = "Hi there! How can I help you today?", IsUser = false });
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage))
                return;

            // Add user message
            Messages.Add(new ChatMessage { Content = CurrentMessage, IsUser = true });

            // In a real app, you'd call your AI service here
            // For demo, just simulate AI response
            string userMessage = CurrentMessage;
            CurrentMessage = string.Empty;

            // Simulate network delay
            Task.Delay(1000).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Add AI response
                    Messages.Add(new ChatMessage
                    {
                        Content = $"I received: {userMessage}",
                        IsUser = false
                    });
                });
            });
        }

        private void GoBack()
        {
            _navigation.PopAsync();
        }
    }

    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsUser { get; set; }
        public bool IsNotUser => !IsUser;
    }
}