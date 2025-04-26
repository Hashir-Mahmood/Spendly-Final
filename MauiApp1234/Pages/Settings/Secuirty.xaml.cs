namespace MauiApp1234
{
    using MySqlConnector;
    using System;
    using System.Threading;
    using Microsoft.Maui.Controls;

    public partial class Secuirty : ContentPage
    {
        private Timer _timer;
        private DateTime _lastInteractionTime;
        private int _timeoutInSeconds;

        public Secuirty()
        {
            InitializeComponent();

            // MySQL connection string
            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            // Open connection to database inside a try-catch-finally block for safety
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connString))
                {
                    conn.Open();
                    // You can execute queries here if needed
                }
            }
            catch (Exception ex)
            {
                // Handle any errors here (for example, display an error message)
                Console.WriteLine($"Database connection failed: {ex.Message}");
            }

            // Set the default timeout to 30 seconds (slider value 0)
            _timeoutInSeconds = 30;
            _lastInteractionTime = DateTime.Now;

            // Start or restart the timeout logic
            RestartTimeout();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            // Navigate to the Login Page (ensure LogInPage3 is correct)
            Navigation.PushAsync(new LogInPage3());
        }

        // This method is called when the slider value changes
        private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            var slider = sender as Slider;
            int value = (int)slider.Value;
            _timeoutInSeconds = 30 * (value + 1); // Set timeout in seconds based on slider value

            // Restart the timer whenever the value changes
            RestartTimeout();
        }

        // Start or restart the timeout logic
        private void RestartTimeout()
        {
            _lastInteractionTime = DateTime.Now;

            // Dispose previous timer if exists
            _timer?.Dispose();

            // Start a new timer to check the timeout every second
            _timer = new Timer(CheckTimeout, null, 1000, 1000);
        }

        // Check if the timeout has been reached
        private void CheckTimeout(object state)
        {
            // Calculate the elapsed time
            var elapsedTime = DateTime.Now - _lastInteractionTime;

            if (elapsedTime.TotalSeconds >= _timeoutInSeconds)
            {
                // Timeout reached, navigate back to login
                MainThread.BeginInvokeOnMainThread(() => NavigateToLogin());
                _timer?.Dispose();
            }
        }

        // Navigate to the login screen
        private async void NavigateToLogin()
        {
            // Navigate to login page (replace with your actual login page name)
            await Navigation.PushAsync(new LogInPage1());
        }
    }
}
