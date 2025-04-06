using System.Collections.ObjectModel;

namespace MauiApp1234;

public partial class InfoHub1 : ContentPage
{

    public InfoHub1()
    {
        InitializeComponent();
        BindingContext = new InfoHubViewModel();
    }
    public class Article
    {
        public string Title { get; set; }
        public string URL { get; set; }
    }

    public class InfoHubViewModel
    {
        public ObservableCollection<Article> Articles { get; set; }

        public InfoHubViewModel()
        {
            Articles = new ObservableCollection<Article>
        {
            new Article { Title = "Budgeting Basics", URL = "https://www.bbc.co.uk/" },
            new Article { Title = "Investing 101", URL = "https://www.bbc.co.uk/" },
            new Article { Title = "Saving Tips", URL = "https://www.bbc.co.uk/" }
        };
        }
    }


    private void Quiz_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new MainPage());
    }

    private void Category_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new InfoHub2());
    }

    private void SignOut_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new LogInPage1());
    }
}