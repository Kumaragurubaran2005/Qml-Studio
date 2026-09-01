using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Windows.UI;

namespace QML_Studio.pages
{
    public sealed partial class ClassicalModelPage : Page
    {
        private string? _selectedCsvPath;
        private string? _batchQueryFilePath;
        private string? _lastOutputCsvPath;
        private string _activeModel = "KNN";

        public ClassicalModelPage()
        {
            this.InitializeComponent();
            this.Loaded += ClassicalModelPage_Loaded;
            UpdateModelUI();
        }

        private void ClassicalModelPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(DataUploadPage.CurrentDatasetPath) && File.Exists(DataUploadPage.CurrentDatasetPath))
            {
                _selectedCsvPath = DataUploadPage.CurrentDatasetPath;
                if (SelectedFileTextBlock != null)
                {
                    SelectedFileTextBlock.Text = DataUploadPage.CurrentDatasetPath;
                }
                PopulateTargetColumns();
            }
            else
            {
                if (SelectedFileTextBlock != null && string.IsNullOrEmpty(SelectedFileTextBlock.Text))
                {
                    SelectedFileTextBlock.Text = string.Empty;
                    SelectedFileTextBlock.PlaceholderText = "No dataset loaded yet. Please upload a dataset on Data Upload page first.";
                }
            }
        }

        private void PopulateTargetColumns()
        {
            if (TargetColumnComboBox == null || string.IsNullOrEmpty(_selectedCsvPath)) return;

            TargetColumnComboBox.Items.Clear();
            var analysis = DatasetBackend.AnalyzeDataset(_selectedCsvPath);
            if (analysis != null && analysis.Columns.Count > 0)
            {
                foreach (var col in analysis.Columns)
                {
                    TargetColumnComboBox.Items.Add(col.Name);
                }
                TargetColumnComboBox.SelectedIndex = analysis.Columns.Count - 1; // Default to last column
            }
            else
            {
                TargetColumnComboBox.Items.Add("target");
                TargetColumnComboBox.SelectedIndex = 0;
            }
        }

        private void BtnModelSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _activeModel = tag;
                UpdateModelUI();
            }
        }

        private void UpdateModelUI()
        {
            var activeBg = new SolidColorBrush(Color.FromArgb(255, 20, 141, 245));
            var inactiveBg = new SolidColorBrush(Color.FromArgb(255, 224, 230, 237));
            var whiteFg = new SolidColorBrush(Microsoft.UI.Colors.White);
            var darkFg = new SolidColorBrush(Color.FromArgb(255, 17, 23, 41));

            Button[] btns = { BtnKnn, BtnSvm, BtnMlp, BtnLogReg, BtnKernel };
            string[] tags = { "KNN", "SVM", "MLP", "LOGREG", "KERNEL" };

            for (int i = 0; i < btns.Length; i++)
            {
                if (tags[i] == _activeModel)
                {
                    btns[i].Background = activeBg;
                    btns[i].Foreground = whiteFg;
                }
                else
                {
                    btns[i].Background = inactiveBg;
                    btns[i].Foreground = darkFg;
                }
            }

            switch (_activeModel)
            {
                case "KNN":
                    SelectedModelTitle.Text = "Selected Model: Classical KNN (Instance-Based Direct Prediction)";
                    Param1TextBox.Header = "K (Nearest Neighbors)";
                    Param1TextBox.Text = "3";
                    Param2TextBox.Header = "Unused for KNN";
                    Param2TextBox.IsEnabled = false;
                    RunModelButton.Content = "⚡ Find k-Nearest Neighbors & Predict Class";
                    break;
                case "SVM":
                    SelectedModelTitle.Text = "Selected Model: Classical Support Vector Machine (RBF)";
                    Param1TextBox.Header = "C Parameter";
                    Param1TextBox.Text = "1.0";
                    Param2TextBox.Header = "Gamma";
                    Param2TextBox.Text = "0.5";
                    Param2TextBox.IsEnabled = true;
                    RunModelButton.Content = "⚡ Predict & Evaluate Classical SVM";
                    break;
                case "MLP":
                    SelectedModelTitle.Text = "Selected Model: Classical MLP Neural Network";
                    Param1TextBox.Header = "Epochs";
                    Param1TextBox.Text = "25";
                    Param2TextBox.Header = "Hidden Layer Neurons";
                    Param2TextBox.Text = "4";
                    Param2TextBox.IsEnabled = true;
                    RunModelButton.Content = "⚡ Train & Evaluate Classical MLP";
                    break;
                case "LOGREG":
                    SelectedModelTitle.Text = "Selected Model: Classical Logistic Regression";
                    Param1TextBox.Header = "Max Iterations";
                    Param1TextBox.Text = "30";
                    Param2TextBox.Header = "Unused for Logistic Regression";
                    Param2TextBox.IsEnabled = false;
                    RunModelButton.Content = "⚡ Predict & Evaluate Logistic Regression";
                    break;
                case "KERNEL":
                    SelectedModelTitle.Text = "Selected Model: Classical Kernel SVM";
                    Param1TextBox.Header = "SVM C Parameter";
                    Param1TextBox.Text = "1.0";
                    Param2TextBox.Header = "Gamma";
                    Param2TextBox.Text = "0.5";
                    Param2TextBox.IsEnabled = true;
                    RunModelButton.Content = "⚡ Predict & Evaluate Kernel SVM";
                    break;
            }
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
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
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls");
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedCsvPath = file.Path;
                SelectedFileTextBlock.Text = file.Path;
                DataUploadPage.CurrentDatasetPath = file.Path;
                PopulateTargetColumns();
            }
        }

        private async void BrowseQueryFileButton_Click(object sender, RoutedEventArgs e)
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
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls");
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _batchQueryFilePath = file.Path;
                BatchQueryFileTextBox.Text = file.Path;
            }
        }

        private void OpenBatchPredictionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_lastOutputCsvPath) && File.Exists(_lastOutputCsvPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(_lastOutputCsvPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    ResultTextBlock.Text += $"\nCould not open predictions file: {ex.Message}";
                }
            }
        }

        private async void RunModelButton_Click(object sender, RoutedEventArgs e)
        {
            string csvPath = SelectedFileTextBlock.Text.Trim();
            if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
            {
                if (!string.IsNullOrEmpty(_selectedCsvPath) && File.Exists(_selectedCsvPath))
                {
                    csvPath = _selectedCsvPath;
                }
                else if (!string.IsNullOrEmpty(DataUploadPage.CurrentDatasetPath) && File.Exists(DataUploadPage.CurrentDatasetPath))
                {
                    csvPath = DataUploadPage.CurrentDatasetPath;
                }
                else
                {
                    ResultTextBlock.Text = "Error: Please upload a dataset on the Data Upload page first.";
                    return;
                }
            }

            string targetCol = TargetColumnComboBox.SelectedItem?.ToString() ?? "target";
            string queryVector = QueryVectorTextBox.Text.Trim();
            int.TryParse(Param1TextBox.Text, out int kNeighbors);
            if (kNeighbors <= 0) kNeighbors = 3;

            OpenBatchPredictionsButton.Visibility = Visibility.Collapsed;

            // If Batch Query File is selected, run batch prediction across all test rows
            if (!string.IsNullOrEmpty(_batchQueryFilePath) && File.Exists(_batchQueryFilePath))
            {
                ResultTextBlock.Text = $"Running Batch Query Predictions across '{Path.GetFileName(_batchQueryFilePath)}' via '{_activeModel}'...";

                BatchPredictionResultModel batchRes = null!;
                await Task.Run(() =>
                {
                    batchRes = DatasetBackend.PredictBatch(csvPath, _batchQueryFilePath);
                });

                if (batchRes == null || !string.IsNullOrEmpty(batchRes.Error))
                {
                    ResultTextBlock.Text = $"Batch Error: {batchRes?.Error ?? "Backend error"}";
                    return;
                }

                _lastOutputCsvPath = batchRes.OutputCsvPath;
                OpenBatchPredictionsButton.Visibility = Visibility.Visible;

                ResultTextBlock.Text = $"✅ Batch Query Prediction Successful!\n" +
                                       $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                       $"Reference Training Dataset: {Path.GetFileName(csvPath)}\n" +
                                       $"Batch Query File: {Path.GetFileName(_batchQueryFilePath)}\n" +
                                       $"Total Test Query Rows Processed: {batchRes.TotalRows}\n" +
                                       $"Appended CSV Output Path: {batchRes.OutputCsvPath}\n\n" +
                                       $"Sample Query Rows with Appended Predicted Target:\n" +
                                       string.Join("\n", batchRes.PredictionsPreview.Take(10).Select(r => $"Row #{r.RowIndex}: Predicted Target = '{r.PredictedTarget}' (Confidence = {r.ConfidencePct}%)"));
                return;
            }

            // Otherwise, run Single Sample Query or Holdout Evaluation
            ResultTextBlock.Text = $"Processing Model '{_activeModel}' on Target '{targetCol}' ({Path.GetFileName(csvPath)})...";

            string modelName = _activeModel switch
            {
                "KNN" => "KNN (K-Nearest Neighbors)",
                "SVM" => "SVM (Support Vector Machine)",
                "MLP" => "MLP (Neural Network)",
                "LOGREG" => "Logistic Regression",
                "KERNEL" => "Kernel SVM",
                _ => "KNN (K-Nearest Neighbors)"
            };

            QmlTrainingResultModel result = null!;
            await Task.Run(() =>
            {
                result = DatasetBackend.TrainQmlModel(csvPath, targetCol, modelName, 4, 1024, "ZZFeatureMap", "FidelityQuantumKernel", "COBYLA");
            });

            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                ResultTextBlock.Text = $"Execution Error: {result?.Error ?? "Backend error"}";
                return;
            }

            string neighborOutput = "";
            if (result.NeighborsInfo != null && result.NeighborsInfo.Count > 0)
            {
                neighborOutput = "\n📍 Top-k Nearest Neighbors Distance Breakdown:\n" +
                                 string.Join("\n", result.NeighborsInfo) + "\n";
            }

            string queryHeader = !string.IsNullOrEmpty(result.QueryPrediction) ? $"{result.QueryPrediction}\n\n" : "";

            ResultTextBlock.Text = $"{queryHeader}" +
                                   $"✅ Model '{modelName}' Execution Successful on Target Column '{targetCol}'!\n" +
                                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                                   $"Active Dataset: {Path.GetFileName(csvPath)}\n" +
                                   $"Target Class Column: '{targetCol}'\n" +
                                   $"Total Samples: {result.TotalSamplesCount} (Train: {result.TrainSamplesCount} | Test: {result.TestSamplesCount})\n" +
                                   $"Train Accuracy: {result.TrainAccuracy}%\n" +
                                   $"Test Accuracy: {result.TestAccuracy}%\n" +
                                   $"Precision: {result.Precision}%\n" +
                                   $"Recall: {result.Recall}%\n" +
                                   $"F1-Score: {result.F1Score}%\n" +
                                   $"ROC-AUC: {result.RocAuc:F3}\n" +
                                   $"Execution Time: {result.TrainTimeSeconds}s\n" +
                                   $"{neighborOutput}";
        }
    }
}
