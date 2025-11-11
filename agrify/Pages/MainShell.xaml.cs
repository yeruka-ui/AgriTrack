using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using agrify.ViewModel;
using agrify.Services;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace agrify.Pages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainShell : Page
{

    public MainShellViewModel ViewModel => (MainShellViewModel)this.DataContext;

    public MainShell()
    {
        this.InitializeComponent();

        SetInitialPageBasedOnRole();
    }

    private void SetInitialPageBasedOnRole()
    {
        // Get the role from our session service
        string role = CurrentUserService.Instance.CurrentUser?.Role ?? "owner"; // Default to owner if something is wrong

        // Navigate to the correct default page AND check the correct button
        switch (role)
        {
            case "owner":
                ContentFrame.Navigate(typeof(DashboardPage));
                DashboardButton.IsChecked = true;
                break;

            case "manager":
                // A manager's default view should be their main job
                ContentFrame.Navigate(typeof(LivestockPage));
                LivestockButton.IsChecked = true;
                break;

            case "accountant":
                // An accountant's default view
                ContentFrame.Navigate(typeof(FiscalPage));
                ExpensesButton.IsChecked = true;
                break;

            default:
                ContentFrame.Navigate(typeof(DashboardPage));
                DashboardButton.IsChecked = true;
                break;
        }
    }

    private void Navigation_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton checkedButton)
        {
            // Get the 'Tag' we assigned in the XAML
            string pageTag = checkedButton.Tag?.ToString();

            Type pageType = pageTag switch
            {
                "dashboard" => typeof(DashboardPage),
                //"calendar" => typeof(CalendarPage),
                "livestock" => typeof(LivestockPage),
               "produce" => typeof(ProducePage), 
                "supplies" => typeof(SuppliesPage),
                //"fiscal" => typeof(FiscalPage),
                //"reports" => typeof(ReportsPage),
                //"sales" => typeof(SalesRecordPage), // Assuming you create SalesRecordPage
                _ => typeof(DashboardPage) // Default
            };

            // Navigate the *internal* Frame, not the whole app
            ContentFrame.Navigate(pageType);
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        // Clear the user session
        Services.CurrentUserService.Instance.Logout();

        // Navigate the *entire app* back to the LoginPage
        // We use this.Frame here to get the app's main frame,
        // not the internal ContentFrame.
        this.Frame.Navigate(typeof(LoginPage));
    }
}
