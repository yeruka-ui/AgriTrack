using agrify.Data;
using agrify.Models;
using agrify.Models.Category;
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
        private System.Collections.Generic.List<Livestock> _masterList;
        public ObservableCollection<string> SpeciesList { get; set; }

        private Livestock _selectedLivestock;

        public LivestockPage()
        {
            this.InitializeComponent();

            LivestockList = new ObservableCollection<Livestock>();
            SpeciesList = new ObservableCollection<string>();
            _masterList = new System.Collections.Generic.List<Livestock>();

            LivestockDataGrid.ItemsSource = LivestockList;
            SpeciesComboBox.ItemsSource = SpeciesList;

            ClearForm();

            // Initialize the UI state logic
            Mode_Changed(null, null);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        // =========================================================
        // 1. DATA LOADING
        // =========================================================
        private async Task LoadDataAsync()
        {
            try
            {
                LivestockList.Clear();
                _masterList.Clear();
                SpeciesList.Clear();

                using (var db = new AgrifyDbContext())
                {
                    var allLivestock = await db.Livestock.ToListAsync();
                    _masterList.AddRange(allLivestock);

                    // Sort by ID descending (Newest first)
                    var orderedInitial = _masterList.OrderByDescending(x => x.Id);
                    foreach (var animal in orderedInitial)
                    {
                        LivestockList.Add(animal);
                    }

                    // Load Species for ComboBox
                    var typesFromCategories = await db.ItemCategories
                        .Where(c => c.CategoryType == "Livestock")
                        .Select(c => c.Name)
                        .ToListAsync();

                    var typesFromItems = await db.Livestock
                        .Select(l => l.Species)
                        .ToListAsync();

                    var allTypes = typesFromCategories.Union(typesFromItems)
                        .Distinct()
                        .OrderBy(n => n);

                    foreach (var type in allTypes) SpeciesList.Add(type);
                }
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Loading Error",
                    Content = $"The database failed to load:\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        // =========================================================
        // 2. UI MODE SWITCHING
        // =========================================================
        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            // Safety check: ensure UI elements exist before accessing properties
            if (QuantityBox == null || TagNumberBox == null) return;

            if (RadioIndividual.IsChecked == true)
            {
                // INDIVIDUAL MODE
                QuantityBox.Value = 1;
                QuantityBox.IsEnabled = false; // Locked to 1

                TagNumberBox.Header = "Tag Number (Required)";
                TagNumberBox.PlaceholderText = "e.g., A-101";
            }
            else
            {
                // BATCH MODE
                QuantityBox.IsEnabled = true;
                // Ensure valid start value
                if (QuantityBox.Value < 2) QuantityBox.Value = 2;

                TagNumberBox.Header = "Batch Name (Optional)";
                TagNumberBox.PlaceholderText = "e.g., Chicks Sept 2024";
            }
        }

        // =========================================================
        // 3. SUBMIT LOGIC
        // =========================================================
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            string gender = (GenderMaleRadioButton.IsChecked == true) ? "Male" : "Female";
            string activity = (ActivityActiveRadioButton.IsChecked == true) ? "Active" : "Inactive";
            string status = StatusSoldRadioButton.IsChecked == true ? "Sold" :
                            StatusTreatmentRadioButton.IsChecked == true ? "Under Treatment" : "Recovered";

            using (var db = new AgrifyDbContext())
            {
                Livestock animalToSave;
                bool isNew = (_selectedLivestock == null);

                if (isNew)
                {
                    animalToSave = new Livestock();
                    db.Livestock.Add(animalToSave);
                }
                else
                {
                    animalToSave = await db.Livestock.FindAsync(_selectedLivestock.Id);
                    if (animalToSave == null) return;
                }

                // === MAPPING ===
                animalToSave.Species = SpeciesComboBox.SelectedItem?.ToString() ?? SpeciesComboBox.Text;

                // FIX 1: Parse to Decimal safely
                if (decimal.TryParse(WeightTextBox.Text, out decimal w))
                    animalToSave.Weight = w;
                else
                    animalToSave.Weight = 0m;

                animalToSave.DateOfBirth = DobDatePicker.Date.DateTime;
                animalToSave.Gender = gender;
                animalToSave.Status = status;
                animalToSave.Activity = activity;
                animalToSave.Notes = NotesTextBox.Text;
                animalToSave.TagNumber = TagNumberBox.Text;

                // FIX 2: Cost is decimal (Defaults to 0 if not used in UI)
                animalToSave.Cost = 0m;

                // FIX 3: Quantity Logic
                if (RadioIndividual.IsChecked == true)
                {
                    animalToSave.Quantity = 1;
                }
                else
                {
                    animalToSave.Quantity = Convert.ToInt32(QuantityBox.Value);
                    if (string.IsNullOrWhiteSpace(animalToSave.TagNumber))
                    {
                        animalToSave.TagNumber = $"BATCH-{DateTime.Now:MMdd}-{animalToSave.Species}";
                    }
                }

                await db.SaveChangesAsync();

                // Update UI
                if (isNew)
                {
                    _masterList.Insert(0, animalToSave);
                    LivestockList.Insert(0, animalToSave);
                    ErrorMessageTextBlock.Text = "Saved successfully.";
                }
                else
                {
                    // Update the UI object properties manually to reflect changes
                    var uiItem = LivestockList.FirstOrDefault(x => x.Id == animalToSave.Id);
                    if (uiItem != null)
                    {
                        uiItem.Species = animalToSave.Species;
                        uiItem.Weight = animalToSave.Weight;
                        uiItem.Quantity = animalToSave.Quantity;
                        uiItem.TagNumber = animalToSave.TagNumber;
                        uiItem.Status = animalToSave.Status;
                        uiItem.Notes = animalToSave.Notes;
                    }
                    ExitEditMode();
                    ErrorMessageTextBlock.Text = "Updated successfully.";
                }
            }
        }

        // =========================================================
        // 4. VALIDATION
        // =========================================================
        private bool ValidateForm()
        {
            ErrorMessageTextBlock.Text = "";

            if (SpeciesComboBox.SelectedItem == null && string.IsNullOrWhiteSpace(SpeciesComboBox.Text))
            {
                ErrorMessageTextBlock.Text = "Select or type a species.";
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

            if (RadioIndividual.IsChecked == true && string.IsNullOrWhiteSpace(TagNumberBox.Text))
            {
                ErrorMessageTextBlock.Text = "Tag Number is required for individuals.";
                return false;
            }

            if (RadioBatch.IsChecked == true && QuantityBox.Value < 2)
            {
                ErrorMessageTextBlock.Text = "Batch quantity must be 2 or more.";
                return false;
            }

            return true;
        }

        // =========================================================
        // 5. HELPER METHODS
        // =========================================================
        private void ClearForm()
        {
            SpeciesComboBox.SelectedIndex = -1;
            WeightTextBox.Text = string.Empty;
            DobDatePicker.SelectedDate = null;
            NotesTextBox.Text = string.Empty;
            TagNumberBox.Text = string.Empty;
            QuantityBox.Value = 1; // Double 1.0

            GenderMaleRadioButton.IsChecked = true;
            StatusRecoveredRadioButton.IsChecked = true;
            ActivityActiveRadioButton.IsChecked = true;

            RadioIndividual.IsChecked = true;
            Mode_Changed(null, null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLivestock == null)
            {
                ErrorMessageTextBlock.Text = "Please select a record to edit.";
                return;
            }

            SpeciesComboBox.SelectedItem = _selectedLivestock.Species;
            if (SpeciesComboBox.SelectedIndex == -1) SpeciesComboBox.Text = _selectedLivestock.Species;

            WeightTextBox.Text = _selectedLivestock.Weight.ToString();
            DobDatePicker.SelectedDate = _selectedLivestock.DateOfBirth;
            NotesTextBox.Text = _selectedLivestock.Notes;
            TagNumberBox.Text = _selectedLivestock.TagNumber;

            // Safe Cast Int to Double for NumberBox
            QuantityBox.Value = Convert.ToDouble(_selectedLivestock.Quantity);

            if (_selectedLivestock.Quantity > 1) RadioBatch.IsChecked = true;
            else RadioIndividual.IsChecked = true;

            Mode_Changed(null, null);

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

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedLivestock == null) return;

            using (var db = new AgrifyDbContext())
            {
                db.Livestock.Remove(_selectedLivestock);
                await db.SaveChangesAsync();
            }

            LivestockList.Remove(_selectedLivestock);
            ExitEditMode();
        }

        private void LivestockDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CancelButton.Visibility == Visibility.Collapsed)
            {
                _selectedLivestock = LivestockDataGrid.SelectedItem as Livestock;
            }
        }

        // =========================================================
        // 6. FILTER & SEARCH
        // =========================================================
        private void ApplyFilters()
        {
            if (_masterList == null) return;

            string query = SearchTextBox.Text?.ToLower() ?? "";

            var filtered = _masterList.Where(animal =>
                (animal.Species != null && animal.Species.ToLower().Contains(query)) ||
                (animal.TagNumber != null && animal.TagNumber.ToLower().Contains(query)) ||
                (animal.Status != null && animal.Status.ToLower().Contains(query))
            );

            switch (SortComboBox.SelectedIndex)
            {
                case 1: filtered = filtered.OrderBy(x => x.Species); break;
                case 2: filtered = filtered.OrderBy(x => x.Status); break;
                default: filtered = filtered.OrderByDescending(x => x.Id); break;
            }

            LivestockList.Clear();
            foreach (var animal in filtered) LivestockList.Add(animal);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void SaveNewSpeciesButton_Click(object sender, RoutedEventArgs e)
        {
            NewSpeciesErrorTextBlock.Text = "";
            var newSpecies = NewSpeciesTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newSpecies)) return;
            if (SpeciesList.Any(s => s.Equals(newSpecies, StringComparison.OrdinalIgnoreCase))) return;

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

        private void AddSpeciesButton_Click(object sender, RoutedEventArgs e) { /* UI reset */ }
    }
}
