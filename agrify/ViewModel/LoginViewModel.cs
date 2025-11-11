using System;
using System.Threading.Tasks;          // Needed for async
using agrify.Data;                     // Needed for our DbContext
using agrify.Models;                   // Needed for our User model
using agrify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Identity; // Needed for PasswordHasher
using Microsoft.EntityFrameworkCore;   // Needed for .FirstOrDefaultAsync()

namespace agrify.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // This event will be our signal to the View
        public event EventHandler? NavigationRequested;

        // --- NEW PROPERTIES ---

        // This attribute creates a "Username" property
        // that we can bind our TextBox to.
        [ObservableProperty]
        private string _username = string.Empty;

        // This creates a "Password" property
        // for our PasswordBox.
        [ObservableProperty]
        private string _password = string.Empty;

        // This creates an "ErrorMessage" property
        // that we can show to the user.
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // --- UPDATED COMMAND ---

        // The command is now an [AsyncRelayCommand]
        // because database calls should always be asynchronous!
        [RelayCommand]
        private async Task Login()
        {
            // 1. Check for empty boxes...
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please complete both fields.";
                return; // <-- STOPS HERE
            }

            try
            {
                // 2. Connect to database...
                await using var db = new AgrifyDbContext();

                // 3. Find the user...
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == Username);

                if (user == null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return; // <-- STOPS HERE
                }

                // 4. Check the password...
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, Password);

                // 5. THIS IS THE "GATE"
                if (result == PasswordVerificationResult.Success)
                {
                    CurrentUserService.Instance.Login(user);
                    // *** ONLY IF THE PASSWORD IS CORRECT ***
                    // Do we let them in.

                    NavigationRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ErrorMessage = "Invalid username or password.";
                    return; // <-- STOPS HERE
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{ex.Message}";
                return; // <-- STOPS HERE
            }
        }

        [RelayCommand]
        private void GenerateHash()
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please type a password in the box first.";
                return;
            }

            // This is the SAME hasher we use to check,
            // but now we're using it to CREATE.
            var hasher = new PasswordHasher<User>();

            // We pass null for the 'user' because we don't need a user
            // to create a new hash.
            var newHash = hasher.HashPassword(null, Password);

            // We'll display the new hash in the error message box!
            ErrorMessage = newHash;
        }
    }
}
