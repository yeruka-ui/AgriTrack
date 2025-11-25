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
            }

            private async void Page_Loaded(object sender, RoutedEventArgs e)
            {
                await LoadDataAsync();
            }

            private async Task LoadDataAsync()
            {
                // 1. Wrap everything in Try-Catch to stop "Silent Crashes"
                try
                {
                    LivestockList.Clear();
                    _masterList.Clear(); // Clear master too
                    SpeciesList.Clear();

                    using (var db = new AgrifyDbContext())
                    {

                        // === DIAGNOSTIC 1: WHERE IS THE DB? ===
                        // Look at your Visual Studio "Output" window to see this path.
                        var connString = db.Database.GetDbConnection().ConnectionString;
                        System.Diagnostics.Debug.WriteLine($"[DB CONNECTION]: {connString}");

                        // 2. Load Livestock
                        // If the table columns don't match C# EXACTLY, this line will crash.
                        var allLivestock = await db.Livestock.ToListAsync();
                        _masterList.AddRange(allLivestock);
                        var orderedInitial = _masterList.OrderByDescending(x => x.Id);
                        foreach (var animal in allLivestock)
                        {
                            LivestockList.Add(animal);
                        }

                        // 3. Load Species (Categories)
                        var typesFromCategories = await db.ItemCategories
                            .Where(c => c.CategoryType == "Livestock")
                            .Select(c => c.Name)
                            .ToListAsync();

                        // 4. Load existing types from Livestock table (fallback)
                        var typesFromItems = await db.Livestock
                            .Select(l => l.Species)
                            .ToListAsync();

                        // 5. Merge
                        var allTypes = typesFromCategories.Union(typesFromItems)
                            .Distinct()
                            .OrderBy(n => n);

                        foreach (var type in allTypes)
                        {
                            SpeciesList.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // === DIAGNOSTIC 2: SHOW THE ERROR ===
                    // This will pop up a dialog box if loading fails.
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Loading Error",
                        Content = $"The database failed to load:\n{ex.Message}\n\nCheck Inner Exception: {ex.InnerException?.Message}",
                        CloseButtonText = "Ok",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();

                    // Also print to debug console
                    System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR]: {ex.ToString()}");
                }
            }

            private void ApplyFilters()
            {
                // Safety check to ensure data exists
                if (_masterList == null) return;

                // 1. Get the search query
                string query = SearchTextBox.Text?.ToLower() ?? "";

                // 2. Filter the Master List
                // We look for matches in Species, Status, Activity, or Notes
                var filtered = _masterList.Where(animal =>
                    animal.Species.ToLower().Contains(query) ||
                    animal.Status.ToLower().Contains(query) ||
                    animal.Activity.ToLower().Contains(query) ||
                    (animal.Notes != null && animal.Notes.ToLower().Contains(query))
                );

                // 3. Apply Sorting based on ComboBox
                // Index 0: Recently Added, 1: Species, 2: Status
                switch (SortComboBox.SelectedIndex)
                {
                    case 1: // Species (A-Z)
                        filtered = filtered.OrderBy(x => x.Species);
                        break;
                    case 2: // Status (A-Z)
                        filtered = filtered.OrderBy(x => x.Status);
                        break;
                    case 0: // Recently Added (Newest ID first)
                    default:
                        filtered = filtered.OrderByDescending(x => x.Id);
                        break;
                }

                // 4. Update the UI
                // We don't replace the ObservableCollection (breaks binding), we clear and refill it.
                LivestockList.Clear();
                foreach (var animal in filtered)
                {
                    LivestockList.Add(animal);
                }
            }
    
    
    
            private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
            {
                ApplyFilters();
            }

            private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                ApplyFilters();
            }


            private async void SubmitButton_Click(object sender, RoutedEventArgs e)
            {
                if (!ValidateForm()) return;

                // 1. Gather Common Data
                string gender = (GenderMaleRadioButton.IsChecked == true) ? "Male" : "Female";
                string activity = (ActivityActiveRadioButton.IsChecked == true) ? "Active" : "Inactive";

                string status = "Recovered";
                if (StatusSoldRadioButton.IsChecked == true) status = "Sold";
                if (StatusTreatmentRadioButton.IsChecked == true) status = "Under Treatment";

                using (var db = new AgrifyDbContext())
                {
                    // === EDIT MODE (Always Single) ===
                    if (_selectedLivestock != null)
                    {
                        // ... (Keep your existing Edit logic here, it is perfect) ...
                        // Be sure to check: cannot edit a batch easily, so disable Batch Toggle when editing!
                    }
                    // === CREATE MODE ===
                    else
                    {
                        // CHECK: Is Batch Mode On?
                        if (BatchModeToggle.IsOn)
                        {
                            string rawInput = BatchQuantityTextBox.Text;
                            if (!int.TryParse(rawInput, out int qtyCheck) || qtyCheck < 2)
                            {
                                ErrorMessageTextBlock.Text = $"Invalid Quantity. You typed: '{rawInput}'";
                                return;
                            }
                            // === BATCH LOGIC ===
                            if (!int.TryParse(BatchQuantityTextBox.Text, out int qty) || qty < 2)
                            {
                                ErrorMessageTextBlock.Text = "Please enter a valid quantity (2+).";
                                return;
                            }

                            // Create a list to hold the new animals
                            var batchList = new System.Collections.Generic.List<Livestock>();

                            for (int i = 0; i < qty; i++)
                            {
                                batchList.Add(new Livestock
                                {
                                    Species = SpeciesComboBox.SelectedItem.ToString(),
                                    Weight = WeightTextBox.Text, // Assuming they all weigh the same approx
                                    DateOfBirth = DobDatePicker.Date.DateTime,
                                    Gender = gender,
                                    Status = status,
                                    Activity = activity,
                                    Notes = NotesTextBox.Text // They share the note
                                });
                            }

                            // FAST SAVE: Use AddRangeAsync
                            await db.Livestock.AddRangeAsync(batchList);
                            await db.SaveChangesAsync();

                            batchList.Reverse();

                        _masterList.AddRange(batchList); // <--- ADD THIS

                        // Update UI Collection (Avoid full reload for performance)
                        foreach (var animal in batchList)
                            {
                                LivestockList.Insert(0, animal); // Add to top
                            }

                            ErrorMessageTextBlock.Text = $"Success! Added batch of {qty}.";
                        }
                        else
                        {
                            // === INDIVIDUAL LOGIC (Your existing code) ===
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
                        _masterList.Insert(0, newAnimal); 
                                                          // Add to top of list so user sees it
                        LivestockList.Insert(0, newAnimal);
                            ErrorMessageTextBlock.Text = "New animal added successfully.";
                        }
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
                BatchModeToggle.IsOn = false;
                BatchModeToggle.IsEnabled = false;

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
                BatchModeToggle.IsEnabled = true;
                BatchModeToggle.IsOn = false;
                BatchQuantityPanel.Visibility = Visibility.Collapsed;
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

            private void BatchModeToggle_Toggled(object sender, RoutedEventArgs e)
            {
                if (BatchModeToggle.IsOn)
                {
                    BatchQuantityPanel.Visibility = Visibility.Visible;
                    // Optional: Change UI text to indicate batch entry
                    SubmitButton.Content = "SUBMIT BATCH";
                }
                else
                {
                    BatchQuantityPanel.Visibility = Visibility.Collapsed;
                    SubmitButton.Content = "SUBMIT";
                }
            }
        }
    }
