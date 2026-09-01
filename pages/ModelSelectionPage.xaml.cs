using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI;

namespace QML_Studio.pages
{
    public sealed partial class ModelSelectionPage : Page
    {
        private Border? _selectedCard;
        private readonly SolidColorBrush _purpleBrush = new SolidColorBrush(Color.FromArgb(255, 101, 71, 255));
        private readonly SolidColorBrush _defaultBorderBrush = new SolidColorBrush(Color.FromArgb(255, 221, 227, 240));
        private readonly SolidColorBrush _tabActiveBg = new SolidColorBrush(Color.FromArgb(255, 244, 241, 255));
        private readonly SolidColorBrush _tabInactiveBg = new SolidColorBrush(Microsoft.UI.Colors.White);
        private readonly SolidColorBrush _tabInactiveFg = new SolidColorBrush(Color.FromArgb(255, 102, 114, 142));

        public ModelSelectionPage()
        {
            this.InitializeComponent();
            SelectCard(QSVMCard);
        }

        private void SelectCard(Border card)
        {
            Border[] cards = { QSVMCard, QNNCard, VQCCard, QCNNCard, QKNNCard, QuantumKernelCard };
            foreach (var c in cards)
            {
                if (c == card)
                {
                    c.BorderBrush = _purpleBrush;
                    c.BorderThickness = new Thickness(2);
                }
                else
                {
                    c.BorderBrush = _defaultBorderBrush;
                    c.BorderThickness = new Thickness(1);
                }
            }
            _selectedCard = card;
        }

        private bool _isClassicalTabActive = false;

        private void QuantumModelsTab_Click(object sender, RoutedEventArgs e)
        {
            _isClassicalTabActive = false;
            HybridCrossStudyPanel.Visibility = Visibility.Collapsed;
            ModelCardsGrid.Visibility = Visibility.Visible;
            SetTabActive(QuantumModelsTab);
        }

        private void HybridModelsTab_Click(object sender, RoutedEventArgs e)
        {
            _isClassicalTabActive = false;
            HybridCrossStudyPanel.Visibility = Visibility.Visible;
            SetTabActive(HybridModelsTab);
        }

        private void ClassicalModelsTab_Click(object sender, RoutedEventArgs e)
        {
            _isClassicalTabActive = true;
            HybridCrossStudyPanel.Visibility = Visibility.Collapsed;
            ModelCardsGrid.Visibility = Visibility.Visible;
            SetTabActive(ClassicalModelsTab);
            this.Frame?.Navigate(typeof(ClassicalModelPage));
        }

        private void SetTabActive(Button activeTab)
        {
            Button[] tabs = { QuantumModelsTab, HybridModelsTab, ClassicalModelsTab };
            foreach (var t in tabs)
            {
                if (t == activeTab)
                {
                    t.Background = _tabActiveBg;
                    t.BorderBrush = _purpleBrush;
                    t.Foreground = _purpleBrush;
                }
                else
                {
                    t.Background = _tabInactiveBg;
                    t.BorderBrush = _defaultBorderBrush;
                    t.Foreground = _tabInactiveFg;
                }
            }
        }

        private async void RunHybridBenchmarkButton_Click(object sender, RoutedEventArgs e)
        {
            string qmlModel = (HybridQmlModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "QSVM";
            string classicalModel = (HybridClassicalModelComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Classical SVM";

            int.TryParse(HybridQubitsInput.Text, out int qubits);
            if (qubits <= 0) qubits = 4;

            int.TryParse(HybridShotsInput.Text, out int shots);
            if (shots <= 0) shots = 1024;

            string datasetPath = DataUploadPage.CurrentDatasetPath;

            HybridStatusBadge.Text = "Running Hybrid Cross-Study via Rust + Python...";
            RunHybridBenchmarkButton.IsEnabled = false;
            HybridResultsContainer.Visibility = Visibility.Visible;
            HybridChartLoading.Visibility = Visibility.Visible;
            HybridChartImage.Source = null;

            HybridCrossStudyResultModel result = null!;
            await Task.Run(() =>
            {
                result = DatasetBackend.RunHybridCrossStudy(datasetPath, qmlModel, classicalModel, qubits, shots);
            });

            RunHybridBenchmarkButton.IsEnabled = true;
            HybridChartLoading.Visibility = Visibility.Collapsed;

            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                HybridStatusBadge.Text = $"Cross-study error: {result?.Error ?? "Backend error"}";
                return;
            }

            // Populate Metric Cards
            HybridAccuracyValue.Text = $"{result.QmlAccuracy}% vs {result.ClassicalAccuracy}%";
            HybridAccuracyDiffText.Text = result.AccuracyImprovement >= 0
                ? $"+{result.AccuracyImprovement}% Quantum Advantage"
                : $"{result.AccuracyImprovement}% Differential";

            HybridF1Value.Text = $"{result.QmlF1}% vs {result.ClassicalF1}%";
            HybridTimeValue.Text = $"{result.QmlTimeSeconds}s vs {result.ClassicalTimeSeconds}s";
            HybridParamReductionValue.Text = $"{result.ParameterReductionPct}% Fewer Params";

            // Render Comparative Performance Graphic
            if (!string.IsNullOrEmpty(result.ChartImageBase64))
            {
                byte[] bytes = Convert.FromBase64String(result.ChartImageBase64);
                var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                }

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                HybridChartImage.Source = bitmap;
            }

            HybridStatusBadge.Text = "Cross-Study Benchmark Completed (Rust & Python)";
        }

        private void QSVMCard_PointerPressed(object sender, PointerRoutedEventArgs e) => SelectCard(QSVMCard);
        private void QNNCard_PointerPressed(object sender, PointerRoutedEventArgs e) => SelectCard(QNNCard);
        private void VQCCard_PointerPressed(object sender, PointerRoutedEventArgs e) => SelectCard(VQCCard);
        private void QCNNCard_PointerPressed(object sender, PointerRoutedEventArgs e) => SelectCard(QCNNCard);
        private void QKNNCard_PointerPressed(object sender, PointerRoutedEventArgs e) => SelectCard(QKNNCard);
        private void QuantumKernelCard_PointerPressed(object sender, PointerRoutedEventArgs e) => SelectCard(QuantumKernelCard);

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isClassicalTabActive)
            {
                this.Frame?.Navigate(typeof(ClassicalModelPage));
            }
            else
            {
                string selectedModel = "QSVM";
                if (_selectedCard == QNNCard) selectedModel = "QNN";
                else if (_selectedCard == VQCCard) selectedModel = "VQC";
                else if (_selectedCard == QCNNCard) selectedModel = "QCNN";
                else if (_selectedCard == QKNNCard) selectedModel = "QKNN";
                else if (_selectedCard == QuantumKernelCard) selectedModel = "Quantum Kernel";

                this.Frame?.Navigate(typeof(OtherModelPage), selectedModel);
            }
        }
    }
}
