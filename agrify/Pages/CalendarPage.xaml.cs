using System;
using System.Collections.Generic; // Needed for List<Color>
using System.Collections.ObjectModel;
using System.Globalization; // Needed for IValueConverter
using System.Linq;
using agrify.Data; // <-- ADD THIS
using agrify.Models;
using Microsoft.EntityFrameworkCore; // <-- ADD THIS
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data; // Needed for IValueConverter
using Microsoft.UI.Xaml.Navigation; // <-- ADD THIS
using Windows.UI; // Needed for Colors.Green


namespace agrify.Pages
{
    public sealed partial class CalendarPage : Page
    {
        // UPDATED: This is now a simple List, not an ObservableCollection
        // It will act as our in-memory cache of all tasks.
        private List<CalendarTask> AllTasks;
        private List<Holiday> AllHolidays;

        public CalendarPage()
        {
            this.InitializeComponent();
            AllTasks = new List<CalendarTask>();
            AllHolidays = new List<Holiday>(); // Initialize the new list

        }

        /// <summary>
        /// NEW: This method is called automatically when the page is navigated to.
        /// This is the correct place to load data from the database.
        /// </summary>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await LoadTasksFromDatabase(); // Load data from DB

            // Now that data is loaded, initialize the views
            NewTaskDatePicker.Date = DateTimeOffset.Now;
            UpdateSidebarTasks(DateTime.Today);
            UpdateAgendaView(DateTime.Today);

            // Force the calendar to show density dots
            RefreshCalendarDisplay();
        }

        /// <summary>
        /// NEW: Loads all tasks from the `agrifyDB` database.
        /// </summary>
        private async System.Threading.Tasks.Task LoadTasksFromDatabase()
        {
            try
            {
                await using var db = new AgrifyDbContext();
                // Load all tasks into our local cache
                AllTasks = await db.CalendarTasks.ToListAsync();
                AllHolidays = await db.Holidays.ToListAsync();

            }
            catch (Exception ex)
            {
                // This will show an error in the task list if the DB fails
                SidebarHeaderTextBlock.Text = "Error loading tasks";
                AllTasks.Add(new CalendarTask { Title = $"DB Error: {ex.Message}", StartTime = DateTime.Today });
            }
        }

        /// <summary>
        /// Populates the sidebar with tasks for the newly selected date.
        /// </summary>
        private void MainCalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (args.AddedDates.Any())
            {
                UpdateSidebarTasks(args.AddedDates.First().DateTime);
            }
        }

        /// <summary>
        /// Filters the sidebar list to show only tasks for the given date.
        /// </summary>
        private void UpdateSidebarTasks(DateTime date)
        {
            SidebarHeaderTextBlock.Text = $"Tasks for {date:MMMM d, yyyy}";

            var tasksForDay = AllTasks
                .Where(t => t.StartTime.Date == date.Date && !t.IsComplete)
                .OrderBy(t => t.StartTime);
            TasksListView.ItemsSource = tasksForDay;

            var holidaysForMonth = AllHolidays
                .Where(h => h.Date.Month == date.Month && h.Date.Year == date.Year)
                .OrderBy(h => h.Date);
            HolidaysListView.ItemsSource = holidaysForMonth;

        }

        /// <summary>
        /// Populates the main "Agenda" view with upcoming tasks.
        /// </summary>
        private void UpdateAgendaView(DateTime startDate)
        {
            var sevenDaysFromStart = startDate.Date.AddDays(7);

            var upcomingTasks = AllTasks
                .Where(t => t.StartTime.Date >= startDate.Date &&
                            t.StartTime.Date < sevenDaysFromStart &&
                            !t.IsComplete)
                .OrderBy(t => t.StartTime);

            UpcomingTasksListView.ItemsSource = upcomingTasks;
        }

        /// <summary>
        /// Handles saving a new task from the flyout.
        /// UPDATED to be async and save to DB
        /// </summary>
        private async void SaveTaskButton_Click(object sender, RoutedEventArgs e)
        {
            TaskErrorTextBlock.Text = ""; // Clear old errors

            // 1. Validation
            if (string.IsNullOrWhiteSpace(NewTaskTitleTextBox.Text))
            {
                TaskErrorTextBlock.Text = "Title is required.";
                return;
            }

            // 2. Create the new task object
            DateTime taskDate = NewTaskDatePicker.Date.DateTime.Date;
            TimeSpan taskTime = NewTaskTimePicker.Time;

            var newTask = new CalendarTask
            {
                Title = NewTaskTitleTextBox.Text,
                StartTime = taskDate + taskTime,
                EndTime = taskDate + taskTime + new TimeSpan(1, 0, 0), // Default 1hr
                Notes = NewTaskNotesTextBox.Text,
                IsComplete = false
            };

            // 3. Add to our main list AND database
            try
            {
                await using var db = new AgrifyDbContext();
                db.CalendarTasks.Add(newTask);
                await db.SaveChangesAsync();

                // Now that it's saved, add it to our local cache
                AllTasks.Add(newTask);
            }
            catch (Exception ex)
            {
                TaskErrorTextBlock.Text = $"Error saving: {ex.Message}";
                return;
            }

            // 4. Refresh BOTH lists and close the flyout
            UpdateSidebarTasks(newTask.StartTime.Date);
            UpdateAgendaView(DateTime.Today); // Refresh agenda
            MainCalendarView.SelectedDates.Clear();
            MainCalendarView.SelectedDates.Add(newTask.StartTime.Date);
            AddTaskFlyout.Hide();

            // 5. Clear the form
            NewTaskTitleTextBox.Text = "";
            NewTaskNotesTextBox.Text = "";
            NewTaskTimePicker.Time = DateTime.Now.TimeOfDay;

            // 6. Force the calendar to re-draw its day items
            RefreshCalendarDisplay();
        }

        /// <summary>
        /// Toggles the visibility of the sidebar.
        /// </summary>
        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (SidebarColumn.Width.Value > 0)
            {
                SidebarColumn.Width = new GridLength(0);
                ToggleSidebarButton.Content = "\uE72B"; // Back
            }
            else
            {
                SidebarColumn.Width = new GridLength(320); // Expand
                ToggleSidebarButton.Content = "\uE72A"; // Forward
            }
        }

        /// <summary>
        /// Fires for every day drawn on the calendar.
        /// We use this to add "density dots" for pending tasks.
        /// </summary>
        private void MainCalendarView_DayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (args.Phase != 0) return;
            var date = args.Item.Date.Date;

            var colors = new List<Color>();

            // Find any pending tasks for this day (from our local cache)
            var tasksForDay = AllTasks
                .Where(t => t.StartTime.Date == date && !t.IsComplete)
                .ToList();

            if (tasksForDay.Any())
            {
                colors.Add(Colors.LightGreen);
            }

            var holidayForDay = AllHolidays
                .FirstOrDefault(h => h.Date.Date == date);

            if (holidayForDay != null)
            {
                colors.Add(Colors.Red); // Add a red dot for holidays
            }

            if (colors.Any())
            {
                args.Item.SetDensityColors(colors);
            }

            else
            {
                args.Item.SetDensityColors(null);
            }
        }

        /// <summary>
        /// Handles the click event for the "Complete" checkmark button.
        /// UPDATED to be async and update the DB
        /// </summary>
        private async void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button completeButton && completeButton.DataContext is CalendarTask taskToComplete)
            {
                // 1. Mark as complete
                taskToComplete.IsComplete = true;

                // 2. Update this in the database
                try
                {
                    await using var db = new AgrifyDbContext();
                    db.CalendarTasks.Update(taskToComplete);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Show error, but don't stop (we can still update the UI)
                }

                // 3. Refresh all our views
                UpdateSidebarTasks(MainCalendarView.SelectedDates.FirstOrDefault().DateTime);
                UpdateAgendaView(DateTime.Today);

                // 4. Force the calendar to redraw its density dots
                RefreshCalendarDisplay();
            }
        }

        /// <summary>
        /// Helper method to force the CalendarView to redraw its day items.
        /// </summary>
        private void RefreshCalendarDisplay()
        {
            // We "trick" it by toggling the date
            var currentDisplay = MainCalendarView.SelectedDates.FirstOrDefault();
            if (currentDisplay == default) currentDisplay = DateTimeOffset.Now;

            MainCalendarView.SetDisplayDate(currentDisplay.AddDays(1));
            MainCalendarView.SetDisplayDate(currentDisplay);
        }

        // We no longer need LoadSampleTasks()
        // private void LoadSampleTasks() { ... }
    }

    // --- Helper Converters for the Agenda ListView ---


    public class DateTimeToDayStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("ddd").ToUpper(); // "MON", "TUE"
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class DateTimeToDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("MMM d"); // "Nov 16"
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
