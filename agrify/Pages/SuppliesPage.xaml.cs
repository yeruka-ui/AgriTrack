using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using agrify.Data;
using agrify.Models;
using agrify.Models.Category;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace agrify.Pages
{
    public sealed partial class SuppliesPage : Page
    {
        private ObservableCollection<Supplies> SuppliesList;
        public ObservableCollection<string> SupplyTypeList { get; set; }
        private Supplies _selectedSupply;

        public SuppliesPage()
        {
            this.InitializeComponent();

            SuppliesList = new ObservableCollection<Supplies>();
            SupplyTypeList = new ObservableCollection<string>();

            SuppliesDataGrid.ItemsSource = SuppliesList;
            SupplyTypeComboBox.ItemsSource = SupplyTypeList;

            ClearForm();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// UPDATED: Now loads types from two places
        /// </summary>
        private async Task LoadDataAsync()
        {
            SuppliesList.Clear();
            SupplyTypeList.Clear();

            using (var db = new AgrifyDbContext())
            {
                // 1. Load all Supplies (unchanged)
                var allSupplies = await db.Supplies.ToListAsync();
                foreach (var supply in allSupplies)
                {
                    SuppliesList.Add(supply);
                }

                // 2. Load types from the new ItemCategories table
                var typesFromCategories = await db.ItemCategories
                                                  .Where(c => c.CategoryType == "Supply")
                                                  .Select(c => c.Name)
                                                  .ToListAsync();

                // 3. Load types from existing items (in case they aren't in the new table)
                var typesFromItems = await db.Supplies
                                             .Select(s => s.ItemName)
                                             .ToListAsync();

                // 4. Combine them, remove duplicates, and sort them
                var allTypes = typesFromCategories.Union(typesFromItems)
                                                  .Distinct()
                                                  .OrderBy(name => name);

                foreach (var type in allTypes)
                {
                    SupplyTypeList.Add(type);
                }
            }
        }

        /// <summary>
        /// UPDATED: Now saves the new category to the database
        /// </summary>
        private async void SaveNewSupplyTypeButton_Click(object sender, RoutedEventArgs e)
        {
            NewSupplyTypeErrorTextBlock.Text = "";
            var newSupplyType = NewSupplyTypeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newSupplyType))
            {
                NewSupplyTypeErrorTextBlock.Text = "Supply type name cannot be empty.";
                return;
            }

            // Check for duplicates in the local list first
            if (SupplyTypeList.Any(s => s.Equals(newSupplyType, StringComparison.OrdinalIgnoreCase)))
            {
                NewSupplyTypeErrorTextBlock.Text = "This supply type already exists.";
                return;
            }

            // --- NEW: Save to database ---
            using (var db = new AgrifyDbContext())
            {
                // Double-check database for duplicate (in case)
                bool exists = await db.ItemCategories
                                      .AnyAsync(c => c.Name == newSupplyType && c.CategoryType == "Supply");

                if (!exists)
                {
                    // Add the new category
                    var newCategory = new ItemCategory
                    {
                        Name = newSupplyType,
                        CategoryType = "Supply" // Mark this as a "Supply" type
                    };
                    db.ItemCategories.Add(newCategory);
                    await db.SaveChangesAsync();
                }
            }
            // --- End of new code ---

            // Add to the local list (now that it's saved)
            SupplyTypeList.Add(newSupplyType);
            SupplyTypeComboBox.SelectedItem = newSupplyType;
            AddSupplyTypeFlyout.Hide();
            NewSupplyTypeTextBox.Text = "";
        }

        // --- All other methods (SubmitButton_Click, RemoveButton_Click, etc.) ---
        // --- remain exactly the same as before. ---
        // --- I'm including them here for a full copy-paste. ---

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                if (_selectedSupply != null)
                {
                    // UPDATE MODE
                    _selectedSupply.ItemName = SupplyTypeComboBox.SelectedItem.ToString();
                    _selectedSupply.Quantity = (int)QuantityNumberBox.Value;
                    _selectedSupply.UnitCost = UnitCostNumberBox.Value;
                    _selectedSupply.DateAcquired = DateAcquiredPicker.Date.DateTime;
                    _selectedSupply.Notes = NotesTextBox.Text;

                    db.Supplies.Update(_selectedSupply);
                    await db.SaveChangesAsync();

                    int index = SuppliesList.IndexOf(_selectedSupply);
                    SuppliesList[index] = _selectedSupply;

                    ErrorMessageTextBlock.Text = "Item updated successfully.";
                }
                else
                {
                    // CREATE NEW MODE
                    var newSupply = new Supplies
                    {
                        ItemName = SupplyTypeComboBox.SelectedItem.ToString(),
                        Quantity = (int)QuantityNumberBox.Value,
                        UnitCost = UnitCostNumberBox.Value,
                        DateAcquired = DateAcquiredPicker.Date.DateTime,
                        Notes = NotesTextBox.Text
                    };

                    db.Supplies.Add(newSupply);
                    await db.SaveChangesAsync();

                    SuppliesList.Add(newSupply);
                    ErrorMessageTextBlock.Text = "New item added successfully.";
                }
            }

            ExitEditMode();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSupply == null)
            {
                ErrorMessageTextBlock.Text = "Please select an item from the grid to remove.";
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                db.Supplies.Remove(_selectedSupply);
                await db.SaveChangesAsync();
            }

            SuppliesList.Remove(_selectedSupply);

            _selectedSupply = null;
            ClearForm();
            ErrorMessageTextBlock.Text = "Item removed successfully.";
        }

        private bool ValidateForm()
        {
            ErrorMessageTextBlock.Text = "";

            if (SupplyTypeComboBox.SelectedItem == null)
            {
                ErrorMessageTextBlock.Text = "Please select a supply type.";
                return false;
            }

            if (double.IsNaN(QuantityNumberBox.Value) || QuantityNumberBox.Value <= 0)
            {
                ErrorMessageTextBlock.Text = "Quantity must be greater than 0.";
                return false;
            }

            if (double.IsNaN(UnitCostNumberBox.Value) || UnitCostNumberBox.Value < 0)
            {
                ErrorMessageTextBlock.Text = "Unit Cost must be 0 or greater.";
                return false;
            }

            if (DateAcquiredPicker.SelectedDate == null)
            {
                ErrorMessageTextBlock.Text = "Date Acquired is a required field.";
                return false;
            }

            return true;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSupply == null)
            {
                ErrorMessageTextBlock.Text = "Please select an item from the grid to edit.";
                return;
            }

            SupplyTypeComboBox.SelectedItem = _selectedSupply.ItemName;
            QuantityNumberBox.Value = _selectedSupply.Quantity;
            UnitCostNumberBox.Value = _selectedSupply.UnitCost;
            DateAcquiredPicker.SelectedDate = _selectedSupply.DateAcquired;
            NotesTextBox.Text = _selectedSupply.Notes;

            SubmitButton.Content = "UPDATE";
            EditButton.Visibility = Visibility.Collapsed;
            RemoveButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;
            ErrorMessageTextBlock.Text = "Editing selected item.";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            ErrorMessageTextBlock.Text = "Edit cancelled.";
        }

        private void ExitEditMode()
        {
            _selectedSupply = null;
            ClearForm();

            SubmitButton.Content = "SUBMIT";
            EditButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private void ClearForm()
        {
            SupplyTypeComboBox.SelectedIndex = -1;
            QuantityNumberBox.Value = 0;
            UnitCostNumberBox.Value = 0;
            DateAcquiredPicker.SelectedDate = null;
            NotesTextBox.Text = string.Empty;
            SuppliesDataGrid.SelectedItem = null;
        }

        private void SuppliesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CancelButton.Visibility == Visibility.Collapsed)
            {
                _selectedSupply = SuppliesDataGrid.SelectedItem as Supplies;
            }
        }

        private void AddSupplyTypeButton_Click(object sender, RoutedEventArgs e)
        {
            NewSupplyTypeErrorTextBlock.Text = "";
            NewSupplyTypeTextBox.Text = "";
        }
    }
}
