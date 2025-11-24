using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using agrify.ViewModels;

namespace agrify.Pages
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            this.InitializeComponent();
            ViewModel = new DashboardViewModel();
            this.DataContext = ViewModel;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataAsync();
        }
    }
}
