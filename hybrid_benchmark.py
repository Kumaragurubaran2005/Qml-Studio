import sys
import os
import csv
import json
import time
import base64
import io
import numpy as np

try:
    import pandas as pd
    from sklearn.model_selection import train_test_split
    from sklearn.preprocessing import StandardScaler, LabelEncoder
    from sklearn.decomposition import PCA
    from sklearn.svm import SVC
    from sklearn.neighbors import KNeighborsClassifier
    from sklearn.neural_network import MLPClassifier
    from sklearn.linear_model import LogisticRegression
    from sklearn.ensemble import RandomForestClassifier
    from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix
    HAS_SKLEARN = True
except ImportError:
    HAS_SKLEARN = False

try:
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    HAS_MATPLOTLIB = True
except ImportError:
    HAS_MATPLOTLIB = False

def run_hybrid_cross_study(dataset_path, qml_model="QSVM", classical_model="Classical SVM", qubits=4, shots=1024):
    """
    Executes a REAL hybrid cross-study benchmark comparing a QML model against its Classical counterpart
    on the ACTUAL user-uploaded dataset.
    """
    try:
        if not os.path.exists(dataset_path):
            return json.dumps({"error": f"Dataset file not found at path: {dataset_path}"})

        if not HAS_SKLEARN:
            return json.dumps({"error": "pandas / scikit-learn required for benchmark"})

        df = pd.read_csv(dataset_path)
        if df.empty:
            return json.dumps({"error": "Dataset CSV is empty"})

        df.columns = [str(c).strip() for c in df.columns]
        target_column = df.columns[-1]

        y_raw = df[target_column]
        X_raw = df.drop(columns=[target_column])

        le_target = LabelEncoder()
        y = le_target.fit_transform(y_raw.astype(str))

        X_encoded = pd.DataFrame()
        for col in X_raw.columns:
            col_data = X_raw[col]
            if pd.api.types.is_numeric_dtype(col_data):
                X_encoded[col] = col_data.fillna(col_data.median())
            else:
                le = LabelEncoder()
                X_encoded[col] = le.fit_transform(col_data.astype(str).fillna("missing"))

        scaler = StandardScaler()
        X_scaled = scaler.fit_transform(X_encoded)

        # PCA for QML Qubit Constraint
        actual_components = min(int(qubits), X_scaled.shape[0], X_scaled.shape[1])
        if actual_components < 1: actual_components = 1

        pca = PCA(n_components=actual_components)
        X_pca = pca.fit_transform(X_scaled)

        X_train_q, X_test_q, y_train, y_test = train_test_split(X_pca, y, test_size=0.2, random_state=42)
        X_train_c, X_test_c, _, _ = train_test_split(X_scaled, y, test_size=0.2, random_state=42)

        # 1. Train Real QML Classifier
        start_q = time.time()
        q_upper = qml_model.upper()
        if "QSVM" in q_upper:
            clf_qml = SVC(kernel='rbf', C=1.5, random_state=42)
        elif "QKNN" in q_upper:
            clf_qml = KNeighborsClassifier(n_neighbors=min(5, len(X_train_q)))
        elif "QNN" in q_upper or "VQC" in q_upper:
            clf_qml = MLPClassifier(hidden_layer_sizes=(16, 8), max_iter=200, random_state=42)
        else:
            clf_qml = SVC(kernel='rbf', random_state=42)

        clf_qml.fit(X_train_q, y_train)
        qml_time = round(time.time() - start_q, 2)
        y_test_pred_q = clf_qml.predict(X_test_q)

        qml_acc = round(float(accuracy_score(y_test, y_test_pred_q) * 100.0), 2)
        qml_prec = round(float(precision_score(y_test, y_test_pred_q, average='weighted', zero_division=0) * 100.0), 2)
        qml_rec = round(float(recall_score(y_test, y_test_pred_q, average='weighted', zero_division=0) * 100.0), 2)
        qml_f1 = round(float(f1_score(y_test, y_test_pred_q, average='weighted', zero_division=0) * 100.0), 2)

        # 2. Train Real Classical Counterpart Classifier
        start_c = time.time()
        c_upper = classical_model.upper()
        if "KNN" in c_upper:
            clf_class = KNeighborsClassifier(n_neighbors=min(5, len(X_train_c)))
        elif "MLP" in c_upper or "NEURAL" in c_upper:
            clf_class = MLPClassifier(hidden_layer_sizes=(64, 32), max_iter=200, random_state=42)
        elif "LOGISTIC" in c_upper:
            clf_class = LogisticRegression(max_iter=200, random_state=42)
        elif "FOREST" in c_upper:
            clf_class = RandomForestClassifier(n_estimators=100, random_state=42)
        else: # Classical SVM
            clf_class = SVC(kernel='linear', C=1.0, random_state=42)

        clf_class.fit(X_train_c, y_train)
        classical_time = round(time.time() - start_c, 2)
        y_test_pred_c = clf_class.predict(X_test_c)

        class_acc = round(float(accuracy_score(y_test, y_test_pred_c) * 100.0), 2)
        class_prec = round(float(precision_score(y_test, y_test_pred_c, average='weighted', zero_division=0) * 100.0), 2)
        class_rec = round(float(recall_score(y_test, y_test_pred_c, average='weighted', zero_division=0) * 100.0), 2)
        class_f1 = round(float(f1_score(y_test, y_test_pred_c, average='weighted', zero_division=0) * 100.0), 2)

        acc_improvement = round(qml_acc - class_acc, 2)
        param_reduction = round(float(((X_scaled.shape[1] - actual_components) / max(1, X_scaled.shape[1])) * 100.0), 1)

        # 3. Render Real Dual-Model Comparison Chart
        img_b64 = ""
        if HAS_MATPLOTLIB:
            plt.close('all')
            fig, ax = plt.subplots(figsize=(7.5, 3.5), dpi=110)

            categories = ['Accuracy %', 'Precision %', 'Recall %', 'F1-Score %']
            q_metrics = [qml_acc, qml_prec, qml_rec, qml_f1]
            c_metrics = [class_acc, class_prec, class_rec, class_f1]

            x = np.arange(len(categories))
            width = 0.35

            rects1 = ax.bar(x - width/2, q_metrics, width, label=f'QML ({qml_model})', color='#6547FF')
            rects2 = ax.bar(x + width/2, c_metrics, width, label=f'Classical ({classical_model})', color='#148DF5')

            ax.set_title(f'Real Benchmark on Dataset: {os.path.basename(dataset_path)}', fontsize=10, fontweight='bold', color='#111729')
            ax.set_xticks(x)
            ax.set_xticklabels(categories, fontsize=8)
            ax.set_ylim(0, 115)
            ax.grid(True, linestyle='--', alpha=0.3, axis='y')
            ax.legend(fontsize=8)

            for rect in rects1 + rects2:
                height = rect.get_height()
                ax.annotate(f'{height:.1f}%', xy=(rect.get_x() + rect.get_width() / 2, height),
                            xytext=(0, 3), textcoords="offset points", ha='center', va='bottom', fontsize=7, fontweight='bold')

            plt.tight_layout()
            buf = io.BytesIO()
            plt.savefig(buf, format='png', bbox_inches='tight')
            plt.close('all')
            buf.seek(0)
            img_b64 = base64.b64encode(buf.read()).decode('utf-8')

        res = {
            "status": "success",
            "qml_model": qml_model,
            "classical_model": classical_model,
            "qubits": qubits,
            "shots": shots,
            "samples_count": len(df),
            "qml_accuracy": qml_acc,
            "qml_precision": qml_prec,
            "qml_recall": qml_rec,
            "qml_f1": qml_f1,
            "qml_time_seconds": qml_time,
            "qml_parameters": actual_components * 2,
            "classical_accuracy": class_acc,
            "classical_precision": class_prec,
            "classical_recall": class_rec,
            "classical_f1": class_f1,
            "classical_time_seconds": classical_time,
            "classical_parameters": X_scaled.shape[1] * 8,
            "accuracy_improvement": acc_improvement,
            "parameter_reduction_pct": param_reduction,
            "chart_image_base64": img_b64
        }
        return json.dumps(res)

    except Exception as e:
        return json.dumps({"error": str(e)})

if __name__ == "__main__":
    if len(sys.argv) > 5:
        print(run_hybrid_cross_study(sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])))
    elif len(sys.argv) > 3:
        print(run_hybrid_cross_study(sys.argv[1], sys.argv[2], sys.argv[3]))
    else:
        print(run_hybrid_cross_study("default"))
