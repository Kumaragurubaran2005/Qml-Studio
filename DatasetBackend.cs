using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QML_Studio
{
    public class ColumnSummaryModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("data_type")]
        public string DataType { get; set; } = string.Empty;

        [JsonPropertyName("non_null_count")]
        public int NonNullCount { get; set; }

        [JsonPropertyName("null_count")]
        public int NullCount { get; set; }

        [JsonPropertyName("null_percentage")]
        public double NullPercentage { get; set; }

        [JsonPropertyName("unique_count")]
        public int UniqueCount { get; set; }

        [JsonPropertyName("sample_val")]
        public string SampleVal { get; set; } = string.Empty;
    }

    public class DatasetAnalysisResultModel
    {
        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("total_rows")]
        public int TotalRows { get; set; }

        [JsonPropertyName("total_columns")]
        public int TotalColumns { get; set; }

        [JsonPropertyName("total_nulls")]
        public int TotalNulls { get; set; }

        [JsonPropertyName("numeric_cols_count")]
        public int NumericColsCount { get; set; }

        [JsonPropertyName("categorical_cols_count")]
        public int CategoricalColsCount { get; set; }

        [JsonPropertyName("columns")]
        public List<ColumnSummaryModel> Columns { get; set; } = new();

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class CleanDatasetResultModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("original_file")]
        public string OriginalFile { get; set; } = string.Empty;

        [JsonPropertyName("cleaned_file")]
        public string CleanedFile { get; set; } = string.Empty;

        [JsonPropertyName("original_rows")]
        public int OriginalRows { get; set; }

        [JsonPropertyName("original_cols")]
        public int OriginalCols { get; set; }

        [JsonPropertyName("cleaned_cols")]
        public int CleanedCols { get; set; }

        [JsonPropertyName("removed_low_variance_cols")]
        public List<string> RemovedLowVarianceCols { get; set; } = new();

        [JsonPropertyName("scaled_numeric_cols")]
        public List<string> ScaledNumericCols { get; set; } = new();

        [JsonPropertyName("encoded_categorical_cols")]
        public List<string> EncodedCategoricalCols { get; set; } = new();

        [JsonPropertyName("nulls_imputed_count")]
        public int NullsImputedCount { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class PlotResultModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("x_col")]
        public string XCol { get; set; } = string.Empty;

        [JsonPropertyName("y_col")]
        public string YCol { get; set; } = string.Empty;

        [JsonPropertyName("plot_type")]
        public string PlotType { get; set; } = string.Empty;

        [JsonPropertyName("image_base64")]
        public string ImageBase64 { get; set; } = string.Empty;

        [JsonPropertyName("pdf_base64")]
        public string PdfBase64 { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class HybridCrossStudyResultModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("qml_model")]
        public string QmlModel { get; set; } = string.Empty;

        [JsonPropertyName("classical_model")]
        public string ClassicalModel { get; set; } = string.Empty;

        [JsonPropertyName("qubits")]
        public int Qubits { get; set; }

        [JsonPropertyName("shots")]
        public int Shots { get; set; }

        [JsonPropertyName("samples_count")]
        public int SamplesCount { get; set; }

        [JsonPropertyName("qml_accuracy")]
        public double QmlAccuracy { get; set; }

        [JsonPropertyName("qml_precision")]
        public double QmlPrecision { get; set; }

        [JsonPropertyName("qml_recall")]
        public double QmlRecall { get; set; }

        [JsonPropertyName("qml_f1")]
        public double QmlF1 { get; set; }

        [JsonPropertyName("qml_time_seconds")]
        public double QmlTimeSeconds { get; set; }

        [JsonPropertyName("qml_parameters")]
        public int QmlParameters { get; set; }

        [JsonPropertyName("classical_accuracy")]
        public double ClassicalAccuracy { get; set; }

        [JsonPropertyName("classical_precision")]
        public double ClassicalPrecision { get; set; }

        [JsonPropertyName("classical_recall")]
        public double ClassicalRecall { get; set; }

        [JsonPropertyName("classical_f1")]
        public double ClassicalF1 { get; set; }

        [JsonPropertyName("classical_time_seconds")]
        public double ClassicalTimeSeconds { get; set; }

        [JsonPropertyName("classical_parameters")]
        public int ClassicalParameters { get; set; }

        [JsonPropertyName("accuracy_improvement")]
        public double AccuracyImprovement { get; set; }

        [JsonPropertyName("parameter_reduction_pct")]
        public double ParameterReductionPct { get; set; }

        [JsonPropertyName("chart_image_base64")]
        public string ChartImageBase64 { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class QmlTrainingResultModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("model_type")]
        public string ModelType { get; set; } = string.Empty;

        [JsonPropertyName("target_column")]
        public string TargetColumn { get; set; } = string.Empty;

        [JsonPropertyName("qubits")]
        public int Qubits { get; set; }

        [JsonPropertyName("shots")]
        public int Shots { get; set; }

        [JsonPropertyName("feature_map")]
        public string FeatureMap { get; set; } = string.Empty;

        [JsonPropertyName("ansatz")]
        public string Ansatz { get; set; } = string.Empty;

        [JsonPropertyName("optimizer")]
        public string Optimizer { get; set; } = string.Empty;

        [JsonPropertyName("learning_type")]
        public string LearningType { get; set; } = string.Empty;

        [JsonPropertyName("quantum_component")]
        public string QuantumComponent { get; set; } = string.Empty;

        [JsonPropertyName("classical_component")]
        public string ClassicalComponent { get; set; } = string.Empty;

        [JsonPropertyName("trainable_parameters")]
        public int TrainableParameters { get; set; }

        [JsonPropertyName("circuit_depth")]
        public int CircuitDepth { get; set; }

        [JsonPropertyName("gate_count")]
        public int GateCount { get; set; }

        [JsonPropertyName("total_samples_count")]
        public int TotalSamplesCount { get; set; }

        [JsonPropertyName("train_samples_count")]
        public int TrainSamplesCount { get; set; }

        [JsonPropertyName("test_samples_count")]
        public int TestSamplesCount { get; set; }

        [JsonPropertyName("train_accuracy")]
        public double TrainAccuracy { get; set; }

        [JsonPropertyName("test_accuracy")]
        public double TestAccuracy { get; set; }

        [JsonPropertyName("precision")]
        public double Precision { get; set; }

        [JsonPropertyName("recall")]
        public double Recall { get; set; }

        [JsonPropertyName("f1_score")]
        public double F1Score { get; set; }

        [JsonPropertyName("roc_auc")]
        public double RocAuc { get; set; }

        [JsonPropertyName("train_time_seconds")]
        public double TrainTimeSeconds { get; set; }

        [JsonPropertyName("loss_final")]
        public double LossFinal { get; set; }

        [JsonPropertyName("neighbors_info")]
        public List<string> NeighborsInfo { get; set; } = new();

        [JsonPropertyName("query_prediction")]
        public string QueryPrediction { get; set; } = string.Empty;

        [JsonPropertyName("chart_image_base64")]
        public string ChartImageBase64 { get; set; } = string.Empty;

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class PredictionResultModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("predicted_class_id")]
        public int PredictedClassId { get; set; }

        [JsonPropertyName("predicted_class_label")]
        public string PredictedClassLabel { get; set; } = string.Empty;

        [JsonPropertyName("confidence_percentage")]
        public double ConfidencePercentage { get; set; }

        [JsonPropertyName("top_contributing_features")]
        public string TopContributingFeatures { get; set; } = string.Empty;

        [JsonPropertyName("feature_count")]
        public int FeatureCount { get; set; }

        [JsonPropertyName("decision_score")]
        public double DecisionScore { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class BatchRowPreviewModel
    {
        [JsonPropertyName("row_index")]
        public int RowIndex { get; set; }

        [JsonPropertyName("predicted_target")]
        public string PredictedTarget { get; set; } = string.Empty;

        [JsonPropertyName("confidence_pct")]
        public double ConfidencePct { get; set; }
    }

    public class BatchPredictionResultModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("total_rows")]
        public int TotalRows { get; set; }

        [JsonPropertyName("output_csv_path")]
        public string OutputCsvPath { get; set; } = string.Empty;

        [JsonPropertyName("predictions_preview")]
        public List<BatchRowPreviewModel> PredictionsPreview { get; set; } = new();

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public static class DatasetBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr analyze_dataset(string path);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr clean_dataset(string path);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr generate_dataset_plot(string path, string xCol, string yCol, string plotType);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_hybrid_cross_study(string path, string qmlModel, string classicalModel, int qubits, int shots);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr train_qml_model(string path, string targetCol, string model, int qubits, int shots, string featureMap, string ansatz, string optimizer);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr predict_sample_ffi(string path, string featuresJson);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr predict_batch_ffi(string path, string testCsvPath);

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        private static string ReadUtf8String(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return "{}";
            try
            {
                return Marshal.PtrToStringUTF8(ptr) ?? Marshal.PtrToStringAnsi(ptr) ?? "{}";
            }
            catch
            {
                return Marshal.PtrToStringAnsi(ptr) ?? "{}";
            }
        }

        public static DatasetAnalysisResultModel AnalyzeDataset(string filePath)
        {
            IntPtr ptr = analyze_dataset(filePath);
            if (ptr == IntPtr.Zero) return new DatasetAnalysisResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<DatasetAnalysisResultModel>(json) ?? new DatasetAnalysisResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new DatasetAnalysisResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }

        public static CleanDatasetResultModel CleanDataset(string filePath)
        {
            IntPtr ptr = clean_dataset(filePath);
            if (ptr == IntPtr.Zero) return new CleanDatasetResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<CleanDatasetResultModel>(json) ?? new CleanDatasetResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new CleanDatasetResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }

        public static PlotResultModel GeneratePlot(string filePath, string xCol, string yCol, string plotType)
        {
            IntPtr ptr = generate_dataset_plot(filePath, xCol, yCol, plotType);
            if (ptr == IntPtr.Zero) return new PlotResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<PlotResultModel>(json) ?? new PlotResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new PlotResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }

        public static HybridCrossStudyResultModel RunHybridCrossStudy(string filePath, string qmlModel, string classicalModel, int qubits, int shots)
        {
            IntPtr ptr = run_hybrid_cross_study(filePath, qmlModel, classicalModel, qubits, shots);
            if (ptr == IntPtr.Zero) return new HybridCrossStudyResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<HybridCrossStudyResultModel>(json) ?? new HybridCrossStudyResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new HybridCrossStudyResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }

        public static QmlTrainingResultModel TrainQmlModel(string filePath, string targetCol, string model, int qubits, int shots, string featureMap, string ansatz, string optimizer)
        {
            IntPtr ptr = train_qml_model(filePath, targetCol, model, qubits, shots, featureMap, ansatz, optimizer);
            if (ptr == IntPtr.Zero) return new QmlTrainingResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<QmlTrainingResultModel>(json) ?? new QmlTrainingResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new QmlTrainingResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }

        public static PredictionResultModel PredictSample(string filePath, string featuresJson)
        {
            IntPtr ptr = predict_sample_ffi(filePath, featuresJson);
            if (ptr == IntPtr.Zero) return new PredictionResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<PredictionResultModel>(json) ?? new PredictionResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new PredictionResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }

        public static BatchPredictionResultModel PredictBatch(string filePath, string testCsvPath)
        {
            IntPtr ptr = predict_batch_ffi(filePath, testCsvPath);
            if (ptr == IntPtr.Zero) return new BatchPredictionResultModel { Error = "Failed to call Rust backend" };
            try
            {
                string json = ReadUtf8String(ptr);
                return JsonSerializer.Deserialize<BatchPredictionResultModel>(json) ?? new BatchPredictionResultModel { Error = "Invalid JSON" };
            }
            catch (Exception ex) { return new BatchPredictionResultModel { Error = ex.Message }; }
            finally { free_string(ptr); }
        }
    }
}
