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
        private readonly AgrifyDbContext _db;
        private ObservableCollection<Sale> SalesList;

        public SalesReport()
        {
            this.InitializeComponent();
            _db = new AgrifyDbContext();
            SalesList = new ObservableCollection<Sale>();
            SalesDataGrid.ItemsSource = SalesList;

            LoadSalesAsync();
        }

        // ===========================
        // LOAD SALES
        // ===========================
        private async Task LoadSalesAsync()
        {
            var sales = await _db.Sales.OrderByDescending(s => s.SaleDate).ToListAsync();
            SalesList.Clear();
            foreach (var s in sales)
                SalesList.Add(s);
        }

        // ===========================
        // ITEM TYPE SELECTION
        // ===========================
        private async void ItemTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ItemComboBox.ItemsSource = null;


          if (ItemTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string type = selectedItem.Content.ToString();

                if (type == "Produce")
                {
                    // Fetch all produce items
                    var produceItems = await _db.Produce
                        .Select(p => new
                        {
                            p.ProduceId,
                            p.ProduceType,
                            p.Quantity,
                            p.Weight,
                            p.HarvestDate
                        })
                        .ToListAsync();

                    ItemComboBox.ItemsSource = produceItems;
                    ItemComboBox.DisplayMemberPath = "ProduceType"; // What user sees
                    ItemComboBox.SelectedValuePath = "ProduceId";   // ID for sale recording
                }
                else if (type == "Livestock")
                {
                    var livestockItems = await _db.Livestock
                        .Select(l => new
                        {
                            l.Id,
                            l.AnimalName, // add this
                            l.Breed,
                            l.TagNumber,
                            l.Cost,
                            l.Quantity
                        })
                        .ToListAsync();

                    ItemComboBox.ItemsSource = livestockItems;
                    ItemComboBox.DisplayMemberPath = "TagNumber"; // you can change to display custom string later
                    ItemComboBox.SelectedValuePath = "Id";
                }


            }


        }



        private async Task<decimal> GetMonthlyExpensesAsync()
        {
            var now = DateTime.UtcNow;

            // Fetch all expenses for the current month and year
            var expenses = await _db.Expenses
                .Where(e => e.Date.Month == now.Month && e.Date.Year == now.Year)
                .ToListAsync();

            // Sum the Amount column safely
            decimal totalExpenses = expenses.Sum(e => (decimal)e.Amount);

            return totalExpenses;
        }

        private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateTotal();
        }

        // ===========================
        // NUMBERBOX EVENTS
        // ===========================
        private void QuantityNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => UpdateTotal();
        private void UnitPriceNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => UpdateTotal();

        private void UpdateTotal()
        {
            try
            {
                // Get values directly from NumberBox (they are double)
                double qtyDouble = QuantityNumberBox.Value;
                double priceDouble = UnitPriceNumberBox.Value;

                // Convert to decimal safely
                decimal qty = Convert.ToDecimal(qtyDouble);
                decimal price = Convert.ToDecimal(priceDouble);

                // Optional: clamp to reasonable max to avoid overflow
                qty = Math.Min(qty, 1000000m);
                price = Math.Min(price, 1000000m);

                TotalTextBlock.Text = (qty * price).ToString("N2");
            }
            catch (OverflowException)
            {
                TotalTextBlock.Text = "0.00";
            }
        }


        // ===========================
        // SET DEFAULT PRICE
        // ===========================
        private async void SetDefaultPriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItemComboBox.SelectedItem == null) return;

            decimal defaultPrice = 0;

            if (ItemTypeComboBox.SelectedItem is ComboBoxItem selectedType)
            {
                if (selectedType.Content.ToString() == "Produce")
                {
                    int produceId = (int)ItemComboBox.SelectedValue;
                    var produce = await _db.Produce.FirstOrDefaultAsync(p => p.ProduceId == produceId);
                    if (produce != null && decimal.TryParse(produce.Weight, out decimal priceFromWeight))
                        defaultPrice = priceFromWeight;
                }
                else if (selectedType.Content.ToString() == "Livestock")
                {
                    int livestockId = (int)ItemComboBox.SelectedValue;
                    var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == livestockId);
                    if (livestock != null)
                        defaultPrice = livestock.Cost;
                }
            }

            UnitPriceNumberBox.Value = (double)defaultPrice;
        }





        // ===========================
        // SUBMIT SALE
        // ===========================
        private async void SubmitSaleButton_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation
            if (ItemComboBox.SelectedItem == null || QuantityNumberBox.Value <= 0)
            {
                SaleMessageTextBlock.Text = "Please select an item and enter a quantity.";
                return;
            }

            // Parse unit price from the text (safer than Value)
            decimal unitPrice = 0m;
            if (!string.IsNullOrWhiteSpace(UnitPriceNumberBox.Text))
            {
                if (!decimal.TryParse(UnitPriceNumberBox.Text, out unitPrice))
                {
                    SaleMessageTextBlock.Text = "Invalid unit price.";
                    return;
                }
            }
            else
            {
                SaleMessageTextBlock.Text = "Please enter unit price.";
                return;
            }

            // Determine item name and types safely
            string itemType = ((ComboBoxItem?)ItemTypeComboBox.SelectedItem)?.Content?.ToString() ?? "";
            string itemName = "";
            int itemId = (int)(ItemComboBox.SelectedValue ?? 0);
            int qty = (int)QuantityNumberBox.Value;

            try
            {
                // The selected ItemComboBox.SelectedItem is an anonymous type from LINQ projection.
                // Use dynamic to read properties that exist on your anonymous projection.
                if (itemType == "Produce")
                {
                    dynamic sel = ItemComboBox.SelectedItem;
                    // common property name from produce projection: ProduceType (fall back to Display or ProduceType)
                    itemName = (sel.ProduceType ?? sel.Display ?? sel.ProduceName ?? "").ToString();
                }
                else if (itemType == "Livestock")
                {
                    dynamic sel = ItemComboBox.SelectedItem;
                    // projection should contain AnimalName/Breed/TagNumber or Display
                    string a = (sel.AnimalName ?? sel.Display ?? sel.TagNumber ?? sel.Breed ?? "").ToString();
                    string b = (sel.Breed ?? "").ToString();
                    string t = (sel.TagNumber ?? "").ToString();
                    itemName = string.IsNullOrWhiteSpace(a) ? $"{b} {t}".Trim() : a;
                }

                // START a transaction so inventory + sale are atomic
                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    // Deduct inventory (use the correct keys)
                    if (itemType == "Produce")
                    {
                        // Your Produce class uses ProduceId as PK, so query with that
                        var produce = await _db.Produce.FirstOrDefaultAsync(p => p.ProduceId == itemId);
                        if (produce == null)
                        {
                            SaleMessageTextBlock.Text = "Selected produce not found in database.";
                            return;
                        }

                        if (produce.Quantity < qty)
                        {
                            SaleMessageTextBlock.Text = "Not enough produce stock.";
                            return;
                        }

                        produce.Quantity -= qty;
                        _db.Produce.Update(produce);
                    }
                    else if (itemType == "Livestock")
                    {
                        // Livestock uses Id as PK
                        var livestock = await _db.Livestock.FirstOrDefaultAsync(l => l.Id == itemId);
                        if (livestock == null)
                        {
                            SaleMessageTextBlock.Text = "Selected livestock not found in database.";
                            return;
                        }

                        if (livestock.Quantity < qty)
                        {
                            SaleMessageTextBlock.Text = "Not enough livestock stock.";
                            return;
                        }

                        livestock.Quantity -= qty;
                        _db.Livestock.Update(livestock);
                    }
                    else
                    {
                        SaleMessageTextBlock.Text = "Unknown item type.";
                        return;
                    }

                    // Create sale record (don't rely on NumberBox.Value for price)
                    var sale = new Sale
                    {
                        ItemId = itemId,
                        ItemType = itemType,
                        ItemName = itemName,
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        // optionally set TotalAmount if your model uses it (or let DB computed column handle it)
                        Notes = NotesTextBox.Text,
                        SaleDate = DateTime.UtcNow
                    };

                    _db.Sales.Add(sale);

                    // Save both inventory changes + sale in one SaveChanges call
                    await _db.SaveChangesAsync();

                    // commit transaction
                    await transaction.CommitAsync();

                    // Update UI
                    SalesList.Insert(0, sale);
                    SaleMessageTextBlock.Text = "Sale recorded successfully.";

                    // Reset_inputs
                    QuantityNumberBox.Value = 0;
                    UnitPriceNumberBox.Value = 0;
                    NotesTextBox.Text = "";
                    TotalTextBlock.Text = "0.00";
                    ItemComboBox.SelectedIndex = -1;

                    // Refresh totals / KPIs if you have functions for that
                    // await LoadSalesAsync(); // optional: reload from DB
                    // await LoadMonthlyProfitAsync(); // if exists
                    // await LoadRoiAsync(); // if exists
                }
            }
            catch (Exception ex)
            {
                // show clear error message to help debugging
                SaleMessageTextBlock.Text = "Error recording sale: " + ex.Message;
                System.Diagnostics.Debug.WriteLine("SubmitSale error: " + ex.ToString());
            }
        }



        private async Task RecordTotalRevenueAsync()
        {
            var now = DateTime.UtcNow;

            // Get all sales for the current month
            var sales = await _db.Sales
                .Where(s => s.SaleDate.Month == now.Month && s.SaleDate.Year == now.Year)
                .ToListAsync();

            decimal totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice); // Compute in memory

            // Check if a record for this month already exists
            var existingRecord = await _db.MonthlyKpi
                .FirstOrDefaultAsync(k => k.Month == now.Month && k.Year == now.Year);

            if (existingRecord != null)
            {
                existingRecord.TotalRevenue = totalRevenue;
                // Optional: keep other fields null or default
            }
            else
            {
                var kpi = new MonthlyKpis
                {
                    Month = now.Month,
                    Year = now.Year,
                    TotalRevenue = totalRevenue
                    // Leave other fields null or 0
                };
                _db.MonthlyKpi.Add(kpi);
            }

            await _db.SaveChangesAsync();
        }

        private async Task RecordMonthlyKpiAsync()
        {
            var now = DateTime.UtcNow;

            var sales = await _db.Sales
                .Where(s => s.SaleDate.Month == now.Month && s.SaleDate.Year == now.Year)
                .ToListAsync();

            var expenses = await _db.Expenses
                .Where(e => e.Date.Month == now.Month && e.Date.Year == now.Year)
                .ToListAsync();

            decimal totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice); // computed in memory
            decimal totalExpenses = expenses.Sum(e => (decimal)e.Amount);
            decimal profit = totalRevenue - totalExpenses;
            decimal roi = totalExpenses == 0 ? 0 : (profit / totalExpenses) * 100;

            // Check if KPI for this month already exists
            var existingKpi = await _db.MonthlyKpi
                .FirstOrDefaultAsync(k => k.Month == now.Month && k.Year == now.Year);

            if (existingKpi != null)
            {
                existingKpi.TotalRevenue = totalRevenue;
                existingKpi.TotalExpenses = totalExpenses;
                existingKpi.Profit = profit;
                existingKpi.Roi = roi;
            }
            else
            {
                var kpi = new MonthlyKpis
                {
                    Month = now.Month,
                    Year = now.Year,
                    TotalRevenue = totalRevenue,
                    TotalExpenses = totalExpenses,
                    Profit = profit,
                    Roi = roi
                };
                _db.MonthlyKpi.Add(kpi);
            }

            await _db.SaveChangesAsync();
        }

        // ===========================
        // SEARCH & FILTER
        // ===========================
        private async void SalesSearchBox_TextChanged(object sender, TextChangedEventArgs e) => await FilterSalesAsync();
        private async void ApplyFilterButton_Click(object sender, RoutedEventArgs e) => await FilterSalesAsync();
        private async void FilterDateChanged(object sender, DatePickerValueChangedEventArgs e) => await FilterSalesAsync();

        private async Task FilterSalesAsync()
        {
            var query = _db.Sales.AsQueryable();

            var from = FromDatePicker.Date.Date;
            var to = ToDatePicker.Date.Date.AddDays(1).AddTicks(-1);

            query = query.Where(s => s.SaleDate >= from && s.SaleDate <= to);

            string search = SalesSearchBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.ItemName.Contains(search));
            }

            var filtered = await query.OrderByDescending(s => s.SaleDate).ToListAsync();
            SalesList.Clear();
            foreach (var s in filtered)
                SalesList.Add(s);
        }
    }
}
