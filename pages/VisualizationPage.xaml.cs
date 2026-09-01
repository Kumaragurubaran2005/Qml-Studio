using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace QML_Studio.pages
{
    public sealed partial class VisualizationPage : Page
    {
        private string _datasetPath = string.Empty;

        public VisualizationPage()
        {
            this.InitializeComponent();
            this.Loaded += VisualizationPage_Loaded;
        }

        private void VisualizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            _datasetPath = DataUploadPage.CurrentDatasetPath;

            if (!string.IsNullOrEmpty(_datasetPath) && File.Exists(_datasetPath))
            {
                PopulateFeatureComboBoxes(_datasetPath);
            }
            else
            {
                PlotStatusBadge.Text = "Please upload a dataset on the Data Upload page first.";
            }
        }

        private void PopulateFeatureComboBoxes(string filePath)
        {
            var analysis = DatasetBackend.AnalyzeDataset(filePath);
            if (analysis != null && analysis.Columns.Count > 0)
            {
                XColComboBox.Items.Clear();
                YColComboBox.Items.Clear();

                foreach (var col in analysis.Columns)
                {
                    XColComboBox.Items.Add(col.Name);
                    YColComboBox.Items.Add(col.Name);
                }

                if (XColComboBox.Items.Count > 0) XColComboBox.SelectedIndex = 0;
                if (YColComboBox.Items.Count > 1) YColComboBox.SelectedIndex = 1;
                else if (YColComboBox.Items.Count > 0) YColComboBox.SelectedIndex = 0;
            }
        }

        private void PlotTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlotPurposeText == null) return;
            string selected = (PlotTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            switch (selected)
            {
                case "Correlation Heatmap":
                    PlotPurposeText.Text = "Purpose: Feature Correlations";
                    break;
                case "Histogram":
                    PlotPurposeText.Text = "Purpose: Distribution of a Feature";
                    break;
                case "Bar Chart":
                    PlotPurposeText.Text = "Purpose: Category Frequencies";
                    break;
                case "Box Plot":
                    PlotPurposeText.Text = "Purpose: Distribution & Outliers";
                    break;
                case "Violin Plot":
                    PlotPurposeText.Text = "Purpose: Distribution Shape";
                    break;
                case "Scatter Plot":
                    PlotPurposeText.Text = "Purpose: Relationship Between Two Features";
                    break;
                case "Scatter Matrix":
                    PlotPurposeText.Text = "Purpose: Pairwise Feature Relationships";
                    break;
                case "Missing Value Chart":
                    PlotPurposeText.Text = "Purpose: Missing-Data Analysis";
                    break;
                case "Feature Distribution Grid":
                    PlotPurposeText.Text = "Purpose: Multiple Feature Distributions";
                    break;
                case "Class Distribution Chart":
                    PlotPurposeText.Text = "Purpose: Target-Class Balance";
                    break;
                case "Outlier Plot":
                    PlotPurposeText.Text = "Purpose: Detect Anomalous Observations";
                    break;
                default:
                    PlotPurposeText.Text = "Purpose: Data Exploration";
                    break;
            }
        }

        private async void GeneratePlotButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_datasetPath) || !File.Exists(_datasetPath))
            {
                PlotStatusBadge.Text = "No dataset active. Go to Data Upload first.";
                return;
            }

            string xCol = XColComboBox.SelectedItem?.ToString() ?? "";
            string yCol = YColComboBox.SelectedItem?.ToString() ?? "";
            string plotType = (PlotTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Correlation Heatmap";

            PlotStatusBadge.Text = "Rust engine contacting Python for plot generation...";
            PlaceholderContainer.Visibility = Visibility.Collapsed;
            PlotImage.Source = null;
            PlotLoadingContainer.Visibility = Visibility.Visible;
            PlotLoadingRing.IsActive = true;
            GeneratePlotButton.IsEnabled = false;

            PlotResultModel result = null!;
            await Task.Run(() =>
            {
                result = DatasetBackend.GeneratePlot(_datasetPath, xCol, yCol, plotType);
            });

            PlotLoadingRing.IsActive = false;
            PlotLoadingContainer.Visibility = Visibility.Collapsed;
            GeneratePlotButton.IsEnabled = true;

            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                PlotStatusBadge.Text = $"Plot error: {result?.Error ?? "Backend error"}";
                PlaceholderContainer.Visibility = Visibility.Visible;
                return;
            }

            if (!string.IsNullOrEmpty(result.ImageBase64))
            {
                byte[] bytes = Convert.FromBase64String(result.ImageBase64);
                
                // Save PNG into Output Workspace
                string pngPath = OutputDirectoryManager.SaveVisualizationImage(bytes, plotType);

                // Save PDF into Output Workspace if available
                if (!string.IsNullOrEmpty(result.PdfBase64))
                {
                    try
                    {
                        byte[] pdfBytes = Convert.FromBase64String(result.PdfBase64);
                        string sanitizedPlotName = plotType.Replace(" ", "_").Replace("/", "_");
                        string timestamp = DateTime.Now.ToString("HHmmss");
                        string pdfPath = System.IO.Path.Combine(OutputDirectoryManager.VisualizationsFolder, $"{sanitizedPlotName}_{timestamp}.pdf");
                        System.IO.File.WriteAllBytes(pdfPath, pdfBytes);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to save PDF: {ex.Message}");
                    }
                }

                var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                }

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);

                PlotImage.Source = bitmap;
                PlotStatusBadge.Text = $"Plot ({plotType}) & PDF exported to workspace output folder!";
            }
            else
            {
                PlotStatusBadge.Text = "Plot generated (Data parsed)";
                PlaceholderContainer.Visibility = Visibility.Visible;
            }
        }
    }
}
