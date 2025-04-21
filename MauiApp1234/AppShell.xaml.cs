namespace MauiApp1234
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("onboarding", typeof(onboardingPage));
        }
    }
}
