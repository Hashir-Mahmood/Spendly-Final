namespace MauiApp1234
{
    public partial class MainPage : ContentPage
    {
      

        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new QuizPage2());

        }

        private void Button1_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new InfoHub1());
        }

       
    }

}
