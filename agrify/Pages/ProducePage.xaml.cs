using agrify.Data;    // Add this to access the DbContext
using agrify.Models;
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
        public ObservableCollection<string> ProduceTypeList { get; set; }
        private Produce _selectedProduce;

        public ProducePage()
        {
            this.InitializeComponent();

            // Initialize the lists, but don't populate them here anymore
            ProduceList = new ObservableCollection<Produce>();
            ProduceTypeList = new ObservableCollection<string>();

            // Set the DataGrid and ComboBox sources
            ProduceDataGrid.ItemsSource = ProduceList;
            ProduceTypeComboBox.ItemsSource = ProduceTypeList;

            // Set default state
            ClearForm();
        }

        /// <summary>
        /// NEW: Runs when the page is loaded. Fetches data from the database.
        /// </summary>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// NEW: Helper method to load all data from the database.
        /// </summary>
        private async Task LoadDataAsync()
        {
            // Clear existing data
            ProduceList.Clear();
            ProduceTypeList.Clear();

            using (var db = new AgrifyDbContext())
            {
                // 1. Load all Produce items
                var allProduce = await db.Produce.ToListAsync();
                foreach (var produce in allProduce)
                {
                    ProduceList.Add(produce);
                }

                // 2. Load all distinct ProduceType strings from the data
                var allTypes = await db.Produce
                                       .Select(p => p.ProduceType)
                                       .Distinct()
                                       .ToListAsync();

                foreach (var type in allTypes)
                {
                    ProduceTypeList.Add(type);
                }
            }

            // (Optional) Add default types if they don't exist,
            // so the ComboBox isn't empty on first run
            if (!ProduceTypeList.Contains("Tomatoes")) ProduceTypeList.Add("Tomatoes");
            if (!ProduceTypeList.Contains("Corn")) ProduceTypeList.Add("Corn");
        }

        /// <summary>
        /// UPDATED: Now saves to the database.
        /// </summary>
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                if (_selectedProduce != null)
                {
                    // UPDATE MODE
                    _selectedProduce.ProduceType = ProduceTypeComboBox.SelectedItem.ToString();
                    _selectedProduce.Quantity = (int)QuantityNumberBox.Value;
                    _selectedProduce.Weight = WeightTextBox.Text;
                    _selectedProduce.HarvestDate = HarvestDatePicker.Date.DateTime;
                    _selectedProduce.Notes = NotesTextBox.Text;

                    db.Produce.Update(_selectedProduce);
                    await db.SaveChangesAsync();

                    // Refresh the item in the list
                    int index = ProduceList.IndexOf(_selectedProduce);
                    ProduceList[index] = _selectedProduce;

                    ErrorMessageTextBlock.Text = "Item updated successfully.";
                }
                else
                {
                    // CREATE NEW MODE
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

                    // Add to the UI list *after* saving (so it has an ID)
                    ProduceList.Add(newProduce);

                    ErrorMessageTextBlock.Text = "New item added successfully.";
                }
            }

            ExitEditMode();
        }

        /// <summary>
        /// UPDATED: Now removes from the database.
        /// </summary>
        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduce == null)
            {
                ErrorMessageTextBlock.Text = "Please select an item from the grid to remove.";
                return;
            }

            using (var db = new AgrifyDbContext())
            {
                // Remove from the database
                db.Produce.Remove(_selectedProduce);
                await db.SaveChangesAsync();
            }

            // Remove from the UI list
            ProduceList.Remove(_selectedProduce);

            _selectedProduce = null;
            ClearForm();
            ErrorMessageTextBlock.Text = "Item removed successfully.";
        }

        // --- All methods below this line are unchanged or have minor tweaks ---

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

            ErrorMessageTextBlock.Text = "Editing selected item. Click UPDATE to save or CANCEL to exit.";
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

        /// <summary>
        /// This logic is unchanged. It adds the new type to the
        /// ComboBox. The next time the user SUBMITS, this new type
        /// will be saved with an item and loaded automatically next time.
        /// </summary>
        private void SaveNewProduceTypeButton_Click(object sender, RoutedEventArgs e)
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
