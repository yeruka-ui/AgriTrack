using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using agrify.ViewModels; // Make sure to import your ViewModels
using System; // Required for System.EventArgs

namespace agrify;

public sealed partial class LoginPage : Page
{
    // A simple property to get our ViewModel
    public LoginViewModel ViewModel => (LoginViewModel)this.DataContext;

    public LoginPage()
    {
        this.InitializeComponent();

        // This is important! We must subscribe to the event
        // right when the page is loaded.
        this.Loaded += LoginPage_Loaded;
    }

    private void LoginPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            // Subscribe to the ViewModel's event
            ViewModel.NavigationRequested += ViewModel_NavigationRequested;
        }
    }

    // This method will be called when the ViewModel raises the event
    private void ViewModel_NavigationRequested(object? sender, EventArgs e)
    {
        // THIS is where the navigation happens!
        // We are telling the app's main "Frame" to navigate
        // to a new instance of the DashboardPage.
        this.Frame.Navigate(typeof(DashboardPage));

        if (ViewModel != null)
        {
            ViewModel.NavigationRequested -= ViewModel_NavigationRequested;
        }
    }
}
