using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MauiApp1234;
using MySqlConnector;
public partial class InfoHub1 : ContentPage
{
    private InfoHubViewModel _viewModel;
    public InfoHub1()
    {
        InitializeComponent();
        string connString = "server=dbhost.cs.man.ac.uk;user=b66855mm;password=iTIfvSknLwQZHtrLaHMy4uTsM/UuEQvZfTqa0ei81+k;database=b66855mm";
        MySqlConnection conn = new MySqlConnection(connString);
        conn.Open();
        // Configurar servicios y ViewModel
        var articleService = new ArticleService();
        var videoService = new VideoService();
        _viewModel = new InfoHubViewModel(Navigation, articleService, videoService);

        // Asignar el ViewModel como contexto de datos
        BindingContext = _viewModel;
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

    private void ChatbotIcon_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void OnNotificationsIconTapped(object sender, TappedEventArgs e)
    {

    }

    private void SettingsIcon_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PushAsync(new Settings());
    }

    // Modelo para los artículos
    public class Article
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string WebUrl { get; set; }
        public string ReadTime { get; set; }
    }

    // Modelo para los videos
    public class Video
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ThumbnailUrl { get; set; }
        public string WebUrl { get; set; }
        public string ViewCount { get; set; }
    }

    // ViewModel para la página de InfoHub
    public class InfoHubViewModel : BindableObject
    {
        private ObservableCollection<Article> _featuredArticles;
        public ObservableCollection<Article> FeaturedArticles
        {
            get => _featuredArticles;
            set
            {
                _featuredArticles = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Video> _latestVideos;
        public ObservableCollection<Video> LatestVideos
        {
            get => _latestVideos;
            set
            {
                _latestVideos = value;
                OnPropertyChanged();
            }
        }

        public ICommand ArticleTappedCommand { get; private set; }
        public ICommand VideoTappedCommand { get; private set; }
        public ICommand ArticlePreferencesCommand { get; private set; }
        public ICommand RetakeQuizCommand { get; private set; }

        private readonly INavigation _navigation;
        private readonly IArticleService _articleService;
        private readonly IVideoService _videoService;

        public InfoHubViewModel(INavigation navigation, IArticleService articleService, IVideoService videoService)
        {
            _navigation = navigation;
            _articleService = articleService;
            _videoService = videoService;

            // Inicializar comandos
            ArticleTappedCommand = new Command<Article>(OnArticleTapped);
            VideoTappedCommand = new Command<Video>(OnVideoTapped);
            ArticlePreferencesCommand = new Command(OnArticlePreferences);
            RetakeQuizCommand = new Command(OnRetakeQuiz);

            // Cargar datos
            LoadArticlesAndVideos();
        }

        private async void LoadArticlesAndVideos()
        {
            try
            {
                // Cargar artículos y videos desde el servicio
                var articles = await _articleService.GetFeaturedArticlesAsync();
                var videos = await _videoService.GetLatestVideosAsync();

                FeaturedArticles = new ObservableCollection<Article>(articles);
                LatestVideos = new ObservableCollection<Video>(videos);
            }
            catch (Exception ex)
            {
                // Manejar errores de carga
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"No se pudieron cargar los datos: {ex.Message}", "OK");
            }
        }

        private async void OnArticleTapped(Article article)
        {
            if (article == null)
                return;

            try
            {
                // Abrir URL del artículo en el navegador
                await Browser.OpenAsync(article.WebUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                // Manejar errores al abrir el navegador
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"No se pudo abrir el artículo: {ex.Message}", "OK");
            }
        }

        private async void OnVideoTapped(Video video)
        {
            if (video == null)
                return;

            try
            {
                // Abrir URL del video en el navegador
                await Browser.OpenAsync(video.WebUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                // Manejar errores al abrir el navegador
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"No se pudo abrir el video: {ex.Message}", "OK");
            }
        }

        private async void OnArticlePreferences()
        {
            // Navegar a la página de preferencias de artículos
            await Shell.Current.GoToAsync("//articlepreferences");
        }

        private async void OnRetakeQuiz()
        {
            // Navegar a la página del quiz
            await Shell.Current.GoToAsync("//quiz");
        }
    }

    // Interfaces para los servicios
    public interface IArticleService
    {
        Task<List<Article>> GetFeaturedArticlesAsync();
        Task<List<Article>> GetArticlesByPreferencesAsync(List<string> preferences);
    }

    public interface IVideoService
    {
        Task<List<Video>> GetLatestVideosAsync();
    }

    // Implementación del servicio de artículos
    public class ArticleService : IArticleService
    {
        // En un escenario real, estos datos vendrían de una API o base de datos
        public Task<List<Article>> GetFeaturedArticlesAsync()
        {
            // Datos de ejemplo
            var articles = new List<Article>
            {
                new Article
                {
                    Id = "1",
                    Title = "Set goals for your money",
                    Description = "Learn how to set realistic financial goals and achieve them step by step",
                    ImageUrl = "article_cover1.png",
                    WebUrl = "https://www.incharge.org/financial-literacy/budgeting-saving/how-to-set-financial-goals/",
                    ReadTime = "5 min read"
                },
                new Article
                {
                    Id = "2",
                    Title = "Understanding investments ",
                    Description = "A beginner's guide to investment concepts and strategies",
                    ImageUrl = "financialstability.jpg",
                    WebUrl = "https://www.investopedia.com/articles/basics/06/invest1000.asp",
                    ReadTime = "8 min read"
                },
                new Article
                {
                    Id = "3",
                    Title = "Budgeting made simple",
                    Description = "Practical tips to create and maintain a budget that works",
                    ImageUrl = "article_cover3.png",
                    WebUrl = "https://www.ramseysolutions.com/budgeting/how-to-make-a-budget",
                    ReadTime = "6 min read"
                }
            };

            return Task.FromResult(articles);
        }

        public Task<List<Article>> GetArticlesByPreferencesAsync(List<string> preferences)
        {
            // Implementación real: filtrar artículos basados en preferencias del usuario
            // Aquí solo devolvemos los mismos artículos de ejemplo
            return GetFeaturedArticlesAsync();
        }
    }

    // Implementación del servicio de videos
    public class VideoService : IVideoService
    {
        public Task<List<Video>> GetLatestVideosAsync()
        {
            // Datos de ejemplo
            var videos = new List<Video>
            {
                new Video
                {
                    Id = "1",
                    Title = "Does saving money mean not purchasing what you want?",
                    Description = "Learn the fundamentals of long-term budgeting",
                    ThumbnailUrl = "article_cover3.png",
                    WebUrl = "https://www.youtube.com/watch?v=DuOZceJ_trg",
                    ViewCount = "1.2K views"
                },
                new Video
                {
                    Id = "2",
                    Title = "Investment strategies for beginners",
                    Description = "Simple strategies to start your investment journey",
                    ThumbnailUrl = "financialstability.jpg",
                    WebUrl = "https://www.youtube.com/watch?v=Ay4fmZdZqJE",
                    ViewCount = "850 views"
                },
                new Video
                {
                    Id = "3",
                    Title = "Understanding credit scores",
                    Description = "How credit scores work and how to improve yours",
                    ThumbnailUrl = "spendly_logo.png",
                    WebUrl = "https://www.youtube.com/watch?v=79vtf30O1fQ&pp=0gcJCdgAo7VqN5tD",
                    ViewCount = "2.5K views"
                }
            };

            return Task.FromResult(videos);
        }
    }
}