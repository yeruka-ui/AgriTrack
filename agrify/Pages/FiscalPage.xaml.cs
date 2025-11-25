using agrify.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace agrify.Pages
{
    public sealed partial class FiscalPage : Page
    {
        private readonly AgrifyDbContext _db;

        public FiscalPage()
        {
            this.InitializeComponent();
            _db = new AgrifyDbContext();

            // Load Total Revenue when page opens
            _ = LoadTotalRevenueAsync();
        }

        private async Task LoadTotalRevenueAsync()
        {
            // Sum all sales in the database
            var sales = await _db.Sales.ToListAsync();
            decimal totalRevenue = sales.Sum(s => s.Quantity * s.UnitPrice);

            TotalRevenueText.Text = totalRevenue.ToString("N2");
        }

        private void CalculateProfit_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(RevenueInput.Text, out decimal revenue) &&
                decimal.TryParse(ExpenseInput.Text, out decimal expense))
            {
                decimal profit = revenue - expense;
                MonthlyProfitText.Text = profit.ToString("N2");
            }
        }

        private void CalculateRoi_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(InvestmentInput.Text, out decimal investment) &&
                decimal.TryParse(RevenueForRoiInput.Text, out decimal revenue))
            {
                if (investment <= 0)
                {
                    RoiText.Text = "0%";
                    return;
                }

                decimal roi = ((revenue - investment) / investment) * 100;
                RoiText.Text = roi.ToString("F2") + "%";
            }
        }

        // Call this after a new sale is submitted in your SalesReport page
        public async Task RefreshTotalRevenueAsync()
        {
            await LoadTotalRevenueAsync();
        }
    }
}
