using System;
using System.Collections.Generic;
using System.IO;
using agrify.Pages;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using agrify.Data;
using agrify.Models;
using agrify.Models.Category;
using Microsoft.EntityFrameworkCore; 
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using agrify.Data;
using Windows.Foundation.Collections;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace agrify.Pages
{

    public sealed partial class FiscalPage : Page
    {
        private readonly AgrifyDbContext _db;

        public FiscalPage()
        {
            this.InitializeComponent();
            _db = new AgrifyDbContext();
            LoadKpis();
        }
        private async void LoadKpis()
        {
            await LoadTotalRevenue();
            await LoadMonthlyProfit();
            await LoadRoi();
        }
        private async Task LoadTotalRevenue()
        {
            var total = await _db.Revenues.SumAsync(r => (decimal?)r.Amount) ?? 0;
            TotalRevenueText.Text = total.ToString("N2");
        }

        // MONTHLY PROFIT
        private async Task LoadMonthlyProfit()
        {
            var now = DateTime.UtcNow;

            var monthlyRevenue = await _db.Revenues
                .Where(r => r.Date.Month == now.Month && r.Date.Year == now.Year)
                .SumAsync(r => (decimal?)r.Amount) ?? 0;

            var monthlyExpenses = await _db.Expenses
                .Where(e => e.Date.Month == now.Month && e.Date.Year == now.Year)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var profit = monthlyRevenue - monthlyExpenses;

            MonthlyProfitText.Text = profit.ToString("N2");
        }

        private async Task LoadRoi()
        {
            var totalRevenue = await _db.Revenues.SumAsync(r => (decimal?)r.Amount) ?? 0;
            var totalInvestment = await _db.Investments.SumAsync(i => (decimal?)i.Amount) ?? 0;

            if (totalInvestment == 0)
            {
                RoiText.Text = "0%";
                return;
            }

            var roi = ((totalRevenue - totalInvestment) / totalInvestment) * 100;
            RoiText.Text = $"{roi:F2}%";
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
    }
}
