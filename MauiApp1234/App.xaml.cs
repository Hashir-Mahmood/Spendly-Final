namespace MauiApp1234
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmtCe0x0RXxbf1x1ZFRHalxVTnRdUiweQnxTdEBjXX1ecXZWQ2VfVUJyW0lfag==");

            MainPage = new AppShell();
        }
    }
}
