using System;
using System.Threading.Tasks;
using agrify.Data;
using agrify.Models;
using agrify.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace agrify.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        public event EventHandler? NavigationRequested;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please complete both fields.";
                return;
            }

            try
            {
                await using var db = new AgrifyDbContext();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == Username);

                if (user == null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }

                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, Password);

                if (result == PasswordVerificationResult.Success)
                {
                    CurrentUserService.Instance.Login(user);
                    NavigationRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{ex.Message}";
                return;
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

            var hasher = new PasswordHasher<User>();
            var newHash = hasher.HashPassword(null, Password);
            ErrorMessage = newHash;
        }
    }
}
