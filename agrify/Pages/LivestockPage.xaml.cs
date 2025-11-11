// === 1. THESE 'using' STATEMENTS WERE MISSING ===
using agrify.Models;                 // Lets the code know what a 'Livestock' is
using System.Collections.ObjectModel;  // Lets the code use 'ObservableCollection'

// These are from your file (and are correct for WinUI/Uno)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace agrify.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LivestockPage : Page
{
    // === 2. THIS 'LivestockList' DECLARATION WAS MISSING ===
    // This is the list that will hold all our livestock data.
    private ObservableCollection<Livestock> LivestockList;

    public LivestockPage()
    {
        this.InitializeComponent();

        // === 3. THIS INITIALIZATION LOGIC WAS MISSING ===
        // Initialize the list
        LivestockList = new ObservableCollection<Livestock>();

        // This line connects your C# list to the XAML DataGrid.
        // It assumes your DataGrid in LivestockPage.xaml is named "LivestockDataGrid"
        LivestockDataGrid.ItemsSource = LivestockList;

        // (Optional) Add some sample data to prove it works
        LivestockList.Add(new Livestock
        {
            Species = "Cow",
            Gender = "Male",
            DateOfBirth = new DateTime(2020, 11, 5),
            Notes = "Recently Sick",
            Status = "Recovered",
            Activity = "Active"
        });
    }

    // This is the method you just added (and it is correct)
    private void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        // 1. Get Gender
        string gender = (GenderMaleRadioButton.IsChecked == true) ? "Male" : "Female";

        // 2. Get Status
        string status = "Recovered"; // Default
        if (StatusSoldRadioButton.IsChecked == true) status = "Sold";
        if (StatusTreatmentRadioButton.IsChecked == true) status = "Under Treatment";

        // 3. Get Activity
        string activity = (ActivityActiveRadioButton.IsChecked == true) ? "Active" : "Inactive";

        // 4. Create the new Livestock object
        // This line will now work because of "using agrify.Models;"
        var newAnimal = new Livestock
        {
            Species = SpeciesTextBox.Text,
            Weight = WeightTextBox.Text,
            DateOfBirth = DobDatePicker.Date.DateTime,
            Gender = gender,
            Status = status,
            Activity = activity,
            Notes = "" // Add a notes TextBox if you want this
        };

        // 5. Add the new animal to our list (the UI will update automatically!)
        // This line will now work because 'LivestockList' is declared
        LivestockList.Add(newAnimal);

        // 6. (Optional) Clear the input fields
        SpeciesTextBox.Text = string.Empty;
        WeightTextBox.Text = string.Empty;
        DobDatePicker.Date = System.DateTimeOffset.Now;
        GenderMaleRadioButton.IsChecked = true;
        StatusRecoveredRadioButton.IsChecked = true;
        ActivityActiveRadioButton.IsChecked = true;
    }
}
