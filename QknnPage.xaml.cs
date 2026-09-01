using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Text.Json;

namespace QML_Studio
{
    public sealed partial class QknnPage : Page
    {
        private string? _selectedCsvPath;

        public QknnPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            // Need the MainWindow handle for the picker
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedCsvPath = file.Path;
                SelectedFileTextBlock.Text = file.Name;
            }
        }

        private void PredictButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultTextBlock.Text = "Running...";

                if (string.IsNullOrEmpty(_selectedCsvPath) || !File.Exists(_selectedCsvPath))
                {
                    ResultTextBlock.Text = "Error: Please select a valid CSV file first.";
                    return;
                }

                string targetColumn = TargetColumnTextBox.Text.Trim();
                if (string.IsNullOrEmpty(targetColumn))
                {
                    ResultTextBlock.Text = "Error: Please specify a target column name.";
                    return;
                }

                if (!int.TryParse(KTextBox.Text, out int k))
                {
                    ResultTextBlock.Text = "Error: Invalid K value.";
                    return;
                }

                if (!int.TryParse(ShotsTextBox.Text, out int shots))
                {
                    ResultTextBlock.Text = "Error: Invalid shots value.";
                    return;
                }
                
                string[] queryStrings = QueryVectorTextBox.Text.Split(',');
                List<double> queryVector = new List<double>();
                foreach(var qStr in queryStrings)
                {
                    if (double.TryParse(qStr.Trim(), out double val))
                    {
                        queryVector.Add(val);
                    }
                    else
                    {
                        ResultTextBlock.Text = "Error: Invalid query vector format.";
                        return;
                    }
                }

                // Read and Parse CSV
                var lines = File.ReadAllLines(_selectedCsvPath);
                if (lines.Length < 2)
                {
                    ResultTextBlock.Text = "Error: CSV must have a header and at least one data row.";
                    return;
                }

                var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();
                int targetIndex = headers.IndexOf(targetColumn);
                
                if (targetIndex == -1)
                {
                    ResultTextBlock.Text = $"Error: Target column '{targetColumn}' not found in CSV headers.";
                    return;
                }

                List<List<double>> trainData = new List<List<double>>();
                List<int> trainLabels = new List<int>();
                Dictionary<string, int> labelEncoder = new Dictionary<string, int>();
                Dictionary<int, string> labelDecoder = new Dictionary<int, string>();
                int labelCounter = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    
                    var parts = lines[i].Split(',');
                    if (parts.Length != headers.Count) continue;

                    List<double> rowFeatures = new List<double>();
                    for (int j = 0; j < parts.Length; j++)
                    {
                        if (j == targetIndex)
                        {
                            string targetValue = parts[j].Trim();
                            if (int.TryParse(targetValue, out int label))
                            {
                                trainLabels.Add(label);
                            }
                            else if (double.TryParse(targetValue, out double dLabel))
                            {
                                trainLabels.Add((int)dLabel);
                            }
                            else
                            {
                                if (!labelEncoder.ContainsKey(targetValue))
                                {
                                    labelEncoder[targetValue] = labelCounter;
                                    labelDecoder[labelCounter] = targetValue;
                                    labelCounter++;
                                }
                                trainLabels.Add(labelEncoder[targetValue]);
                            }
                        }
                        else
                        {
                            if (double.TryParse(parts[j].Trim(), out double feature))
                            {
                                rowFeatures.Add(feature);
                            }
                            else
                            {
                                throw new Exception($"Invalid feature value at row {i+1}, col {j+1}");
                            }
                        }
                    }
                    trainData.Add(rowFeatures);
                }

                string trainDataPath = Path.GetTempFileName();
                string trainLabelsPath = Path.GetTempFileName();
                string queryPath = Path.GetTempFileName();

                try
                {
                    File.WriteAllLines(trainDataPath, trainData.Select(row => string.Join(",", row)));
                    File.WriteAllLines(trainLabelsPath, trainLabels.Select(label => label.ToString()));
                    File.WriteAllText(queryPath, string.Join(",", queryVector));

                    string result = QknnBackend.RunQknnPaths(trainDataPath, trainLabelsPath, queryPath, k, shots);

                    // Save QKNN Model Artifact to Output Workspace
                    string savedModelPath = OutputDirectoryManager.SaveModelArtifact("qknn", result);
                    string saveNote = !string.IsNullOrEmpty(savedModelPath) ? $" (Saved to {Path.GetFileName(savedModelPath)})" : "";

                    if (labelEncoder.Count > 0)
                    {
                        string mappingInfo = string.Join(", ", labelEncoder.Select(kv => $"'{kv.Key}'={kv.Value}"));
                        ResultTextBlock.Text = $"Label Mappings: {mappingInfo}\n\nResult{saveNote}:\n{result}";
                    }
                    else
                    {
                        ResultTextBlock.Text = $"Result{saveNote}:\n{result}";
                    }
                }
                finally
                {
                    if (File.Exists(trainDataPath)) File.Delete(trainDataPath);
                    if (File.Exists(trainLabelsPath)) File.Delete(trainLabelsPath);
                    if (File.Exists(queryPath)) File.Delete(queryPath);
                }
            }
            catch (Exception ex)
            {
                ResultTextBlock.Text = $"Error: {ex.Message}";
            }
        }
    }
}
