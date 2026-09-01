using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace QML_Studio.pages
{
    public sealed partial class PredictionPage : Page
    {
        private readonly Dictionary<string, TextBox> _featureInputs = new();
        public ObservableCollection<BatchRowPreviewModel> BatchPreviewRows { get; } = new();
        private string _datasetPath = string.Empty;
        private string _batchTestFilePath = string.Empty;
        private string _latestOutputCsvPath = string.Empty;

        public PredictionPage()
        {
            this.InitializeComponent();
            this.Loaded += PredictionPage_Loaded;
            BatchPreviewListView.ItemsSource = BatchPreviewRows;
        }

        private void PredictionPage_Loaded(object sender, RoutedEventArgs e)
        {
            _datasetPath = DataUploadPage.CurrentDatasetPath;
            PopulateDynamicFeatureInputs();
        }

        private void PopulateDynamicFeatureInputs()
        {
            DynamicFeatureInputsPanel.Children.Clear();
            _featureInputs.Clear();

            if (!string.IsNullOrEmpty(_datasetPath) && File.Exists(_datasetPath))
            {
                var analysis = DatasetBackend.AnalyzeDataset(_datasetPath);
                if (analysis != null && analysis.Columns.Count > 0)
                {
                    // Exclude the last column (Target Column) from predictor input fields
                    int predictorCount = Math.Max(1, analysis.Columns.Count - 1);
                    PredictionStatusBadge.Text = $"Loaded {predictorCount} predictor features (Target column excluded).";

                    for (int i = 0; i < predictorCount; i++)
                    {
                        var col = analysis.Columns[i];
                        var stack = new StackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 4) };
                        var label = new TextBlock
                        {
                            Text = $"{col.Name} ({col.DataType})",
                            Foreground = (SolidColorBrush)Resources["SecondaryTextBrush"],
                            FontSize = 9,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        };

                        string defaultVal = string.IsNullOrEmpty(col.SampleVal) ? "0.0" : col.SampleVal;
                        var textBox = new TextBox
                        {
                            Text = defaultVal,
                            Height = 30,
                            FontSize = 10,
                            Background = new SolidColorBrush(Color.FromArgb(255, 245, 246, 252))
                        };

                        stack.Children.Add(label);
                        stack.Children.Add(textBox);
                        DynamicFeatureInputsPanel.Children.Add(stack);

                        _featureInputs[col.Name] = textBox;
                    }
                    return;
                }
            }

            PredictionStatusBadge.Text = "No active dataset loaded. Displaying default predictor fields.";
            string[] defaultFeats = { "Feature 1", "Feature 2", "Feature 3", "Feature 4" };
            foreach (var feat in defaultFeats)
            {
                var stack = new StackPanel { Spacing = 3, Margin = new Thickness(0, 0, 0, 4) };
                var label = new TextBlock
                {
                    Text = feat,
                    Foreground = (SolidColorBrush)Resources["SecondaryTextBrush"],
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                var textBox = new TextBox
                {
                    Text = "0.5",
                    Height = 30,
                    FontSize = 10,
                    Background = new SolidColorBrush(Color.FromArgb(255, 245, 246, 252))
                };

                stack.Children.Add(label);
                stack.Children.Add(textBox);
                DynamicFeatureInputsPanel.Children.Add(stack);

                _featureInputs[feat] = textBox;
            }
        }

        private void SingleSampleTab_Click(object sender, RoutedEventArgs e)
        {
            SingleSampleTab.Background = (SolidColorBrush)Resources["PurpleBrush"];
            SingleSampleTab.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            BatchUploadTab.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            BatchUploadTab.Foreground = (SolidColorBrush)Resources["SecondaryTextBrush"];

            SingleSampleContainer.Visibility = Visibility.Visible;
            BatchUploadContainer.Visibility = Visibility.Collapsed;

            SingleSampleResultsGrid.Visibility = Visibility.Visible;
            BatchResultsCard.Visibility = Visibility.Collapsed;
        }

        private void BatchUploadTab_Click(object sender, RoutedEventArgs e)
        {
            BatchUploadTab.Background = (SolidColorBrush)Resources["PurpleBrush"];
            BatchUploadTab.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            SingleSampleTab.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            SingleSampleTab.Foreground = (SolidColorBrush)Resources["SecondaryTextBrush"];

            SingleSampleContainer.Visibility = Visibility.Collapsed;
            BatchUploadContainer.Visibility = Visibility.Visible;

            SingleSampleResultsGrid.Visibility = Visibility.Collapsed;
            BatchResultsCard.Visibility = Visibility.Visible;
        }

        private async void BrowseBatchFileButton_Click(object sender, RoutedEventArgs e)
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
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _batchTestFilePath = file.Path;
                BatchFilePathText.Text = $"Selected: {file.Name} ({file.Path})";
                RunBatchPredictionButton.IsEnabled = true;
            }
        }

        private async void RunPredictionButton_Click(object sender, RoutedEventArgs e)
        {
            var dict = new Dictionary<string, string>();
            foreach (var kvp in _featureInputs)
            {
                dict[kvp.Key] = kvp.Value.Text.Trim();
            }

            string jsonStr = JsonSerializer.Serialize(dict);
            PredictionStatusBadge.Text = "Running Python + Rust Quantum Inference Engine...";
            RunPredictionButton.IsEnabled = false;

            PredictionResultModel result = null!;
            await Task.Run(() =>
            {
                result = DatasetBackend.PredictSample(_datasetPath, jsonStr);
            });

            RunPredictionButton.IsEnabled = true;

            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                PredictionStatusBadge.Text = $"Prediction error: {result?.Error ?? "Backend error"}";
                return;
            }

            PredictionClassText.Text = result.PredictedClassLabel;
            ConfidenceValue.Text = $"{result.ConfidencePercentage:F1}%";
            ConfidenceBarFill.Width = 290.0 * (result.ConfidencePercentage / 100.0);

            InsightConfidenceValue.Text = result.ConfidencePercentage > 90.0 ? $"Optimal ({result.ConfidencePercentage:F1}%)" : $"High ({result.ConfidencePercentage:F1}%)";
            InsightModelValue.Text = "QSVM / Quantum Kernel Classifier";
            InsightFeaturesValue.Text = result.TopContributingFeatures;

            PredictionStatusBadge.Text = "Quantum Single Inference Completed!";
        }

        private async void RunBatchPredictionButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_batchTestFilePath) || !File.Exists(_batchTestFilePath))
            {
                BatchSummaryText.Text = "Please select a valid test CSV file first.";
                return;
            }

            PredictionStatusBadge.Text = "Executing Batch Model Predictions via Python + Rust...";
            RunBatchPredictionButton.IsEnabled = false;
            BatchSummaryText.Text = "Running batch predictions across all test rows...";

            BatchPredictionResultModel result = null!;
            await Task.Run(() =>
            {
                result = DatasetBackend.PredictBatch(_datasetPath, _batchTestFilePath);
            });

            RunBatchPredictionButton.IsEnabled = true;

            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                BatchSummaryText.Text = $"Batch Error: {result?.Error ?? "Backend error"}";
                PredictionStatusBadge.Text = "Batch prediction failed.";
                return;
            }

            _latestOutputCsvPath = result.OutputCsvPath;

            // Save copy into Output Workspace models/predictions folder
            string savedWorkspacePath = OutputDirectoryManager.SavePredictionOutput(result.OutputCsvPath);
            if (!string.IsNullOrEmpty(savedWorkspacePath)) _latestOutputCsvPath = savedWorkspacePath;

            BatchSummaryText.Text = $"Successfully processed {result.TotalRows} test rows! Predictions saved to: {Path.GetFileName(_latestOutputCsvPath)}";

            BatchPreviewRows.Clear();
            foreach (var row in result.PredictionsPreview)
            {
                BatchPreviewRows.Add(row);
            }

            PredictionStatusBadge.Text = $"Batch Predictions Completed! Output saved to workspace output folder.";
        }

        private void OpenPredictionsCsvButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_latestOutputCsvPath) && File.Exists(_latestOutputCsvPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _latestOutputCsvPath,
                    UseShellExecute = true
                });
            }
            else
            {
                string modelsFolder = OutputDirectoryManager.GetModelsFolder();
                if (Directory.Exists(modelsFolder))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = modelsFolder,
                        UseShellExecute = true
                    });
                }
            }
        }

        private void ViewQuantumCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame?.Navigate(typeof(VisualizationPage));
        }
    }
}
