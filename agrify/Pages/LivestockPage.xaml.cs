// === 1. THESE 'using' STATEMENTS ARE NOW ALL INCLUDED ===
using agrify.Models;            // Lets the code know what a 'Livestock' is
using System.Collections.ObjectModel;  // Lets the code use 'ObservableCollection'
using System.Linq; // For checking duplicates
using System; // For StringComparison

// These are from your file (and are correct for WinUI/Uno)
using System.Collections.Generic;
using System.IO;
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

namespace agrify.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LivestockPage : Page
    {
        // === 2. MERGED PROPERTIES ===
        // This is the list that will hold all our livestock data.
        private ObservableCollection<Livestock> LivestockList;
        // This is the list that our ComboBox will show.
        public ObservableCollection<string> SpeciesList { get; set; }

        public LivestockPage()
        {
            this.InitializeComponent();

            // === 3. MERGED CONSTRUCTOR ===
            // --- Your existing code (GOOD) ---
            LivestockList = new ObservableCollection<Livestock>();
            LivestockDataGrid.ItemsSource = LivestockList;

            // (Optional) Add some sample data to prove it works
            LivestockList.Add(new Livestock
            {
                Species = "Cow",
                Gender = "Male",
                Weight = "500kg", // Assuming Weight is a string for now
                DateOfBirth = new DateTime(2020, 11, 5),
                Notes = "Recently Sick",
                Status = "Recovered",
                Activity = "Active"
            });

            // --- My new code (MERGED IN) ---
            // Initialize and set up the ComboBox list
            SpeciesList = new ObservableCollection<string>
            {
                "Cow",
                "Goat",
                "Chicken"
            };
            SpeciesComboBox.ItemsSource = SpeciesList;
            SpeciesComboBox.SelectedIndex = 0; // Default to 'Cow'
        }

        /// <summary>
        /// Handles the main "SUBMIT" button click for the form.
        /// === 4. THIS IS THE FULLY MERGED METHOD ===
        /// </summary>
        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageTextBlock.Text = ""; // Clear old errors

            // --- FORM VALIDATION (from my new code) ---
            if (SpeciesComboBox.SelectedItem == null)
            {
                ErrorMessageTextBlock.Text = "Please select an animal/species.";
                return;
            }

            if (string.IsNullOrWhiteSpace(WeightTextBox.Text))
            {
                ErrorMessageTextBlock.Text = "Weight is a required field.";
                return;
            }

            // You can add more checks here (e.g., for numeric weight)

            if (DobDatePicker.SelectedDate == null)
            {
                ErrorMessageTextBlock.Text = "Date of Birth is a required field.";
                return;
            }

            // --- DATA GATHERING (from your code, but updated) ---
            // 1. Get Gender
            string gender = (GenderMaleRadioButton.IsChecked == true) ? "Male" : "Female";

            // 2. Get Status
            string status = "Recovered"; // Default
            if (StatusSoldRadioButton.IsChecked == true) status = "Sold";
            if (StatusTreatmentRadioButton.IsChecked == true) status = "Under Treatment";

            // 3. Get Activity
            string activity = (ActivityActiveRadioButton.IsChecked == true) ? "Active" : "Inactive";

            // 4. Create the new Livestock object
            var newAnimal = new Livestock
            {
                // UPDATED to read from ComboBox and NotesTextBox
                Species = SpeciesComboBox.SelectedItem.ToString(),
                Weight = WeightTextBox.Text,
                DateOfBirth = DobDatePicker.Date.DateTime,
                Gender = gender,
                Status = status,
                Activity = activity,
                Notes = NotesTextBox.Text // Now reads from the new notes field
            };

            // 5. Add the new animal to our list (your existing logic)
            LivestockList.Add(newAnimal);

            // 6. (Optional) Clear the input fields
            SpeciesComboBox.SelectedIndex = 0; // Resets ComboBox
            WeightTextBox.Text = string.Empty;
            DobDatePicker.SelectedDate = null; // Clears DatePicker
            NotesTextBox.Text = string.Empty; // Clears Notes
            GenderMaleRadioButton.IsChecked = true;
            StatusRecoveredRadioButton.IsChecked = true;
            ActivityActiveRadioButton.IsChecked = true;
        }

        // === 5. THESE ARE THE NEW HELPER METHODS FOR THE '+' BUTTON ===

        /// <summary>
        /// Handles the click event for the "Add New Species" flyout button.
        /// </summary>
        private void SaveNewSpeciesButton_Click(object sender, RoutedEventArgs e)
        {
            NewSpeciesErrorTextBlock.Text = ""; // Clear old errors
            var newSpecies = NewSpeciesTextBox.Text.Trim();

            // 1. Validation: Check if empty
            if (string.IsNullOrWhiteSpace(newSpecies))
            {
                NewSpeciesErrorTextBlock.Text = "Species name cannot be empty.";
                return;
            }

            // 2. Validation: Check for duplicates (case-insensitive)
            if (SpeciesList.Any(s => s.Equals(newSpecies, StringComparison.OrdinalIgnoreCase)))
            {
                NewSpeciesErrorTextBlock.Text = "This species already exists.";
                return;
            }

            // 3. Success: Add to list, select it, and close the flyout
            SpeciesList.Add(newSpecies);
            SpeciesComboBox.SelectedItem = newSpecies;
            AddSpeciesFlyout.Hide();
            NewSpeciesTextBox.Text = ""; // Clear text for next time
        }

        /// <summary>
        /// Clears the error text when the user clicks the "+" button
        /// </summary>
        private void AddSpeciesButton_Click(object sender, RoutedEventArgs e)
        {
            NewSpeciesErrorTextBlock.Text = "";
            NewSpeciesTextBox.Text = "";
            // This just opens the flyout, which is defined in the XAML.
        }
    }
}
