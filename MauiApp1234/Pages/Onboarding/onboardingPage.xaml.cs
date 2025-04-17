using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MauiApp1234
{
    public partial class onboardingPage : ContentPage
    {
        private const string ActiveColor = "#1F2937";   // Dark color for selected dot
        private const string InactiveColor = "#E5E7EB"; // Light gray for unselected dots
        private int _currentPageIndex = 0;
        private ObservableCollection<OnboardingItem> _onboardingItems;
        public ObservableCollection<IndicatorModel> IndicatorStates { get; set; }

        public onboardingPage()
        {
            InitializeComponent();
            _onboardingItems = new ObservableCollection<OnboardingItem>();
            IndicatorStates = new ObservableCollection<IndicatorModel>();
            BindingContext = this;
            SetupOnboardingItems();
            onboardingCarousel.ItemsSource = _onboardingItems;
            UpdateIndicators();
        }

        private void SetupOnboardingItems()
        {
            _onboardingItems = new ObservableCollection<OnboardingItem>
            {
                new OnboardingItem
                {
                    Title = "Simple Budgeting",
                    Description = "Budgeting That Fits Your Lifestyle",
                    ImageSource = "asian1.png"
                },
                new OnboardingItem
                {
                    Title = "Smart Insights",
                    Description = "Enjoy having a tailored financial plan",
                    ImageSource = "wheelchair.png"
                },
                new OnboardingItem
                {
                    Title = "Keep on Track",
                    Description = "Stay motivated with your streak",
                    ImageSource = "track.png"
                }
            };
        }

        private void UpdateIndicators()
        {
            IndicatorStates.Clear();
            for (int i = 0; i < _onboardingItems.Count; i++)
            {
                IndicatorStates.Add(new IndicatorModel
                {
                    Color = i == _currentPageIndex ? ActiveColor : InactiveColor,
                    Width = i == _currentPageIndex ? 20 : 6
                });
            }
        }

        private void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            if (e.CurrentItem is OnboardingItem item)
            {
                _currentPageIndex = _onboardingItems.IndexOf(item);
                UpdateIndicators();
                // Change button text depending on page
                NextButton.Text = _currentPageIndex == _onboardingItems.Count - 1 ? "Get Started" : "Next";
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs e)
        {
            if (_currentPageIndex < _onboardingItems.Count - 1)
            {
                onboardingCarousel.Position = _currentPageIndex + 1;
            }
            else
            {
                await NavigateToMainAppWithAnimation();
            }
        }

        private async void OnSkipButtonClicked(object sender, EventArgs e)
        {
            await NavigateToMainAppWithAnimation();
        }

        private async Task NavigateToMainAppWithAnimation()
        {
            // Disable buttons during animation
            NextButton.IsEnabled = false;
            SkipButton.IsEnabled = false;

            // Create the login page wrapped in navigation
            var loginPage = new NavigationPage(new LogInPage1());

            // Animate current page fade out
            await this.FadeTo(0, 250, Easing.CubicInOut);

            // Set the main page (this happens instantly)
            Application.Current.MainPage = loginPage;

            // Ensure the login page starts invisible by setting opacity at the page level
            if (loginPage.CurrentPage is LogInPage1 page)
            {
                page.Opacity = 0;

                // Wait a tiny bit for the page to render
                await Task.Delay(50);

                // Fade in the login page
                await page.FadeTo(1, 350, Easing.CubicInOut);
            }
        }
    }

    public class OnboardingItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageSource { get; set; } = string.Empty;
    }

    public class IndicatorModel
    {
        public string Color { get; set; } = string.Empty;
        public double Width { get; set; }
    }
}