using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace QML_Studio.pages
{
    public sealed partial class DataUploadPage : Page
    {
        public ObservableCollection<ColumnSummaryModel> FeatureColumns { get; } = new();
        private string _activeFilePath = string.Empty;

        public static string CurrentDatasetPath { get; set; } = string.Empty;

        public DataUploadPage()
        {
            this.InitializeComponent();
            ColumnListView.ItemsSource = FeatureColumns;
        }

        private void ShowLoading(string text)
        {
            LoadingText.Text = text;
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
        }

        private void HideLoading()
        {
            LoadingRing.IsActive = false;
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private async void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            if (App.MainWindow != null)
            {
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);
            }

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await ProcessFileAndAnalyzeAsync(file.Path, file.Name);
            }
        }

        private void UploadContainer_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop to analyze dataset";
            e.DragUIOverride.IsCaptionVisible = true;
        }

        private async void UploadContainer_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0 && items[0] is StorageFile file)
                {
                    await ProcessFileAndAnalyzeAsync(file.Path, file.Name);
                }
            }
        }

        private async Task ProcessFileAndAnalyzeAsync(string filePath, string fileName)
        {
            _activeFilePath = filePath;
            CurrentDatasetPath = filePath;

            // Initialize Project Output Workspace Folder
            string projFolder = OutputDirectoryManager.InitializeProjectFolder(filePath);

            DropInstructionText.Text = fileName;
            DropOrText.Text = filePath;
            BackendStatusBadge.Text = "Analyzing via Rust Backend...";

            ShowLoading("Analyzing dataset via Rust Engine...");

            try
            {
                DatasetAnalysisResultModel result = null!;
                await Task.Run(() =>
                {
                    result = DatasetBackend.AnalyzeDataset(filePath);
                });

                if (result == null || !string.IsNullOrEmpty(result.Error))
                {
                    BackendStatusBadge.Text = $"Error: {result?.Error ?? "Backend error"}";
                    return;
                }

                TotalRowsValue.Text = result.TotalRows.ToString("N0");
                TotalColsValue.Text = result.TotalColumns.ToString("N0");
                ColsBreakdownText.Text = $"{result.NumericColsCount} Numeric • {result.CategoricalColsCount} Categorical";

                double nullRatio = result.TotalRows > 0 ? (double)result.TotalNulls / (result.TotalRows * result.TotalColumns) * 100.0 : 0.0;
                TotalNullsValue.Text = $"{result.TotalNulls.ToString("N0")} ({nullRatio:F1}%)";
                NullsRatioText.Text = result.TotalNulls == 0 ? "100% Complete (No Missing Data)" : $"{result.TotalNulls} missing data points detected";

                double healthScore = Math.Max(0.0, 100.0 - nullRatio * 2.0);
                DataHealthValue.Text = $"{healthScore:F0}%";

                double validPct = Math.Max(0.0, 100.0 - nullRatio);
                double nullPct = 100.0 - validPct;
                ValidBarCol.Width = new GridLength(validPct, GridUnitType.Star);
                NullBarCol.Width = new GridLength(nullPct, GridUnitType.Star);
                ValidBarLabel.Text = $"Valid Data: {validPct:F1}%";
                NullBarLabel.Text = $"Missing: {nullPct:F1}%";

                FeatureColumns.Clear();
                foreach (var col in result.Columns)
                {
                    FeatureColumns.Add(col);
                }

                BackendStatusBadge.Text = $"Output Workspace: {Path.GetFileName(projFolder)}";
            }
            catch (Exception ex)
            {
                BackendStatusBadge.Text = $"Analysis Error: {ex.Message}";
            }
            finally
            {
                HideLoading();
            }
        }

        private async void CleanDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_activeFilePath))
            {
                BackendStatusBadge.Text = "Please upload a dataset first.";
                return;
            }

            ShowLoading("Cleaning & encoding via Python + Rust...");
            BackendStatusBadge.Text = "Cleaning via Python + Rust...";

            try
            {
                CleanDatasetResultModel cleanResult = null!;
                await Task.Run(() =>
                {
                    cleanResult = DatasetBackend.CleanDataset(_activeFilePath);
                });

                if (cleanResult == null || !string.IsNullOrEmpty(cleanResult.Error))
                {
                    BackendStatusBadge.Text = $"Cleaning error: {cleanResult?.Error ?? "Backend error"}";
                    return;
                }

                // Save cleaned dataset copy into Output Workspace
                string savedCleanedPath = OutputDirectoryManager.SaveCleanedDataset(cleanResult.CleanedFile);

                string removedDetails = cleanResult.RemovedLowVarianceCols.Count > 0
                    ? $"Removed {cleanResult.RemovedLowVarianceCols.Count} low-variance/constant feature(s) ({string.Join(", ", cleanResult.RemovedLowVarianceCols)})"
                    : "No features had near-zero variance (<0.01 threshold)";

                CleanSummaryCard.Visibility = Visibility.Visible;
                CleanSummaryDetails.Text = $"Cleaned dataset saved to output folder | " +
                                           $"Scaled {cleanResult.ScaledNumericCols.Count} numeric features | " +
                                           $"Encoded {cleanResult.EncodedCategoricalCols.Count} categorical features | " +
                                           $"{removedDetails} | " +
                                           $"Imputed {cleanResult.NullsImputedCount} nulls.";

                await ProcessFileAndAnalyzeAsync(cleanResult.CleanedFile, Path.GetFileName(cleanResult.CleanedFile));
                BackendStatusBadge.Text = "Cleaned & Preprocessed (Saved in Workspace Output Folder)";
            }
            catch (Exception ex)
            {
                BackendStatusBadge.Text = $"Cleaning Error: {ex.Message}";
            }
            finally
            {
                HideLoading();
            }
        }

        private void VisualizeDataButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame?.Navigate(typeof(VisualizationPage));
        }

        private void DevelopModelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame?.Navigate(typeof(ModelSelectionPage));
        }
    }
}
