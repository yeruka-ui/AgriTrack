using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using agrify.Services; // We need this to check the current user

namespace agrify.ViewModel;
public partial class MainShellViewModel : ObservableObject
{
    // --- Role-Based Access Properties ---

    // These properties will be bound to the Visibility of your sidebar buttons.
    // The "owner" can see everything.

    [ObservableProperty]
    private bool _canSeeManagerContent; // For Inventory/Supplies

    [ObservableProperty]
    private bool _canSeeAccountantContent; // For Fiscal/Expenses

    [ObservableProperty]
    private bool _canSeeAllContent; // For Owner

    [ObservableProperty]
    private string _currentUserName;

    public MainShellViewModel()
    {
        var user = CurrentUserService.Instance.CurrentUser;

        if (user != null)
        {
            CurrentUserName = user.Username;

            // Set permissions based on role
            string role = user.Role ?? string.Empty;

            CanSeeAllContent = role == "owner";
            CanSeeManagerContent = role == "owner" || role == "manager";
            CanSeeAccountantContent = role == "owner" || role == "accountant";
        }
        else
        {
            // Failsafe in case the user isn't set
            CurrentUserName = "Guest";
            CanSeeAllContent = false;
            CanSeeManagerContent = false;
            CanSeeAccountantContent = false;
        }
    }
}
