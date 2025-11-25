using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using agrify.Data;
using agrify.Models;

namespace agrify.ViewModels
{
    public class InsightsViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public InsightsViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        // =========================================================
        // PROPERTIES
        // =========================================================

        private string _totalHerdText = "0";
        public string TotalHerdText { get => _totalHerdText; set { _totalHerdText = value; OnPropertyChanged(); } }

        private string _activeCountText = "0";
        public string ActiveCountText { get => _activeCountText; set { _activeCountText = value; OnPropertyChanged(); } }

        private string _deadCountText = "0";
        public string DeadCountText { get => _deadCountText; set { _deadCountText = value; OnPropertyChanged(); } }

        private string _mortalityRateText = "0%";
        public string MortalityRateText { get => _mortalityRateText; set { _mortalityRateText = value; OnPropertyChanged(); } }

        private string _healthStatusText = "Unknown";
        public string HealthStatusText { get => _healthStatusText; set { _healthStatusText = value; OnPropertyChanged(); } }

        private SolidColorBrush _healthStatusColor = new SolidColorBrush(Colors.LightGray);
        public SolidColorBrush HealthStatusColor { get => _healthStatusColor; set { _healthStatusColor = value; OnPropertyChanged(); } }

        private string _yieldTodayText = "0.0 KG";
        public string YieldTodayText { get => _yieldTodayText; set { _yieldTodayText = value; OnPropertyChanged(); } }

        private string _yieldTrendText = "--%";
        public string YieldTrendText { get => _yieldTrendText; set { _yieldTrendText = value; OnPropertyChanged(); } }

        private SolidColorBrush _yieldTrendColor = new SolidColorBrush(Colors.LightGray);
        public SolidColorBrush YieldTrendColor { get => _yieldTrendColor; set { _yieldTrendColor = value; OnPropertyChanged(); } }

        // --- CHART DATA (Manual Implementation) ---
        public ObservableCollection<BarChartItem> WeeklyTrendList { get; set; } = new ObservableCollection<BarChartItem>();

        public ObservableCollection<ProduceDisplayItem> RecentProductionList { get; set; } = new ObservableCollection<ProduceDisplayItem>();

        // =========================================================
        // LOGIC
        // =========================================================

        public async Task LoadDashboardDataAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var db = new AgrifyDbContext())
                    {
                        // 1. LIVESTOCK LOGIC
                        var allAnimals = db.Livestock
                            .Where(l => l.AnimalName != null && l.Breed != null && l.TagNumber != null)
                            .ToList();
                        int totalCount = allAnimals.Count;
                        int deadCount = allAnimals.Count(a => !string.IsNullOrEmpty(a.Status) && (a.Status.ToLower().Contains("dead") || a.Status.ToLower().Contains("deceased")));
                        int activeCount = totalCount - deadCount;
                        double mortalityRate = totalCount > 0 ? ((double)deadCount / totalCount) * 100 : 0;
                        string healthStatus = mortalityRate > 5 ? "Critical" : (mortalityRate > 2 ? "Monitor" : "Good");
                        var healthColor = mortalityRate > 5 ? Colors.Red : (mortalityRate > 2 ? Colors.Orange : Colors.LightGreen);

                        // 2. PRODUCE LOGIC
                        var allProduce = db.Produce.ToList();
                        var today = DateTime.Today;

                        double todayWeight = allProduce.Where(p => p.HarvestDate.Date == today).Sum(p => ParseWeightString(p.Weight));
                        double yesterdayWeight = allProduce.Where(p => p.HarvestDate.Date == today.AddDays(-1)).Sum(p => ParseWeightString(p.Weight));

                        string trendText = "--";
                        var trendColor = Colors.LightGray;
                        if (yesterdayWeight > 0)
                        {
                            double growth = ((todayWeight - yesterdayWeight) / yesterdayWeight) * 100;
                            trendText = $"{growth:+0.0;-0.0}%";
                            trendColor = growth >= 0 ? Colors.LightGreen : Colors.OrangeRed;
                        }

                        // --- 3. CHART LOGIC (Manual Calculation) ---
                        var last7Days = Enumerable.Range(0, 7)
                                                  .Select(i => DateTime.Today.AddDays(-6 + i))
                                                  .ToList();

                        var dailyTotals = new Dictionary<DateTime, double>();
                        double maxDailyValue = 1;

                        foreach (var day in last7Days)
                        {
                            double sum = allProduce.Where(p => p.HarvestDate.Date == day).Sum(p => ParseWeightString(p.Weight));
                            dailyTotals[day] = sum;
                            if (sum > maxDailyValue) maxDailyValue = sum;
                        }

                        var chartItems = new List<BarChartItem>();
                        foreach (var day in last7Days)
                        {
                            double val = dailyTotals[day];
                            // Normalize height: (Value / Max) * 120px
                            double barHeight = (val / maxDailyValue) * 120;
                            if (barHeight < 4) barHeight = 4; // Min height

                            chartItems.Add(new BarChartItem
                            {
                                DayLabel = day.ToString("ddd"),
                                ValueLabel = val > 0 ? $"{val:0.#}" : "",
                                BarHeight = barHeight,
                                BarColor = val > 0 ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.DarkGray)
                            });
                        }

                        // 4. RECENT ITEMS
                        var recentItems = allProduce.OrderByDescending(p => p.HarvestDate).Take(20)
                            .Select(p => new ProduceDisplayItem
                            {
                                HarvestDateFormatted = p.HarvestDate.ToString("MM/dd/yyyy"),
                                ProduceType = p.ProduceType,
                                Quantity = p.Quantity,
                                Weight = p.Weight,
                                Notes = p.Notes
                            }).ToList();

                        // UPDATE UI
                        _dispatcherQueue?.TryEnqueue(() =>
                        {
                            TotalHerdText = totalCount.ToString();
                            ActiveCountText = activeCount.ToString();
                            DeadCountText = deadCount.ToString();
                            MortalityRateText = $"{mortalityRate:F1}%";
                            HealthStatusText = healthStatus;
                            HealthStatusColor = new SolidColorBrush(healthColor);
                            YieldTodayText = $"{todayWeight:F1} KG";
                            YieldTrendText = trendText;
                            YieldTrendColor = new SolidColorBrush(trendColor);

                            RecentProductionList.Clear();
                            foreach (var item in recentItems) RecentProductionList.Add(item);

                            WeeklyTrendList.Clear();
                            foreach (var item in chartItems) WeeklyTrendList.Add(item);
                        });
                    }
                });
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private double ParseWeightString(string weightStr)
        {
            if (string.IsNullOrWhiteSpace(weightStr)) return 0.0;
            try
            {
                string cleanStr = weightStr.ToLower().Replace("kg", "").Replace("lbs", "").Replace("g", "").Trim();
                if (double.TryParse(cleanStr, out double result)) return result;
            }
            catch { }
            return 0.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProduceDisplayItem
    {
        public string HarvestDateFormatted { get; set; }
        public string ProduceType { get; set; }
        public int Quantity { get; set; }
        public string Weight { get; set; }
        public string Notes { get; set; }
    }

    public class BarChartItem
    {
        public string DayLabel { get; set; }
        public string ValueLabel { get; set; }
        public double BarHeight { get; set; }
        public SolidColorBrush BarColor { get; set; }

        public double RawValue { get; set; }
    }
}
