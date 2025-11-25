using agrify.Data;    // Add this to access the DbContext
using agrify.Models;
using agrify.Models.Category;
using Microsoft.EntityFrameworkCore; // Add this for EF Core functions
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Threading.Tasks; // Add this for async tasks
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace agrify.Pages
{
    public sealed partial class ProducePage : Page
    {
        private ObservableCollection<Produce> ProduceList;

        // 1. ADD MASTER LIST
        private System.Collections.Generic.List<Produce> _masterList;

        public ObservableCollection<string> ProduceTypeList { get; set; }
        private Produce _selectedProduce;

        public ProducePage()
        {
            this.InitializeComponent();

            ProduceList = new ObservableCollection<Produce>();
            // 2. INITIALIZE MASTER LIST
            _masterList = new System.Collections.Generic.List<Produce>();
            ProduceTypeList = new ObservableCollection<string>();

            ProduceDataGrid.ItemsSource = ProduceList;
            ProduceTypeComboBox.ItemsSource = ProduceTypeList;

            ClearForm();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            ProduceList.Clear();
            _masterList.Clear(); // 3. CLEAR MASTER
            ProduceTypeList.Clear();

            using (var db = new AgrifyDbContext())
            {
                var allProduce = await db.Produce.ToListAsync();

                // 4. FILL MASTER LIST FIRST
                _masterList.AddRange(allProduce);

                // Default sort (Recently Added / ID Descending)
                var orderedInitial = _masterList.OrderByDescending(x => x.ProduceId);

                foreach (var produce in orderedInitial)
                {
                    ProduceList.Add(produce);
                }

                // Load Categories (Same as before)
                var typesFromCategories = await db.ItemCategories
                                                 .Where(c => c.CategoryType == "Produce")
                                                 .Select(c => c.Name)
                                                 .ToListAsync();

                var typesFromItems = await db.Produce
                                               .Select(p => p.ProduceType)
                                               .ToListAsync();

                var allTypes = typesFromCategories.Union(typesFromItems)
                                                  .Distinct()
                                                  .OrderBy(name => name);

                foreach (var type in allTypes)
                {
                    ProduceTypeList.Add(type);
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
            var filtered = _masterList.Where(p =>
                p.ProduceType.ToLower().Contains(query) ||
                (p.Notes != null && p.Notes.ToLower().Contains(query))
            // You can add Weight filtering here if you want string matching
            );

            // Sorting logic based on ComboBox
            switch (SortComboBox.SelectedIndex)
            {
                case 1: // Produce Type (A-Z)
                    filtered = filtered.OrderBy(x => x.ProduceType);
                    break;
                case 2: // Quantity (High to Low)
                    filtered = filtered.OrderByDescending(x => x.Quantity);
                    break;
                case 0: // Recently Added (Default)
                default:
                    filtered = filtered.OrderByDescending(x => x.ProduceId);
                    break;
            }

            // Update UI
            ProduceList.Clear();
            foreach (var p in filtered)
            {
                ProduceList.Add(p);
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            using (var db = new AgrifyDbContext())
            {
                if (_selectedProduce != null)
                {
                    // === UPDATE EXISTING ===
                    db.Produce.Attach(_selectedProduce); // Ensure EF is tracking it

                    _selectedProduce.ProduceType = ProduceTypeComboBox.SelectedItem.ToString();
                    _selectedProduce.Quantity = (int)QuantityNumberBox.Value;
                    _selectedProduce.Weight = WeightTextBox.Text;
                    _selectedProduce.HarvestDate = HarvestDatePicker.Date.DateTime;
                    _selectedProduce.Notes = NotesTextBox.Text;

                    db.Produce.Update(_selectedProduce);
                    await db.SaveChangesAsync();

                    ErrorMessageTextBlock.Text = "Item updated successfully.";
                }
                else
                {
                    // === CREATE NEW ===
                    var newProduce = new Produce
                    {
                        ProduceType = ProduceTypeComboBox.SelectedItem.ToString(),
                        Quantity = (int)QuantityNumberBox.Value,
                        Weight = WeightTextBox.Text,
                        HarvestDate = HarvestDatePicker.Date.DateTime,
                        Notes = NotesTextBox.Text
                    };

                    db.Produce.Add(newProduce);
                    await db.SaveChangesAsync();

                    // 6. SYNC LISTS
                    _masterList.Insert(0, newProduce); // Add to Master
                    ProduceList.Insert(0, newProduce); // Add to UI

                    ErrorMessageTextBlock.Text = "New item added successfully.";
                }
            }

            ExitEditMode();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduce == null)
            {
                ErrorMessageTextBlock.Text = "Please select an item from the grid to remove.";
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                db.Produce.Remove(_selectedProduce);
                await db.SaveChangesAsync();
            }

            // 7. SYNC REMOVAL
            _masterList.Remove(_selectedProduce);
            ProduceList.Remove(_selectedProduce);

            _selectedProduce = null;
            ClearForm();
            ErrorMessageTextBlock.Text = "Item removed successfully.";
        }

        // ... Rest of the helper methods (EditButton_Click, CancelButton_Click, etc.) remain the same ...

        private bool ValidateForm()
        {
            ErrorMessageTextBlock.Text = "";

            if (ProduceTypeComboBox.SelectedItem == null)
            {
                ErrorMessageTextBlock.Text = "Please select a produce type.";
                return false;
            }

            if (double.IsNaN(QuantityNumberBox.Value) || QuantityNumberBox.Value <= 0)
            {
                ErrorMessageTextBlock.Text = "Quantity must be greater than 0.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(WeightTextBox.Text))
            {
                ErrorMessageTextBlock.Text = "Weight is a required field.";
                return false;
            }

            if (HarvestDatePicker.SelectedDate == null)
            {
                ErrorMessageTextBlock.Text = "Date Harvested is a required field.";
                return false;
            }

            return true;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduce == null)
            {
                ErrorMessageTextBlock.Text = "Please select an item from the grid to edit.";
                return;
            }

            ProduceTypeComboBox.SelectedItem = _selectedProduce.ProduceType;
            QuantityNumberBox.Value = _selectedProduce.Quantity;
            WeightTextBox.Text = _selectedProduce.Weight;
            HarvestDatePicker.SelectedDate = _selectedProduce.HarvestDate;
            NotesTextBox.Text = _selectedProduce.Notes;

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
            _selectedProduce = null;
            ClearForm();

            SubmitButton.Content = "SUBMIT";
            EditButton.Visibility = Visibility.Visible;
            RemoveButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Collapsed;
        }

        private void ClearForm()
        {
            ProduceTypeComboBox.SelectedIndex = -1;
            QuantityNumberBox.Value = 0;
            WeightTextBox.Text = string.Empty;
            HarvestDatePicker.SelectedDate = null;
            NotesTextBox.Text = string.Empty;
            ProduceDataGrid.SelectedItem = null;
        }

        private void ProduceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CancelButton.Visibility == Visibility.Collapsed)
            {
                _selectedProduce = ProduceDataGrid.SelectedItem as Produce;
            }
        }

        private async void SaveNewProduceTypeButton_Click(object sender, RoutedEventArgs e)
        {
            NewProduceTypeErrorTextBlock.Text = "";
            var newProduceType = NewProduceTypeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(newProduceType))
            {
                NewProduceTypeErrorTextBlock.Text = "Produce type name cannot be empty.";
                return;
            }

            if (ProduceTypeList.Any(s => s.Equals(newProduceType, StringComparison.OrdinalIgnoreCase)))
            {
                NewProduceTypeErrorTextBlock.Text = "This produce type already exists.";
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                bool exists = await db.ItemCategories
                                      .AnyAsync(c => c.Name == newProduceType && c.CategoryType == "Produce");

                if (!exists)
                {
                    var newCategory = new ItemCategory
                    {
                        Name = newProduceType,
                        CategoryType = "Produce"
                    };
                    db.ItemCategories.Add(newCategory);
                    await db.SaveChangesAsync();
                }
            }

            ProduceTypeList.Add(newProduceType);
            ProduceTypeComboBox.SelectedItem = newProduceType;
            AddProduceTypeFlyout.Hide();
            NewProduceTypeTextBox.Text = "";
        }

        private void AddProduceTypeButton_Click(object sender, RoutedEventArgs e)
        {
            NewProduceTypeErrorTextBlock.Text = "";
            NewProduceTypeTextBox.Text = "";
        }
    }
}
