using System.ComponentModel.DataAnnotations;
using MySqlConnector;


namespace MauiApp1234
{
    public partial class LogInPage2 : ContentPage
    {
        // Constructor
        public LogInPage2()
        {
            InitializeComponent();
        }

        // Sign-In Details Class
        public class SignInDetails
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        // Forgot Password Navigation
        private void ForgotPassword_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new LogInPage3());
        }

        // Sign-In Logic
        private void SignIn_Clicked(object sender, EventArgs e)
        {
            // Make previous errors invisible
            ErrorLabel.IsVisible = false;

            // Validate email
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                ShowError("You have not filled the email field correctly.");
                return;
            }
            else if (!IsValidEmail(EmailEntry.Text))
            {
                ShowError("Please enter a valid email.");
                return;
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                ShowError("Password is not filled correctly.");
                return;
            }
            else if (PasswordEntry.Text.Length < 8)
            {
                ShowError("Password must have at least 8 characters.");
                return;
            }

            // Database connection string
            string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";

            using (var conn = new MySqlConnection(connString))
            {
                try
                {
                    conn.Open();

                    // SQL query to validate credentials and fetch customerId
                    string query = "SELECT customer_id FROM customer WHERE LOWER(email) = LOWER(@Email) AND password = @Password";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Add parameters to prevent SQL injection
                        cmd.Parameters.AddWithValue("@Email", EmailEntry.Text.Trim());
                        cmd.Parameters.AddWithValue("@Password", PasswordEntry.Text.Trim());

                        // Execute query and fetch customerId
                        var customer_id = cmd.ExecuteScalar();
                        if (customer_id != null)
                        {
                            // Save customerId in app memory using Preferences
                            Preferences.Set("customer_id", customer_id.ToString());

                            Console.WriteLine($"Logged in user ID: {customer_id}"); // Debugging log

                            // Navigate to Budgeting page
                            Navigation.PushAsync(new Budgeting());
                        }
                        else
                        {
                            ShowError("Invalid email or password.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log and display database error
                    Console.WriteLine($"Database error: {ex.Message}"); // Debugging log
                    ShowError($"Database error: {ex.Message}");
                }
            }
        }

        // Error Display
        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        // Email Validation Logic
        private bool IsValidEmail(string email)
        {
            // Validate email format using a regular expression
            var regex = new RegularExpressionAttribute(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");
            return regex.IsValid(email);
        }

        // Toggle Password Visibility
        private void TogglePasswordButton_Clicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            // You can also change the button image here
            // TogglePasswordButton.Source = PasswordEntry.IsPassword ? "eye_icon.png" : "eye_slash_icon.png";
        }

        // Gesture Recognizer for Navigation
        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            Navigation.PushAsync(new LogInPage1());
        }
    }
}