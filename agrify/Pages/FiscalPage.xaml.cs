using agrify.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.EntityFrameworkCore;


namespace agrify;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
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
    // TOTAL REVENUE
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

    // ROI
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

