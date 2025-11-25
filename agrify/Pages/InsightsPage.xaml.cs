using System.Text;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using WinRT.Interop;

using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using agrify.ViewModels;
using Windows.Storage;


namespace agrify.Pages
{
    public sealed partial class InsightsPage : Page
    {
        public InsightsViewModel ViewModel { get; }

        public InsightsPage()
        {
            this.InitializeComponent();
            ViewModel = new InsightsViewModel();
            this.DataContext = ViewModel;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                await ViewModel.LoadDashboardDataAsync();
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validation
                if (ViewModel.RecentProductionList == null || ViewModel.RecentProductionList.Count == 0)
                {
                    ShowErrorDialog("No data to export.");
                    return;
                }

                // 2. Build CSV Content
                var csv = new StringBuilder();
                csv.AppendLine("Harvest Date,Produce Type,Quantity,Weight (KG),Notes");

                foreach (var item in ViewModel.RecentProductionList)
                {
                    string safeNotes = item.Notes?.Replace(",", " ").Replace("\"", "\"\"") ?? "";
                    string date = item.HarvestDateFormatted ?? "";
                    string type = item.ProduceType ?? "";
                    string weight = item.Weight ?? "0";

                    csv.AppendLine($"{date},{type},{item.Quantity},{weight},\"{safeNotes}\"");
                }

                string csvContent = csv.ToString();

                // 3. Setup Picker
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
                savePicker.SuggestedFileName = $"FarmReport_{DateTime.Now:yyyy-MM-dd}.csv";
                savePicker.DefaultFileExtension = ".csv";

                // 4. Initialize picker with window handle
                var window = (Application.Current as App)?.MainWindow;
                if (window == null)
                {
                    ShowErrorDialog("Could not access main window.");
                    return;
                }

                var hWnd = WindowNative.GetWindowHandle(window);
                InitializeWithWindow.Initialize(savePicker, hWnd);

                // 5. Show picker and get file
                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file == null)
                {
                    // User cancelled
                    return;
                }

                // 6. Write to file using CachedFileManager (CRITICAL!)
                CachedFileManager.DeferUpdates(file);

                try
                {
                    // Try method 1: FileIO (best for Uno)
                    await FileIO.WriteTextAsync(file, csvContent);
                }
                catch
                {
                    // Fallback method 2: Stream
                    try
                    {
                        using (var stream = await file.OpenStreamForWriteAsync())
                        {
                            stream.SetLength(0); // Clear any existing content
                            using (var writer = new StreamWriter(stream, Encoding.UTF8))
                            {
                                await writer.WriteAsync(csvContent);
                                await writer.FlushAsync();
                            }
                        }
                    }
                    catch
                    {
                        // Fallback method 3: IRandomAccessStream
                        using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            stream.Size = 0; // Clear existing content
                            using (var outputStream = stream.GetOutputStreamAt(0))
                            {
                                using (var writer = new Windows.Storage.Streams.DataWriter(outputStream))
                                {
                                    writer.WriteString(csvContent);
                                    await writer.StoreAsync();
                                    await outputStream.FlushAsync();
                                }
                            }
                        }
                    }
                }

                // 7. Complete the write operation
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                if (status == FileUpdateStatus.Complete)
                {
                    ContentDialog successDialog = new ContentDialog
                    {
                        Title = "Export Successful",
                        Content = $"Report saved to:\n{file.Path}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                else
                {
                    ShowErrorDialog($"File was not saved successfully. Status: {status}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                ShowErrorDialog("Access denied. Please check app permissions in Windows Settings > Privacy > File system.");
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"Export failed: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
            }
        }

        private async void ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Export Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
