using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using agrify.Data;
using agrify.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace agrify.Pages
{
    public sealed partial class FiscalPage : Page
    {
        private ObservableCollection<Expense> _expenseList = new();
        private ObservableCollection<Investment> _investmentList = new();

        public FiscalPage()
        {
            this.InitializeComponent();
            ExpensesGrid.ItemsSource = _expenseList;
            InvestmentsGrid.ItemsSource = _investmentList;

            // Set default date
            ExpDatePicker.Date = DateTimeOffset.Now;

            Loaded += FiscalPage_Loaded;
        }

        private async void FiscalPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAllData();
        }

        private async Task RefreshAllData()
        {
            try
            {
                using (var db = new AgrifyDbContext())
                {
                    await db.Database.EnsureCreatedAsync();

                    // 1. Load History Lists
                    var expenses = await db.Expenses.OrderByDescending(x => x.Date).ToListAsync();
                    _expenseList.Clear();
                    foreach (var ex in expenses) _expenseList.Add(ex);

                    var investments = await db.Investments.OrderByDescending(x => x.Date).ToListAsync();
                    _investmentList.Clear();
                    foreach (var inv in investments) _investmentList.Add(inv);

                    // 2. Calculate KPIs
                    // Revenue comes from SALES table
                    double totalRevenue = await db.Sales.SumAsync(s => (double)(s.Quantity * s.UnitPrice));

                    double totalExpenses = expenses.Sum(x => (double)x.Amount);
                    double totalInvestments = investments.Sum(x => (double)x.Amount);
                    double netProfit = totalRevenue - totalExpenses;

                    // 3. Update UI
                    TotalRevenueText.Text = $"₱{totalRevenue:N2}";

                    TotalProfitText.Text = $"₱{netProfit:N2}";
                    if (netProfit >= 0) TotalProfitText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LimeGreen);
                    else TotalProfitText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

                    if (totalInvestments > 0)
                    {
                        double roi = ((totalRevenue - totalInvestments) / totalInvestments) * 100;
                        RoiText.Text = $"{roi:F1}%";
                    }
                    else
                    {
                        RoiText.Text = "N/A";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fiscal Load Error: {ex.Message}");
            }
        }

        // ============================
        // ADD EXPENSE LOGIC
        // ============================
        private async void AddExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(ExpNameBox.Text) || ExpAmountBox.Value <= 0) return;

            var newExpense = new Expense
            {
                ExpenseName = ExpNameBox.Text,
                Category = (ExpCategoryCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Other",
                Amount = (decimal)ExpAmountBox.Value,
                Date = ExpDatePicker.Date.DateTime
            };

            using (var db = new AgrifyDbContext())
            {
                db.Expenses.Add(newExpense);
                await db.SaveChangesAsync();
            }

            // Reset UI
            ExpNameBox.Text = "";
            ExpAmountBox.Value = 0;

            // Refresh
            await RefreshAllData();
        }

        // ============================
        // ADD INVESTMENT LOGIC
        // ============================
        private async void AddInvestmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InvSourceBox.Text) || InvAmountBox.Value <= 0) return;

            var newInv = new Investment
            {
                Source = InvSourceBox.Text,
                Amount = (decimal)InvAmountBox.Value,
                Date = DateTime.UtcNow
            };

            using (var db = new AgrifyDbContext())
            {
                db.Investments.Add(newInv);
                await db.SaveChangesAsync();
            }

            InvSourceBox.Text = "";
            InvAmountBox.Value = 0;

            await RefreshAllData();
        }

        // ============================
        // TAB SWITCHING
        // ============================
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (ExpensesGrid == null || InvestmentsGrid == null) return;

            if (TabExpenses.IsChecked == true)
            {
                ExpensesGrid.Visibility = Visibility.Visible;
                InvestmentsGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                ExpensesGrid.Visibility = Visibility.Collapsed;
                InvestmentsGrid.Visibility = Visibility.Visible;
            }
        }
    }
}
