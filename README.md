# QML Studio

**QML Studio** is a Quantum Machine Learning (QML) desktop application built with **WinUI 3 (.NET 8)**, a native **Rust (FFI)** backend engine, and a **Python / Qiskit** compute pipeline. It provides a visual workspace to upload datasets, preprocess data, train quantum and classical machine learning models, run single/batch predictions, generate visualizations, and conduct hybrid cross-study benchmarks.

---

## 🌟 Key Features

- **Modern WinUI 3 Desktop Interface**: High-performance, dark-themed Windows 10/11 user interface.
- **Quantum Machine Learning (QML) Suite**:
  - **Variational Quantum Classifier (VQC)**: Custom feature maps, variational ansatz layers (Ry rotations, CNOT entanglers), and optimizers (COBYLA, SPSA, Adam).
  - **Quantum Neural Network (QNN)**: Parameterized quantum circuits configured for classification tasks.
  - **Quantum Support Vector Machine (QSVM)**: Quantum kernel-based classification.
  - **Quantum K-Nearest Neighbors (QKNN)**: Quantum state distance and fidelity estimation.
  - **Quantum Kernels**: Quantum feature maps for compute fidelity matrices.
- **Classical Machine Learning Models**:
  - Classical baselines including Support Vector Machines (SVM), K-Nearest Neighbors (KNN), Multi-Layer Perceptrons (MLP / Neural Networks), and Logistic Regression.
- **Hybrid Cross-Study Benchmarking**:
  - Compare QML models directly against classical machine learning counterparts.
  - Measure Accuracy, Precision, Recall, F1-Score, Execution Time (seconds), Parameter Efficiency, and Percentage Improvement.
- **Dataset Analysis & Cleaning**:
  - Auto-detection of numeric vs. categorical columns, null percentages, sample values, and unique counts.
  - Automated data cleaning (imputation, scaling, categorical encoding, and low-variance column drop).
- **Data Visualization**:
  - Generate scatter plots, bar charts, histograms, and line plots with base64/image preview output.
- **Inference Pipeline**:
  - Single-sample parameter predictions and full CSV batch predictions.

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    WinUI 3 / C# UI                      │
│ (DataUploadPage, ModelSelectionPage, Experiments, etc.) │
└────────────────────────────┬────────────────────────────┘
                             │ P/Invoke (C FFI)
                             ▼
┌─────────────────────────────────────────────────────────┐
│               Rust Native Core (backend_qml)            │
│  - Memory-safe CSV parsing & dataset analysis           │
│  - C-FFI String Memory Management                       │
│  - Python Subprocess Orchestration                      │
└────────────────────────────┬────────────────────────────┘
                             │ std::process::Command ("python")
                             ▼
┌─────────────────────────────────────────────────────────┐
│               Python Compute & QML Engine               │
│  - Qiskit & Qiskit Aer (Quantum Circuit Execution)      │
│  - Scikit-Learn, SciPy, NumPy, Pandas (Data & ML)       │
│  - Matplotlib (Plot & Chart Generation)                 │
└─────────────────────────────────────────────────────────┘
```

---

## 📁 Repository Structure

```
QML Studio/
├── App.xaml / App.xaml.cs          # Application entry point & window management
├── MainWindow.xaml / cs            # Shell navigation frame & primary UI layout
├── pages/                          # WinUI 3 Navigation Pages
│   ├── DataUploadPage.xaml / cs    # Dataset loading & statistical summaries
│   ├── ModelSelectionPage.xaml / cs# QML model configuration & training UI
│   ├── ClassicalModelPage.xaml / cs# Classical ML model training UI
│   ├── ExperimentsPage.xaml / cs   # Hybrid benchmarking workspace
│   ├── VisualizationPage.xaml / cs # Data plotting & visual analytics
│   └── PredictionPage.xaml / cs    # Single sample & batch CSV prediction
├── ClassicalBackend.cs             # C# wrapper for Classical ML tasks
├── DatasetBackend.cs               # C# wrapper for Rust P/Invoke FFI bindings
├── OutputDirectoryManager.cs       # Export & artifact file management
├── backend_qml/                    # Rust backend crate
│   ├── Cargo.toml                  # Rust dependencies (serde, csv, serde_json)
│   └── src/lib.rs                  # Native C FFI functions (`backend_qml.dll`)
├── qml_trainer.py                  # Python script for QML model training & evaluation
├── qml_inference.py                # Python script for single & batch predictions
├── hybrid_benchmark.py             # Python script for QML vs Classical benchmarking
├── data_processor.py               # Python script for data cleaning & plot generation
├── vqc.py                          # Qiskit VQC circuit construction & optimization
├── qnn.py                          # Qiskit QNN model implementation
├── qsvm.py                         # Qiskit Quantum SVM & kernel evaluation
├── qknn.py                         # Quantum K-NN implementation
├── quantum_kernel.py               # Quantum feature map & kernel matrix calculator
└── classical_models.py             # Scikit-learn classical model baselines
```

---

## 🛠️ Prerequisites

### 1. Windows Operating System
- **Windows 10** (version 1809 / Build 17763 or higher) or **Windows 11**.

### 2. .NET 8 & Windows App SDK
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (v17.8 or higher) with workload:
  - `.NET Desktop Development`
  - `Universal Windows Platform development` / `Windows App SDK C# templates`

### 3. Python 3.10+ & Required Libraries
Ensure Python 3.10+ is installed and accessible via `python` in your `PATH`. Install the required Python packages:

```bash
pip install numpy scipy pandas scikit-learn qiskit qiskit-aer matplotlib openpyxl
```

### 4. Rust Toolchain (Optional for editing Rust core)
- [Rust toolchain](https://rustup.rs/) (cargo, rustc) if you plan to recompile `backend_qml.dll`.

---

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/Kumaragurubaran2005/Qml-Studio.git
cd "QML Studio/QML Studio"
```

### 2. Build the Rust Backend DLL (Optional)
If you make changes inside the `backend_qml` directory, build the release DLL and copy it to the root directory:

```bash
cd backend_qml
cargo build --release
copy target\release\backend_qml.dll ..\backend_qml.dll
cd ..
```

### 3. Build & Run the WinUI 3 Application
Using Visual Studio 2022:
1. Open `QML Studio.csproj` or the solution file.
2. Select target platform `x64` (or `x86`/`ARM64`).
3. Press **F5** to build and launch.

Or via .NET CLI:
```bash
dotnet build "QML Studio.csproj" -c Debug
dotnet run --project "QML Studio.csproj"
```

---

## 🔬 Usage Workflow

1. **Upload Dataset**: Navigate to **Data Upload**, browse for a CSV/Excel file, and inspect the structural breakdown (rows, columns, data types, nulls).
2. **Preprocess & Clean**: Run data cleaning to impute missing values, scale numeric columns, and encode categorical variables.
3. **Train QML Model**: Go to **Model Selection**, configure qubits, shots, feature maps, ansatz layers, and optimizers, then run training.
4. **Benchmark**: Use **Experiments** to compare quantum model results against classical algorithms like SVM or Neural Networks.
5. **Visualize Results**: Generate and view correlation plots, loss curves, and decision metric comparisons.
6. **Predict**: Load a saved model under **Prediction** to classify individual input samples or perform batch CSV inference.

---

## 📜 License

This project is open-source and available under the MIT License.
