using agrify.Data;
using agrify.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace agrify.Pages
{
    public sealed partial class SalesReport : Page
    {
        private ObservableCollection<Sale> SalesList;
        // Variables to hold current selection data
        private int _maxStock = 0;
        private int _selectedItemId = 0;

        public SalesReport()
        {
            this.InitializeComponent();
            SalesList = new ObservableCollection<Sale>();
            SalesDataGrid.ItemsSource = SalesList;
            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSalesHistoryAsync();
        }

        // ============================
        // 1. LOAD HISTORY
        // ============================
        private async Task LoadSalesHistoryAsync()
        {
            try
            {
                using (var db = new AgrifyDbContext())
                {
                    // Ensure the table exists
                    await db.Database.EnsureCreatedAsync();

                    var sales = await db.Sales
                        .OrderByDescending(s => s.SaleDate)
                        .ToListAsync();

                    SalesList.Clear();
                    foreach (var s in sales) SalesList.Add(s);
                }
            }
            catch (Exception ex)
            {
                SaleMessageTextBlock.Text = "Error loading history.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        // ============================
        // 2. TYPE SELECTION (Switch Lists)
        // ============================
        private async void ItemTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ItemComboBox.ItemsSource = null;
            AvailableStockTextBlock.Text = "Available: -";
            QuantityNumberBox.Value = 1;
            UnitPriceNumberBox.Value = 0;

            if (ItemTypeComboBox.SelectedItem is ComboBoxItem selectedType)
            {
                string type = selectedType.Content.ToString();
                using (var db = new AgrifyDbContext())
                {
                    if (type == "Produce")
                    {
                        var list = await db.Produce
                            .Select(p => new { Id = p.ProduceId, Name = p.ProduceType, Qty = p.Quantity, Price = 0m }) // Produce usually has no 'Price' column yet?
                            .ToListAsync();

                        // We use a simplified anonymous object for the ComboBox
                        ItemComboBox.ItemsSource = list;
                        ItemComboBox.DisplayMemberPath = "Name";
                        ItemComboBox.SelectedValuePath = "Id";
                    }
                    else if (type == "Livestock")
                    {
                        // NOTE: We select 'Cost' as the base Price suggestion
                        var list = await db.Livestock
                            .Where(l => l.Quantity > 0) // Only show available animals
                            .Select(l => new { l.Id, Name = $"{l.Species} ({l.TagNumber})", Qty = l.Quantity, Price = l.Cost })
                            .ToListAsync();

                        ItemComboBox.ItemsSource = list;
                        ItemComboBox.DisplayMemberPath = "Name";
                        ItemComboBox.SelectedValuePath = "Id";
                    }
                }
            }
        }

        // ============================
        // 3. ITEM SELECTION (Auto-Fill)
        // ============================
        private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemComboBox.SelectedItem == null) return;

            // Access the anonymous object properties safely using dynamic
            dynamic selectedItem = ItemComboBox.SelectedItem;

            _selectedItemId = selectedItem.Id;
            _maxStock = selectedItem.Qty;
            decimal suggestedPrice = (decimal)selectedItem.Price;

            // UI Updates
            AvailableStockTextBlock.Text = $"Available: {_maxStock}";

            // Auto-fill price from DB (User can edit this)
            UnitPriceNumberBox.Value = (double)suggestedPrice;

            // Default Quantity
            QuantityNumberBox.Value = 1;
            QuantityNumberBox.Maximum = _maxStock; // Prevent selecting more than we have!

            UpdateTotal();
        }

        // ============================
        // 4. LIVE TOTAL CALCULATION
        // ============================
        private void Input_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            if (TotalTextBlock == null) return;
            double qty = QuantityNumberBox.Value;
            double price = UnitPriceNumberBox.Value;
            double total = qty * price;
            TotalTextBlock.Text = $"${total:N2}";
        }

        // ============================
        // 5. SUBMIT SALE
        // ============================
        private async void SubmitSaleButton_Click(object sender, RoutedEventArgs e)
        {
            SaleMessageTextBlock.Text = "";

            if (ItemComboBox.SelectedItem == null)
            {
                SaleMessageTextBlock.Text = "Select an item.";
                return;
            }

            int sellQty = (int)QuantityNumberBox.Value;
            if (sellQty <= 0)
            {
                SaleMessageTextBlock.Text = "Qty must be > 0.";
                return;
            }
            if (sellQty > _maxStock)
            {
                SaleMessageTextBlock.Text = $"Only {_maxStock} in stock!";
                return;
            }

            decimal finalPrice = (decimal)UnitPriceNumberBox.Value;
            string type = ((ComboBoxItem)ItemTypeComboBox.SelectedItem).Content.ToString();
            dynamic selItem = ItemComboBox.SelectedItem;
            string name = selItem.Name;

            try
            {
                using (var db = new AgrifyDbContext())
                {
                    // 1. Deduct Inventory
                    if (type == "Livestock")
                    {
                        var animal = await db.Livestock.FindAsync(_selectedItemId);
                        if (animal != null)
                        {
                            animal.Quantity -= sellQty;
                            if (animal.Quantity < 0) animal.Quantity = 0; // Safety
                        }
                    }
                    else if (type == "Produce")
                    {
                        var produce = await db.Produce.FindAsync(_selectedItemId);
                        if (produce != null)
                        {
                            produce.Quantity -= sellQty;
                        }
                    }

                    // 2. Create Record
                    var newSale = new Sale
                    {
                        ItemId = _selectedItemId,
                        ItemType = type,
                        ItemName = name,
                        Quantity = sellQty,
                        UnitPrice = finalPrice,
                        SaleDate = DateTime.UtcNow
                    };

                    db.Sales.Add(newSale);
                    await db.SaveChangesAsync();

                    // 3. UI Update
                    SalesList.Insert(0, newSale);
                    SaleMessageTextBlock.Text = "Sale Recorded!";

                    // Reset inputs
                    QuantityNumberBox.Value = 1;
                    AvailableStockTextBlock.Text = "Stock updated.";

                    // Refresh the ComboBox to show new stock levels?
                    // Optional: ItemTypeComboBox_SelectionChanged(null, null);
                }
            }
            catch (Exception ex)
            {
                SaleMessageTextBlock.Text = "DB Error: " + ex.Message;
            }
        }

        private void SalesSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Simple filter logic if needed
            string filter = SalesSearchBox.Text.ToLower();
            // Note: Since SalesList is ObservableCollection, better to reload from DB with Where clause
            // or use a CollectionViewSource for advanced filtering.
        }
    }
}
