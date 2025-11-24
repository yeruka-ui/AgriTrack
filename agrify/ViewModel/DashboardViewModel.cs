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
                        var animals = db.Livestock.ToList();
                        var produce = db.Produce.ToList();
                        var schedules = db.CalendarTasks.ToList();

                        // 1. METRICS
                        int totalCount = animals.Count;
                        int activeCount = animals.Count(a => (a.Status ?? "").ToLower() != "dead" && (a.Status ?? "").ToLower() != "sold" && (a.Status ?? "").ToLower() != "deceased");
                        int qurbaniCount = animals.Count(a => (a.Status ?? "").ToLower().Contains("fattening") || (a.Notes ?? "").ToLower().Contains("qurbani"));
                        int birthsCount = animals.Count(a => a.DateOfBirth.Month == DateTime.Now.Month && a.DateOfBirth.Year == DateTime.Now.Year);

                        // 2. CHART DATA
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

                        foreach (var item in chartItems)
                        {
                            item.BarHeight = (item.RawValue / maxVal) * 100;
                            if (item.BarHeight < 4 && item.RawValue > 0) item.BarHeight = 4;
                            item.BarColor = item.DayLabel == DateTime.Today.ToString("ddd")
                                ? new SolidColorBrush(Colors.LightGreen)
                                : new SolidColorBrush(Colors.White);
                        }

                        // 3. REMINDERS (Prepared Logic)
                        var upcomingTasks = schedules
                            .Where(s => s.StartTime.Date >= DateTime.Today && s.StartTime.Date <= DateTime.Today.AddDays(7))
                            .OrderBy(s => s.StartTime)
                            .Select(s =>
                            {
                                string statusLabel = "Upcoming";
                                var color = Colors.LightBlue;

                                if (s.IsComplete)
                                {
                                    statusLabel = "Done";
                                    color = Colors.Gray;
                                }
                                else if (s.StartTime.Date == DateTime.Today)
                                {
                                    statusLabel = "Today";
                                    color = Colors.LightGreen;
                                }

                                return new ReminderItem
                                {
                                    Date = s.StartTime.ToString("MMM dd"),
                                    Title = s.Title,
                                    Status = statusLabel,
                                    StatusColor = color
                                };
                            })
                            .ToList();

                        // 4. UPDATE UI (THE FIX IS HERE)
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            TotalLivestock = totalCount.ToString();
                            ActiveHerdLabel = $"+ {activeCount} Active Herd";
                            QurbaniReadyCount = qurbaniCount.ToString();
                            NewBirthsCount = $"+{birthsCount}";
                            MilkCostPerLiter = "Rs 1,156";
                            FeedDailyCost = "Rs 0";

                            // Update Chart
                            MilkProductionTrend.Clear();
                            foreach (var item in chartItems) MilkProductionTrend.Add(item);

                            // FIX: Update Reminders List! (This was missing)
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
