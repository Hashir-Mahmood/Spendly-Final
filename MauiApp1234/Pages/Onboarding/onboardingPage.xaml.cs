using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace MauiApp1234
{
    public partial class onboardingPage : ContentPage
    {
        private const string Colour = "#FFFFFF";
        private int _currentPageIndex = 0;
        private ObservableCollection<OnboardingItem> _onboardingItems;

        public onboardingPage()
        {
            InitializeComponent();
            _onboardingItems = new ObservableCollection<OnboardingItem>();
            SetupOnboardingItems();
            onboardingCarousel.ItemsSource = _onboardingItems;
        }

        private void SetupOnboardingItems()
        {
            _onboardingItems = new ObservableCollection<OnboardingItem>
            {
                new OnboardingItem
                {
                    Title = "Simple Budgeting",
                    Description = "Budgeting That Fits Your Lifestyle",
                    ImageSource = "onboarding_image1.png",
                    Page1Color = Colour,
                    Page2Color = Colour,
                    Page3Color = Colour
                },
                new OnboardingItem
                {
                    Title = "Smart Insights",
                    Description = "Enjoy having a tailored financial plan",
                    ImageSource = "onboarding_image2.png",
                    Page1Color = Colour,
                    Page2Color = Colour,
                    Page3Color = Colour
                },
                new OnboardingItem
                {
                    Title = "Keep on Track",
                    Description = "Stay motivated with your streak",
                    ImageSource = "onboarding_image3.png",
                    Page1Color = Colour,
                    Page2Color = Colour,
                    Page3Color = Colour
                }
            };
        }

        private void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            if (e.CurrentItem is OnboardingItem item)
            {
                _currentPageIndex = _onboardingItems.IndexOf(item);

                // Update button text on the last page
                if (_currentPageIndex == _onboardingItems.Count - 1)
                {
                    NextButton.Text = "Get Started";
                }
                else
                {
                    NextButton.Text = "Next";
                }
            }
        }

        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (_currentPageIndex < _onboardingItems.Count - 1)
            {
                // Move to the next page
                onboardingCarousel.Position = _currentPageIndex + 1;
            }
            else
            {
                // On the last page, navigate to the main app
                NavigateToMainApp();
            }
        }

        private void OnSkipButtonClicked(object sender, EventArgs e)
        {
            // Skip all onboarding and go to main app
            NavigateToMainApp();
        }

        private async void NavigateToMainApp()
        {
            // Navigate to your main app page
            await Shell.Current.GoToAsync("///MainPage");

            // Or if not using Shell:
            // await Navigation.PushAsync(new MainPage());
        }
    }

    public class OnboardingItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageSource { get; set; } = string.Empty;
        public string Page1Color { get; set; } = string.Empty;
        public string Page2Color { get; set; } = string.Empty;
        public string Page3Color { get; set; } = string.Empty;
    }
}