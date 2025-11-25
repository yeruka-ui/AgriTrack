using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using agrify.Data;
using agrify.Models;

namespace agrify.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public DashboardViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        // === PROPERTIES ===
        private string _totalLivestock = "0";
        public string TotalLivestock { get => _totalLivestock; set { _totalLivestock = value; OnPropertyChanged(); } }

        public ObservableCollection<StockAlertItem> LowStockItems { get; set; } = new ObservableCollection<StockAlertItem>();

        private string _activeHerdLabel = "+ 0 Active";
        public string ActiveHerdLabel { get => _activeHerdLabel; set { _activeHerdLabel = value; OnPropertyChanged(); } }

        private string _milkCostPerLiter = "Rs 0";
        public string MilkCostPerLiter { get => _milkCostPerLiter; set { _milkCostPerLiter = value; OnPropertyChanged(); } }

        private string _qurbaniReadyCount = "0";
        public string QurbaniReadyCount { get => _qurbaniReadyCount; set { _qurbaniReadyCount = value; OnPropertyChanged(); } }

        private string _feedDailyCost = "Rs 0";
        public string FeedDailyCost { get => _feedDailyCost; set { _feedDailyCost = value; OnPropertyChanged(); } }

        private string _newBirthsCount = "0";
        public string NewBirthsCount { get => _newBirthsCount; set { _newBirthsCount = value; OnPropertyChanged(); } }

        public ObservableCollection<BarChartItem> MilkProductionTrend { get; set; } = new ObservableCollection<BarChartItem>();
        public ObservableCollection<ReminderItem> UpcomingReminders { get; set; } = new ObservableCollection<ReminderItem>();

        // === LOAD REAL DATA ===
        public async Task LoadDataAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    using (var db = new AgrifyDbContext())
                    {
                        // Load Tables
                        var animals = db.Livestock.ToList();
                        var produce = db.Produce.ToList();
                        var schedules = db.CalendarTasks.ToList();
                        var expenses = db.Expenses.ToList(); // NEW: Load Expenses

                        // 1. METRICS (EXISTING)
                        int totalCount = animals.Count;
                        int activeCount = animals.Count(a => (a.Status ?? "").ToLower() != "dead" && (a.Status ?? "").ToLower() != "sold");
                        int qurbaniCount = animals.Count(a => (a.Status ?? "").ToLower().Contains("fattening") || (a.Notes ?? "").ToLower().Contains("qurbani"));

                        // BIRTHS (This was already working, kept as is)
                        int birthsCount = animals.Count(a => a.DateOfBirth.Month == DateTime.Now.Month && a.DateOfBirth.Year == DateTime.Now.Year);

                        // 2. FINANCIAL METRICS (THE FIX)

                        // A. FEED DAILY COST (Average of last 30 days)
                        var last30Days = DateTime.Now.AddDays(-30);
                        var recentFeedExpenses = expenses
                            .Where(e => e.Category == "Feed" && e.Date >= last30Days)
                            .Sum(e => (double)e.Amount);

                        double dailyFeedCost = recentFeedExpenses / 30; // Avg per day

                        // B. MILK PRODUCTION COST (Total Expenses / Total Liters in last 30 days)
                        // We use ALL farm expenses (Feed, Vet, Labor) to get true cost, or just Feed for simple cost.
                        // Let's use Feed + Vet + Maintenance for "Production Cost"
                        var productionExpenses = expenses
                            .Where(e => (e.Category == "Feed" || e.Category == "Medicine/Vet" || e.Category == "Maintenance") && e.Date >= last30Days)
                            .Sum(e => (double)e.Amount);

                        var totalLitersProduced = produce
                            .Where(p => (p.ProduceType ?? "").ToLower().Contains("milk") && p.HarvestDate >= last30Days)
                            .Sum(p => ParseWeight(p.Weight));

                        double costPerLiter = 0;
                        if (totalLitersProduced > 0)
                        {
                            costPerLiter = productionExpenses / totalLitersProduced;
                        }

                        // 3. CHART DATA (Milk Trend)
                        var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
                        var chartItems = new System.Collections.Generic.List<BarChartItem>();
                        double maxVal = 1;

                        foreach (var day in last7Days)
                        {
                            double dailySum = produce
                                .Where(p => (p.ProduceType ?? "").ToLower().Contains("milk") && p.HarvestDate.Date == day)
                                .Sum(p => ParseWeight(p.Weight));

                            if (dailySum > maxVal) maxVal = dailySum;

                            chartItems.Add(new BarChartItem
                            {
                                DayLabel = day.ToString("ddd"),
                                ValueLabel = dailySum > 0 ? dailySum.ToString("0") : "",
                                RawValue = dailySum
                            });
                        }

                        // Color Logic
                        foreach (var item in chartItems)
                        {
                            item.BarHeight = (item.RawValue / maxVal) * 100;
                            if (item.BarHeight < 4 && item.RawValue > 0) item.BarHeight = 4; // Min height for visibility

                            item.BarColor = item.DayLabel == DateTime.Today.ToString("ddd")
                                ? new SolidColorBrush(Colors.DarkGreen) // Today is Green
                                : new SolidColorBrush(Colors.Black);     // Others are White
                        }

                        // 4. REMINDERS (Kept your existing logic, it was good)
                        var upcomingTasks = schedules
                            .Where(s => s.StartTime.Date >= DateTime.Today && s.StartTime.Date <= DateTime.Today.AddDays(7))
                            .OrderBy(s => s.StartTime)
                            .Select(s =>
                            {
                                string statusLabel = "Upcoming";
                                var color = Colors.LightBlue;
                                if (s.IsComplete) { statusLabel = "Done"; color = Colors.Gray; }
                                else if (s.StartTime.Date == DateTime.Today) { statusLabel = "Today"; color = Colors.LightGreen; }

                                return new ReminderItem
                                {
                                    Date = s.StartTime.ToString("MMM dd"),
                                    Title = s.Title,
                                    Status = statusLabel,
                                    StatusColor = color
                                };
                            }).ToList();

                        // 5. UPDATE UI
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            TotalLivestock = totalCount.ToString();
                            ActiveHerdLabel = $"+ {activeCount} Active Herd";
                            QurbaniReadyCount = qurbaniCount.ToString();
                            NewBirthsCount = $"+{birthsCount}"; // This works!

                            // UPDATED: Bind real values
                            MilkCostPerLiter = $"Rs {costPerLiter:N0}";
                            FeedDailyCost = $"Rs {dailyFeedCost:N0}";

                            MilkProductionTrend.Clear();
                            foreach (var item in chartItems) MilkProductionTrend.Add(item);

                            UpcomingReminders.Clear();
                            if (upcomingTasks.Any())
                            {
                                foreach (var item in upcomingTasks) UpcomingReminders.Add(item);
                            }
                            else
                            {
                                UpcomingReminders.Add(new ReminderItem { Date = "--", Title = "No upcoming tasks", Status = "", StatusColor = Colors.Gray });
                            }
                        });
                        // Inside LoadDataAsync...

                        // 6. LOW STOCK ALERTS (SMART AGGREGATION)
                        var lowStock = new System.Collections.Generic.List<StockAlertItem>();

                        // STRATEGY: Group items by name and SUM their quantities. 
                        // We filter out items with 0 quantity (sold out) to keep the list clean.
                        var produceInventory = produce
                            .Where(p => p.Quantity > 0)
                            .GroupBy(p => p.ProduceType)
                            .Select(g => new
                            {
                                Name = g.Key,
                                TotalQty = g.Sum(p => p.Quantity)
                            })
                            .Where(i => i.TotalQty < 20) // The Threshold (Adjust to your preference)
                            .ToList();

                        foreach (var item in produceInventory)
                        {
                            // Optional: Skip "Milk" if you consider it a daily harvest rather than stocked inventory
                            // if (item.Name.ToLower().Contains("milk")) continue;

                            lowStock.Add(new StockAlertItem
                            {
                                Name = item.Name,
                                QuantityLabel = $"{item.TotalQty} Left"
                            });
                        }

                        // Check Expenses (e.g. Feed)
                        // Since your Expense table just tracks money spent, not quantity remaining, 
                        // we can't reliably calculate "Feed Stock" from the Expenses table alone. 
                        // You would need an "Inventory" table for that. 
                        // For now, the Produce Aggregation above fixes the "7 Milk Alerts" bug.

                        // UPDATE UI
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            // ... (Other updates remain the same) ...

                            // Bind Low Stock
                            LowStockItems.Clear();
                            if (lowStock.Any())
                            {
                                foreach (var item in lowStock) LowStockItems.Add(item);
                            }
                            else
                            {
                                // If stock is healthy, show a green/positive message
                                LowStockItems.Add(new StockAlertItem { Name = "Inventory Healthy", QuantityLabel = "All Good" });
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Dashboard Error: " + ex.Message);
                }
            });
        }

        private double ParseWeight(string w)
        {
            if (string.IsNullOrEmpty(w)) return 0;
            var clean = new string(w.Where(c => char.IsDigit(c) || c == '.').ToArray());
            if (double.TryParse(clean, out double r)) return r;
            return 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ReminderItem
    {
        public string Date { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public Windows.UI.Color StatusColor { get; set; }
        public SolidColorBrush StatusBrush => new SolidColorBrush(StatusColor);
    }
}
