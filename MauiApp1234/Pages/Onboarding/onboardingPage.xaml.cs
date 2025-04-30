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
        private async void OnGetStartedClicked(object sender, EventArgs e)
        {
            Preferences.Set("HasSeenOnboarding", true);

            // Navigate to dashboard tab (or any tab)
            await Shell.Current.GoToAsync("//dashboardPage");
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

           

            // Slide out the current page (move it off the screen to the left)
            await this.TranslateTo(-this.Width, 0, 250, Easing.CubicInOut);

            // Navigate to login within Shell
            await Shell.Current.GoToAsync("///loginPage");

            // After navigating, slide in the new page from the right (you can handle this on the new page if desired)
            // If you want the new page to have a slide effect as well, handle this on the page's Appearing event.
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