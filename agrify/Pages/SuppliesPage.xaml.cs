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
        // 1. ADD MASTER LIST
        private System.Collections.Generic.List<Supplies> _masterList;

        public ObservableCollection<string> SupplyTypeList { get; set; }
        private Supplies _selectedSupply;

        public SuppliesPage()
        {
            this.InitializeComponent();

            SuppliesList = new ObservableCollection<Supplies>();
            // 2. INITIALIZE MASTER LIST
            _masterList = new System.Collections.Generic.List<Supplies>();
            SupplyTypeList = new ObservableCollection<string>();

            SuppliesDataGrid.ItemsSource = SuppliesList;
            SupplyTypeComboBox.ItemsSource = SupplyTypeList;

            ClearForm();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            SuppliesList.Clear();
            _masterList.Clear(); // 3. CLEAR MASTER
            SupplyTypeList.Clear();

            using (var db = new AgrifyDbContext())
            {
                var allSupplies = await db.Supplies.ToListAsync();

                // 4. FILL MASTER LIST FIRST
                _masterList.AddRange(allSupplies);

                // Default Sort: Recently Added
                var orderedInitial = _masterList.OrderByDescending(x => x.SuppliesId);

                foreach (var supply in orderedInitial)
                {
                    SuppliesList.Add(supply);
                }

                // Load Categories
                var typesFromCategories = await db.ItemCategories
                                                 .Where(c => c.CategoryType == "Supply")
                                                 .Select(c => c.Name)
                                                 .ToListAsync();

                var typesFromItems = await db.Supplies
                                               .Select(s => s.ItemName)
                                               .ToListAsync();

                var allTypes = typesFromCategories.Union(typesFromItems)
                                                  .Distinct()
                                                  .OrderBy(name => name);

                foreach (var type in allTypes)
                {
                    SupplyTypeList.Add(type);
                }
            }
        }

        // === 5. FILTERING ENGINE ===
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_masterList == null) return;

            string query = SearchTextBox.Text?.ToLower() ?? "";

            // Filter logic
            var filtered = _masterList.Where(s =>
                s.ItemName.ToLower().Contains(query) ||
                (s.Notes != null && s.Notes.ToLower().Contains(query))
            );

            // Sorting logic
            switch (SortComboBox.SelectedIndex)
            {
                case 1: // Supply Type (A-Z)
                    filtered = filtered.OrderBy(x => x.ItemName);
                    break;
                case 2: // Quantity (High to Low)
                    filtered = filtered.OrderByDescending(x => x.Quantity);
                    break;
                case 0: // Recently Added (Default)
                default:
                    filtered = filtered.OrderByDescending(x => x.SuppliesId);
                    break;
            }

            // Update UI
            SuppliesList.Clear();
            foreach (var item in filtered)
            {
                SuppliesList.Add(item);
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            using (var db = new AgrifyDbContext())
            {
                if (_selectedSupply != null)
                {
                    // === UPDATE EXISTING ===
                    db.Supplies.Attach(_selectedSupply); // Ensure EF is tracking

                    _selectedSupply.ItemName = SupplyTypeComboBox.SelectedItem.ToString();
                    _selectedSupply.Quantity = (int)QuantityNumberBox.Value;
                    _selectedSupply.UnitCost = UnitCostNumberBox.Value;
                    _selectedSupply.DateAcquired = DateAcquiredPicker.Date.DateTime;
                    _selectedSupply.Notes = NotesTextBox.Text;

                    db.Supplies.Update(_selectedSupply);
                    await db.SaveChangesAsync();

                    // Refresh existing item in list
                    // (Note: Since ObservableCollection updates automatically if object properties change 
                    // and support INotifyPropertyChanged, this is often automatic, but forcing refresh ensures it)
                    int index = SuppliesList.IndexOf(_selectedSupply);

                    ErrorMessageTextBlock.Text = "Item updated successfully.";
                }
                else
                {
                    // === CREATE NEW ===
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

                    // 6. SYNC LISTS
                    _masterList.Insert(0, newSupply); // Add to master
                    SuppliesList.Insert(0, newSupply); // Add to UI

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

            // 7. SYNC REMOVAL
            _masterList.Remove(_selectedSupply);
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
            SuppliesDataGrid.SelectedItem = null;
        }

        private void ClearForm()
        {
            SupplyTypeComboBox.SelectedIndex = -1;
            QuantityNumberBox.Value = 0;
            UnitCostNumberBox.Value = 0;
            DateAcquiredPicker.SelectedDate = null;
            NotesTextBox.Text = string.Empty;
        }

        private void SuppliesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CancelButton.Visibility == Visibility.Collapsed)
            {
                _selectedSupply = SuppliesDataGrid.SelectedItem as Supplies;
            }
        }

        private async void SaveNewSupplyTypeButton_Click(object sender, RoutedEventArgs e)
        {
            NewSupplyTypeErrorTextBlock.Text = "";
            var newSupplyType = NewSupplyTypeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newSupplyType))
            {
                NewSupplyTypeErrorTextBlock.Text = "Supply type name cannot be empty.";
                return;
            }

            if (SupplyTypeList.Any(s => s.Equals(newSupplyType, StringComparison.OrdinalIgnoreCase)))
            {
                NewSupplyTypeErrorTextBlock.Text = "This supply type already exists.";
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                bool exists = await db.ItemCategories
                                      .AnyAsync(c => c.Name == newSupplyType && c.CategoryType == "Supply");

                if (!exists)
                {
                    var newCategory = new ItemCategory
                    {
                        Name = newSupplyType,
                        CategoryType = "Supply"
                    };
                    db.ItemCategories.Add(newCategory);
                    await db.SaveChangesAsync();
                }
            }

            SupplyTypeList.Add(newSupplyType);
            SupplyTypeComboBox.SelectedItem = newSupplyType;
            AddSupplyTypeFlyout.Hide();
            NewSupplyTypeTextBox.Text = "";
        }

        private void AddSupplyTypeButton_Click(object sender, RoutedEventArgs e)
        {
            NewSupplyTypeErrorTextBlock.Text = "";
            NewSupplyTypeTextBox.Text = "";
        }
    }
}
