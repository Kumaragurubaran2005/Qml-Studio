using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace QML_Studio
{
    public sealed partial class OtherModelPage : Page
    {
        private string _datasetPath = string.Empty;

        public OtherModelPage()
        {
            this.InitializeComponent();
            this.Loaded += OtherModelPage_Loaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string modelName && ModelTypeComboBox != null)
            {
                foreach (ComboBoxItem item in ModelTypeComboBox.Items)
                {
                    string content = item.Content?.ToString() ?? "";
                    if (content.Contains(modelName, StringComparison.OrdinalIgnoreCase))
                    {
                        ModelTypeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void OtherModelPage_Loaded(object sender, RoutedEventArgs e)
        {
            _datasetPath = pages.DataUploadPage.CurrentDatasetPath;
            if (!string.IsNullOrEmpty(_datasetPath) && File.Exists(_datasetPath))
            {
                ActiveDatasetText.Text = $"Active Dataset: {Path.GetFileName(_datasetPath)} | Ready for Model Studio";
                PopulateTargetColumns();
            }
            else
            {
                ActiveDatasetText.Text = "No active dataset loaded. Please upload & clean a dataset on the Data Upload page first.";
            }
            UpdateDynamicModelConfigurationUI();
        }

        private void PopulateTargetColumns()
        {
            if (TargetColumnComboBox == null || string.IsNullOrEmpty(_datasetPath)) return;

            TargetColumnComboBox.Items.Clear();
            var analysis = DatasetBackend.AnalyzeDataset(_datasetPath);
            if (analysis != null && analysis.Columns.Count > 0)
            {
                foreach (var col in analysis.Columns)
                {
                    TargetColumnComboBox.Items.Add(col.Name);
                }
                TargetColumnComboBox.SelectedIndex = analysis.Columns.Count - 1;

                ValidationMappingBadge.Text = $"✓ Feature Mapping: Auto ({analysis.Columns.Count - 1} features → 4 Qubits)";
            }
            else
            {
                TargetColumnComboBox.Items.Add("target");
                TargetColumnComboBox.SelectedIndex = 0;
            }
        }

        private void ModelTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelTitleText == null) return;
            string selected = (ModelTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "QSVM";
            ModelTitleText.Text = $"{selected} Configuration, Training & Testing Studio";
            UpdateDynamicModelConfigurationUI();
        }

        private void UpdateDynamicModelConfigurationUI()
        {
            if (DynamicField1Label == null || DynamicField1ComboBox == null || DynamicField2Label == null || DynamicField2ComboBox == null) return;

            string selected = (ModelTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "QSVM";
            string upper = selected.ToUpper();

            DynamicField1ComboBox.Items.Clear();
            DynamicField2ComboBox.Items.Clear();
            AdvField1ComboBox?.Items.Clear();

            // Classical Models
            if (upper.Contains("KNN (K-NEAREST"))
            {
                ModelDescriptionText.Text = "Classical k-Nearest Neighbors classifier using vector distance metrics for majority voting.";
                DynamicField1Label.Text = "Neighbors (k)";
                DynamicField1ComboBox.Items.Add("5");
                DynamicField1ComboBox.Items.Add("3");
                DynamicField1ComboBox.Items.Add("7");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Distance Metric";
                DynamicField2ComboBox.Items.Add("Euclidean");
                DynamicField2ComboBox.Items.Add("Manhattan");
                DynamicField2ComboBox.Items.Add("Cosine");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
            else if (upper.Contains("SVM (SUPPORT"))
            {
                ModelDescriptionText.Text = "Classical maximum-margin Support Vector Machine (SVM) classifier.";
                DynamicField1Label.Text = "Kernel Function";
                DynamicField1ComboBox.Items.Add("RBF");
                DynamicField1ComboBox.Items.Add("Linear");
                DynamicField1ComboBox.Items.Add("Polynomial");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "SVM Parameter C";
                DynamicField2ComboBox.Items.Add("1.0");
                DynamicField2ComboBox.Items.Add("0.5");
                DynamicField2ComboBox.Items.Add("2.0");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
            else if (upper.Contains("MLP (NEURAL"))
            {
                ModelDescriptionText.Text = "Classical Multi-Layer Perceptron (MLP) neural network with dense hidden layers.";
                DynamicField1Label.Text = "Activation Function";
                DynamicField1ComboBox.Items.Add("ReLU");
                DynamicField1ComboBox.Items.Add("Tanh");
                DynamicField1ComboBox.Items.Add("Logistic");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Solver / Optimizer";
                DynamicField2ComboBox.Items.Add("Adam");
                DynamicField2ComboBox.Items.Add("SGD");
                DynamicField2ComboBox.Items.Add("LBFGS");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Visible;
                AdvField1Label.Text = "Hidden Layers";
                AdvField1ComboBox.Items.Add("64, 32");
                AdvField1ComboBox.Items.Add("128, 64");
                AdvField1ComboBox.SelectedIndex = 0;

                AdvField2Label.Text = "Learning Rate";
                AdvField2TextBox.Text = "0.001";
                AdvField3Label.Text = "Epochs / Max Iter";
                AdvField3TextBox.Text = "100";
                AdvField4Label.Text = "Batch Size";
                AdvField4TextBox.Text = "32";
            }
            else if (upper.Contains("LOGISTIC REGRESSION"))
            {
                ModelDescriptionText.Text = "Classical linear logistic model outputting calibrated class probabilities via Sigmoid/Softmax.";
                DynamicField1Label.Text = "Regularization";
                DynamicField1ComboBox.Items.Add("L2");
                DynamicField1ComboBox.Items.Add("L1");
                DynamicField1ComboBox.Items.Add("None");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Regularization C";
                DynamicField2ComboBox.Items.Add("1.0");
                DynamicField2ComboBox.Items.Add("0.5");
                DynamicField2ComboBox.Items.Add("2.0");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
            else if (upper.Contains("KERNEL SVM"))
            {
                ModelDescriptionText.Text = "Classical kernel-based Support Vector Machine using non-linear Mercer feature kernels.";
                DynamicField1Label.Text = "Classical Kernel";
                DynamicField1ComboBox.Items.Add("RBF Kernel");
                DynamicField1ComboBox.Items.Add("Polynomial Kernel");
                DynamicField1ComboBox.Items.Add("Sigmoid Kernel");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "SVM Parameter C";
                DynamicField2ComboBox.Items.Add("1.0");
                DynamicField2ComboBox.Items.Add("0.5");
                DynamicField2ComboBox.Items.Add("2.0");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
            // Quantum Models
            else if (upper.Contains("QSVM"))
            {
                ModelDescriptionText.Text = "Quantum kernel maps features to Hilbert space; classical SVM computes optimal decision boundary. No trainable circuit ansatz or gradient optimization required.";
                DynamicField1Label.Text = "Quantum Kernel";
                DynamicField1ComboBox.Items.Add("FidelityQuantumKernel");
                DynamicField1ComboBox.Items.Add("ProjectedQuantumKernel");
                DynamicField1ComboBox.Items.Add("CosineQuantumKernel");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "SVM Parameter C";
                DynamicField2ComboBox.Items.Add("1.0");
                DynamicField2ComboBox.Items.Add("0.5");
                DynamicField2ComboBox.Items.Add("2.0");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
            else if (upper.Contains("QNN"))
            {
                ModelDescriptionText.Text = "Trainable parameterized quantum neural network optimized via classical gradient descent.";
                DynamicField1Label.Text = "Ansatz Circuit";
                DynamicField1ComboBox.Items.Add("RealAmplitudes");
                DynamicField1ComboBox.Items.Add("TwoLocal");
                DynamicField1ComboBox.Items.Add("EfficientSU2");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Optimizer";
                DynamicField2ComboBox.Items.Add("Adam");
                DynamicField2ComboBox.Items.Add("COBYLA");
                DynamicField2ComboBox.Items.Add("SPSA");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Visible;
                AdvField1Label.Text = "Gradient Method";
                AdvField1ComboBox.Items.Add("ParameterShift");
                AdvField1ComboBox.Items.Add("FiniteDifference");
                AdvField1ComboBox.SelectedIndex = 0;

                AdvField2Label.Text = "Learning Rate";
                AdvField2TextBox.Text = "0.01";
                AdvField3Label.Text = "Epochs";
                AdvField3TextBox.Text = "30";
                AdvField4Label.Text = "Ansatz Reps";
                AdvField4TextBox.Text = "2";
            }
            else if (upper.Contains("VQC"))
            {
                ModelDescriptionText.Text = "Variational quantum classifier minimizing objective loss loop over parameters.";
                DynamicField1Label.Text = "Ansatz Circuit";
                DynamicField1ComboBox.Items.Add("RealAmplitudes");
                DynamicField1ComboBox.Items.Add("TwoLocal");
                DynamicField1ComboBox.Items.Add("EfficientSU2");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Optimizer";
                DynamicField2ComboBox.Items.Add("COBYLA");
                DynamicField2ComboBox.Items.Add("Adam");
                DynamicField2ComboBox.Items.Add("SPSA");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Visible;
                AdvField1Label.Text = "Loss Function";
                AdvField1ComboBox.Items.Add("CrossEntropy");
                AdvField1ComboBox.Items.Add("SquaredError");
                AdvField1ComboBox.SelectedIndex = 0;

                AdvField2Label.Text = "Max Iterations";
                AdvField2TextBox.Text = "100";
                AdvField3Label.Text = "Epochs";
                AdvField3TextBox.Text = "50";
                AdvField4Label.Text = "Ansatz Reps";
                AdvField4TextBox.Text = "2";
            }
            else if (upper.Contains("QCNN"))
            {
                ModelDescriptionText.Text = "Hierarchical quantum convolution and reduction pooling layers for feature extraction.";
                DynamicField1Label.Text = "Convolution Ansatz";
                DynamicField1ComboBox.Items.Add("TwoLocal");
                DynamicField1ComboBox.Items.Add("RealAmplitudes");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Pooling Ansatz";
                DynamicField2ComboBox.Items.Add("Controlled-RY");
                DynamicField2ComboBox.Items.Add("Controlled-RZ");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Visible;
                AdvField1Label.Text = "Conv Layers";
                AdvField1ComboBox.Items.Add("2");
                AdvField1ComboBox.Items.Add("1");
                AdvField1ComboBox.SelectedIndex = 0;

                AdvField2Label.Text = "Pool Layers";
                AdvField2TextBox.Text = "2";
                AdvField3Label.Text = "Filter Size";
                AdvField3TextBox.Text = "2";
                AdvField4Label.Text = "Stride";
                AdvField4TextBox.Text = "2";
            }
            else if (upper.Contains("QKNN"))
            {
                ModelDescriptionText.Text = "Quantum state overlap computes similarity matrix for k-nearest neighbors majority voting.";
                DynamicField1Label.Text = "Quantum Distance";
                DynamicField1ComboBox.Items.Add("Fidelity Distance");
                DynamicField1ComboBox.Items.Add("State Overlap");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "k Neighbors";
                DynamicField2ComboBox.Items.Add("3");
                DynamicField2ComboBox.Items.Add("5");
                DynamicField2ComboBox.Items.Add("7");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
            else // Quantum Kernel
            {
                ModelDescriptionText.Text = "Computes explicit quantum kernel matrix K(X, X) for downstream machine learning algorithms.";
                DynamicField1Label.Text = "Kernel Type";
                DynamicField1ComboBox.Items.Add("FidelityQuantumKernel");
                DynamicField1ComboBox.Items.Add("ProjectedQuantumKernel");
                DynamicField1ComboBox.Items.Add("CosineQuantumKernel");
                DynamicField1ComboBox.SelectedIndex = 0;

                DynamicField2Label.Text = "Downstream Algorithm";
                DynamicField2ComboBox.Items.Add("SVM");
                DynamicField2ComboBox.Items.Add("Kernel Ridge");
                DynamicField2ComboBox.Items.Add("Kernel PCA");
                DynamicField2ComboBox.Items.Add("Similarity Analysis");
                DynamicField2ComboBox.SelectedIndex = 0;

                AdvancedDynamicRowGrid.Visibility = Visibility.Collapsed;
            }
        }

        private async void TrainModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_datasetPath) || !File.Exists(_datasetPath))
            {
                TrainingStatusBadge.Text = "Please upload a dataset on Data Upload page first.";
                return;
            }

            string modelType = (ModelTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "QSVM";
            string targetCol = TargetColumnComboBox.SelectedItem?.ToString() ?? "target";
            string featureMap = (FeatureMapComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "ZZFeatureMap";
            string ansatzOrKernel = DynamicField1ComboBox.SelectedItem?.ToString() ?? "FidelityQuantumKernel";

            int.TryParse(QubitsTextBox.Text, out int qubits);
            if (qubits <= 0) qubits = 4;

            int.TryParse(ShotsTextBox.Text, out int shots);
            if (shots <= 0) shots = 1024;

            TrainingStatusBadge.Text = $"Training {modelType} targeting '{targetCol}' via Python + Rust Engine...";
            TrainModelButton.IsEnabled = false;
            ResultsDashboard.Visibility = Visibility.Visible;
            TrainingLoadingContainer.Visibility = Visibility.Visible;
            TrainingLoadingContainer.IsActive = true;
            TrainingChartImage.Source = null;

            QmlTrainingResultModel result = null!;
            await Task.Run(() =>
            {
                result = DatasetBackend.TrainQmlModel(_datasetPath, targetCol, modelType, qubits, shots, featureMap, ansatzOrKernel, "COBYLA");
            });

            TrainModelButton.IsEnabled = true;
            TrainingLoadingContainer.IsActive = false;
            TrainingLoadingContainer.Visibility = Visibility.Collapsed;

            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                TrainingStatusBadge.Text = $"Training error: {result?.Error ?? "Backend error"}";
                return;
            }

            // Bind Performance Metrics
            TrainAccuracyText.Text = $"{result.TrainAccuracy}%";
            TestAccuracyText.Text = $"{result.TestAccuracy}%";
            TestAccuracySubtitle.Text = $"Evaluated on {result.TestSamplesCount} test samples ({result.TrainSamplesCount} train | {result.TotalSamplesCount} total rows)";
            F1ScoreText.Text = $"{result.F1Score}% / {result.RocAuc:F3}";
            TrainTimeText.Text = $"{result.TrainTimeSeconds}s";
            bool isKnn = modelType.ToUpper().Contains("KNN");
            LossFinalText.Text = isKnn ? "Distance Search Complete" : $"Convergence Loss: {result.LossFinal:F3}";

            // Bind Architecture Side Panel Info
            InfoLearningType.Text = result.LearningType;
            InfoQuantumComponent.Text = result.QuantumComponent;
            InfoClassicalComponent.Text = result.ClassicalComponent;
            InfoTrainableParams.Text = result.TrainableParameters.ToString();
            InfoCircuitDepth.Text = result.CircuitDepth.ToString();
            InfoGateCount.Text = result.GateCount.ToString();
            InfoPipelineText.Text = $"Features → {result.QuantumComponent} → {result.ClassicalComponent} → Prediction";

            // Bind Visualization Chart Image
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
                TrainingChartImage.Source = bitmap;
            }

            // Save Model Artifact to Output Workspace Directory
            string savedModelPath = OutputDirectoryManager.SaveModelArtifact(modelType.ToLower(), result);
            string modelFileName = !string.IsNullOrEmpty(savedModelPath) ? Path.GetFileName(savedModelPath) : "models folder";

            TrainingStatusBadge.Text = $"Model Training Completed on Target '{targetCol}'! Saved to {modelFileName}";
        }

        private void ViewQuantumCircuitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame?.Navigate(typeof(pages.VisualizationPage));
        }

        private void GoToPredictionButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame?.Navigate(typeof(pages.PredictionPage));
        }
    }
}
