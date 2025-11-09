using agrify.Models; // We need this!
using System;

namespace agrify.Services
{
    public class CurrentUserService
    {
        // This is the singleton pattern. It creates a single, lazy-loaded
        // instance of this class that the whole app can share.
        private static readonly Lazy<CurrentUserService> _instance =
            new Lazy<CurrentUserService>(() => new CurrentUserService());

        public static CurrentUserService Instance => _instance.Value;

        // This is the property that will hold our user's data
        public User? CurrentUser { get; private set; }

        // A helper property to quickly check if someone is logged in
        public bool IsLoggedIn => CurrentUser != null;

        // Private constructor so no one else can create a new one
        private CurrentUserService() { }

        // This is called by the LoginViewModel on success
        public void Login(User user)
        {
            CurrentUser = user;
        }

        // You can call this from a "Logout" button later
        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
