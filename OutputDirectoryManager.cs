using System;
using System.IO;
using System.Text.Json;

namespace QML_Studio
{
    public static class OutputDirectoryManager
    {
        public static string CurrentProjectFolder { get; private set; } = string.Empty;
        public static string DatasetFolder { get; private set; } = string.Empty;
        public static string VisualizationsFolder { get; private set; } = string.Empty;
        public static string ModelsFolder { get; private set; } = string.Empty;
        public static string ReportsFolder { get; private set; } = string.Empty;

        public static string EnsureFolderInitialized(string fallbackPath = "")
        {
            if (!string.IsNullOrEmpty(CurrentProjectFolder) && Directory.Exists(ModelsFolder))
            {
                return CurrentProjectFolder;
            }

            string activePath = !string.IsNullOrEmpty(fallbackPath) ? fallbackPath : pages.DataUploadPage.CurrentDatasetPath;
            if (string.IsNullOrEmpty(activePath))
            {
                activePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dataset.csv");
            }

            return InitializeProjectFolder(activePath);
        }

        public static string InitializeProjectFolder(string inputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                inputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dataset.csv");
            }

            string fileName = Path.GetFileNameWithoutExtension(inputFilePath);
            if (string.IsNullOrEmpty(fileName)) fileName = "Dataset";

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string folderName = $"QML_Studio_Output_{fileName}_{timestamp}";

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseProjectsDir = Path.Combine(documentsPath, "QML_Studio_Projects");
            CurrentProjectFolder = Path.Combine(baseProjectsDir, folderName);

            DatasetFolder = Path.Combine(CurrentProjectFolder, "dataset");
            VisualizationsFolder = Path.Combine(CurrentProjectFolder, "visualizations");
            ModelsFolder = Path.Combine(CurrentProjectFolder, "models");
            ReportsFolder = Path.Combine(CurrentProjectFolder, "reports");

            Directory.CreateDirectory(DatasetFolder);
            Directory.CreateDirectory(VisualizationsFolder);
            Directory.CreateDirectory(ModelsFolder);
            Directory.CreateDirectory(ReportsFolder);

            try
            {
                if (File.Exists(inputFilePath))
                {
                    string destRawPath = Path.Combine(DatasetFolder, "raw_input.csv");
                    File.Copy(inputFilePath, destRawPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy raw input dataset: {ex.Message}");
            }

            return CurrentProjectFolder;
        }

        public static string SaveCleanedDataset(string cleanedSourcePath)
        {
            EnsureFolderInitialized(cleanedSourcePath);

            try
            {
                string destCleanedPath = Path.Combine(DatasetFolder, "cleaned_dataset.csv");
                if (File.Exists(cleanedSourcePath))
                {
                    File.Copy(cleanedSourcePath, destCleanedPath, overwrite: true);
                    return destCleanedPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save cleaned dataset: {ex.Message}");
            }

            return string.Empty;
        }

        public static string SaveVisualizationImage(byte[] imageBytes, string plotType)
        {
            EnsureFolderInitialized();

            try
            {
                string sanitizedPlotName = plotType.Replace(" ", "_").Replace("/", "_");
                string timestamp = DateTime.Now.ToString("HHmmss");
                string pngPath = Path.Combine(VisualizationsFolder, $"{sanitizedPlotName}_{timestamp}.png");

                File.WriteAllBytes(pngPath, imageBytes);
                return pngPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save visualization PNG: {ex.Message}");
            }

            return string.Empty;
        }

        public static string SaveModelArtifact(string modelName, object modelResultData, byte[]? binaryWeights = null)
        {
            EnsureFolderInitialized();

            try
            {
                string sanitizedModelName = modelName.Replace(" ", "_").ToLower();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                // 1. Save JSON Parameter & Metric Metadata
                string jsonPath = Path.Combine(ModelsFolder, $"{sanitizedModelName}_{timestamp}.json");
                string jsonContent = modelResultData is string strData ? strData : JsonSerializer.Serialize(modelResultData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, jsonContent);

                // 2. Save Binary Weights / Model Weights File (.bin)
                string binPath = Path.Combine(ModelsFolder, $"{sanitizedModelName}_{timestamp}.bin");
                if (binaryWeights != null && binaryWeights.Length > 0)
                {
                    File.WriteAllBytes(binPath, binaryWeights);
                }
                else
                {
                    // Generate binary payload from json bytes for portable model loading
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                    File.WriteAllBytes(binPath, bytes);
                }

                return binPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save model artifact: {ex.Message}");
            }

            return string.Empty;
        }

        public static string GetModelsFolder()
        {
            EnsureFolderInitialized();
            return ModelsFolder;
        }

        public static string SavePredictionOutput(string csvSourcePath)
        {
            EnsureFolderInitialized(csvSourcePath);

            try
            {
                string timestamp = DateTime.Now.ToString("HHmmss");
                string destPath = Path.Combine(ModelsFolder, $"batch_predictions_{timestamp}.csv");
                if (File.Exists(csvSourcePath))
                {
                    File.Copy(csvSourcePath, destPath, overwrite: true);
                    return destPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save batch prediction output: {ex.Message}");
            }

            return csvSourcePath;
        }
    }
}
