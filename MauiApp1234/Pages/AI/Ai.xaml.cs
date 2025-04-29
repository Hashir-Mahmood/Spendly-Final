using System;

using Microsoft.Maui.Controls;

namespace MauiApp1234.Pages.AI
{
    public partial class Ai : ContentPage
    {
        public Ai()
        {
            InitializeComponent();
            
        }

        private void ReturnButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }
}