using agrify.Data;            // Ensure this namespace exists for AgrifyDbContext
using agrify.Models;
using agrify.Models.Category; // Ensure this exists for ItemCategory
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace agrify.Pages
{
    public sealed partial class LivestockPage : Page
    {
        private ObservableCollection<Livestock> LivestockList;
        public ObservableCollection<string> SpeciesList { get; set; }

        // The State Machine: Tracks which item is currently being edited
        private Livestock _selectedLivestock;

        public LivestockPage()
        {
            this.InitializeComponent();

            LivestockList = new ObservableCollection<Livestock>();
            SpeciesList = new ObservableCollection<string>();

            LivestockDataGrid.ItemsSource = LivestockList;
            SpeciesComboBox.ItemsSource = SpeciesList;

            ClearForm();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            LivestockList.Clear();
            SpeciesList.Clear();

            using (var db = new AgrifyDbContext())
            {
                // 1. Load Livestock
                var allLivestock = await db.Livestock.ToListAsync();
                foreach (var animal in allLivestock)
                {
                    LivestockList.Add(animal);
                }

                // 2. Load Species (Categories)
                var typesFromCategories = await db.ItemCategories
                    .Where(c => c.CategoryType == "Livestock")
                    .Select(c => c.Name)
                    .ToListAsync();

                // 3. Load existing types from Livestock table (fallback)
                var typesFromItems = await db.Livestock
                    .Select(l => l.Species)
                    .ToListAsync();

                // 4. Merge
                var allTypes = typesFromCategories.Union(typesFromItems)
                    .Distinct()
                    .OrderBy(n => n);

                foreach (var type in allTypes)
                {
                    SpeciesList.Add(type);
                }
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            // Gather Data from UI
            string gender = (GenderMaleRadioButton.IsChecked == true) ? "Male" : "Female";
            string activity = (ActivityActiveRadioButton.IsChecked == true) ? "Active" : "Inactive";

            string status = "Recovered";
            if (StatusSoldRadioButton.IsChecked == true) status = "Sold";
            if (StatusTreatmentRadioButton.IsChecked == true) status = "Under Treatment";

            using (var db = new AgrifyDbContext())
            {
                if (_selectedLivestock != null)
                {
                    // === UPDATE EXISTING ===
                    db.Livestock.Attach(_selectedLivestock); // Attach to context

                    _selectedLivestock.Species = SpeciesComboBox.SelectedItem.ToString();
                    _selectedLivestock.Weight = WeightTextBox.Text;
                    _selectedLivestock.DateOfBirth = DobDatePicker.Date.DateTime;
                    _selectedLivestock.Gender = gender;
                    _selectedLivestock.Status = status;
                    _selectedLivestock.Activity = activity;
                    _selectedLivestock.Notes = NotesTextBox.Text;

                    db.Livestock.Update(_selectedLivestock);
                    await db.SaveChangesAsync();

                    // UI Refresh
                    int index = LivestockList.IndexOf(_selectedLivestock);
                    if (index >= 0) LivestockList[index] = _selectedLivestock;

                    ErrorMessageTextBlock.Text = "Record updated successfully.";
                }
                else
                {
                    // === CREATE NEW ===
                    var newAnimal = new Livestock
                    {
                        Species = SpeciesComboBox.SelectedItem.ToString(),
                        Weight = WeightTextBox.Text,
                        DateOfBirth = DobDatePicker.Date.DateTime,
                        Gender = gender,
                        Status = status,
                        Activity = activity,
                        Notes = NotesTextBox.Text
                    };

                    db.Livestock.Add(newAnimal);
                    await db.SaveChangesAsync();

                    LivestockList.Add(newAnimal);
                    ErrorMessageTextBlock.Text = "New animal added successfully.";
                }
            }

            ExitEditMode();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLivestock == null)
            {
                ErrorMessageTextBlock.Text = "Please select a record to remove.";
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                db.Livestock.Remove(_selectedLivestock);
                await db.SaveChangesAsync();
            }

            LivestockList.Remove(_selectedLivestock);
            ExitEditMode(); // Clears selection
            ErrorMessageTextBlock.Text = "Record removed.";
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLivestock == null)
            {
                ErrorMessageTextBlock.Text = "Please select a record to edit.";
                return;
            }

            // Populate UI
            SpeciesComboBox.SelectedItem = _selectedLivestock.Species;
            WeightTextBox.Text = _selectedLivestock.Weight;
            DobDatePicker.SelectedDate = _selectedLivestock.DateOfBirth;
            NotesTextBox.Text = _selectedLivestock.Notes;

            // Handle Radio Buttons Logic
            if (_selectedLivestock.Gender == "Male") GenderMaleRadioButton.IsChecked = true;
            else GenderFemaleRadioButton.IsChecked = true;

            if (_selectedLivestock.Activity == "Active") ActivityActiveRadioButton.IsChecked = true;
            else ActivityInactiveRadioButton.IsChecked = true;

            switch (_selectedLivestock.Status)
            {
                case "Sold": StatusSoldRadioButton.IsChecked = true; break;
                case "Under Treatment": StatusTreatmentRadioButton.IsChecked = true; break;
                default: StatusRecoveredRadioButton.IsChecked = true; break;
            }

            // Toggle Buttons
            SubmitButton.Content = "UPDATE";
            EditButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;

            ErrorMessageTextBlock.Text = "Editing mode active.";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            ErrorMessageTextBlock.Text = "Edit cancelled.";
        }

        private void ExitEditMode()
        {
            _selectedLivestock = null;
            ClearForm();

            SubmitButton.Content = "SUBMIT";
            EditButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Collapsed;
            LivestockDataGrid.SelectedItem = null;
        }

        private void ClearForm()
        {
            SpeciesComboBox.SelectedIndex = -1;
            WeightTextBox.Text = string.Empty;
            DobDatePicker.SelectedDate = null;
            NotesTextBox.Text = string.Empty;

            // Reset Radios to Defaults
            GenderMaleRadioButton.IsChecked = true;
            StatusRecoveredRadioButton.IsChecked = true;
            ActivityActiveRadioButton.IsChecked = true;
        }

        private void LivestockDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only allow changing selection if we are NOT in the middle of an edit (Cancel button visible)
            // Or you can allow it but you must update the _selectedLivestock
            if (CancelButton.Visibility == Visibility.Collapsed)
            {
                _selectedLivestock = LivestockDataGrid.SelectedItem as Livestock;
            }
        }

        private bool ValidateForm()
        {
            ErrorMessageTextBlock.Text = "";
            if (SpeciesComboBox.SelectedItem == null)
            {
                ErrorMessageTextBlock.Text = "Select a species.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(WeightTextBox.Text))
            {
                ErrorMessageTextBlock.Text = "Weight is required.";
                return false;
            }
            if (DobDatePicker.SelectedDate == null)
            {
                ErrorMessageTextBlock.Text = "Date of Birth is required.";
                return false;
            }
            return true;
        }

        // === Flyout Logic for adding new Species ===
        private async void SaveNewSpeciesButton_Click(object sender, RoutedEventArgs e)
        {
            NewSpeciesErrorTextBlock.Text = "";
            var newSpecies = NewSpeciesTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newSpecies))
            {
                NewSpeciesErrorTextBlock.Text = "Name cannot be empty.";
                return;
            }
            if (SpeciesList.Any(s => s.Equals(newSpecies, StringComparison.OrdinalIgnoreCase)))
            {
                NewSpeciesErrorTextBlock.Text = "Exists already.";
                return;
            }

            // Save to DB
            using (var db = new AgrifyDbContext())
            {
                bool exists = await db.ItemCategories.AnyAsync(c => c.Name == newSpecies && c.CategoryType == "Livestock");
                if (!exists)
                {
                    db.ItemCategories.Add(new ItemCategory { Name = newSpecies, CategoryType = "Livestock" });
                    await db.SaveChangesAsync();
                }
            }

            SpeciesList.Add(newSpecies);
            SpeciesComboBox.SelectedItem = newSpecies;
            AddSpeciesFlyout.Hide();
            NewSpeciesTextBox.Text = "";
        }

        private void AddSpeciesButton_Click(object sender, RoutedEventArgs e)
        {
            NewSpeciesErrorTextBlock.Text = "";
            NewSpeciesTextBox.Text = "";
        }
    }
}
