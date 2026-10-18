using Trocampus.Models;
using Trocampus.PageModels;

namespace Trocampus.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}