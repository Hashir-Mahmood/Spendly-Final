namespace MauiApp1234
{
    public partial class App : Application
    {
        // Add a static property to access the TTS service from anywhere
        

        // Modify constructor to accept the TTS service
        public App()
        {
            InitializeComponent();

            // Store the TTS service
          
            // Register Syncfusion License
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
                "Ngo9BigBOggjHTQxAR8/V1NNaF5cXmtCe0x0RXxbf1x1ZFRHalxVTnRdUiweQnxTdEBjXX1ecXZWQ2VfVUJyW0lfag==");

            // Set the main page to AppShell
            MainPage = new AppShell();

            // Navigate to onboarding if not seen before
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool hasSeenOnboarding = Preferences.Get("HasSeenOnboarding", false);
                if (!hasSeenOnboarding)
                {
                    await Shell.Current.GoToAsync("///onboarding");
                }
                else
                {
                    // Navigate to a tab page if onboarding has been seen
                    await Shell.Current.GoToAsync("///loginPage");
                }
            });
        }
    }
}