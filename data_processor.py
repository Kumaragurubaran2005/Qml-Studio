import sys
import os
import csv
import json
import base64
import io
from collections import Counter
import numpy as np

# Optional pandas import
try:
    import pandas as pd
    HAS_PANDAS = True
except ImportError:
    HAS_PANDAS = False

# Optional matplotlib import
try:
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    HAS_MATPLOTLIB = True
except ImportError:
    HAS_MATPLOTLIB = False

def clean_and_preprocess(file_path, variance_threshold=0.01, max_identical_ratio=0.95):
    """
    Cleans null values, standardizes/normalizes numeric columns,
    removes low-variance & near-constant features (>95% identical or var < 0.01),
    and label-encodes categorical values.
    Returns JSON dictionary with cleaning summary and output CSV path.
    """
    if not os.path.exists(file_path):
        return json.dumps({"error": f"File not found: {file_path}"})

    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            reader = csv.reader(f)
            rows = list(reader)

        if not rows:
            return json.dumps({"error": "Empty file"})

        header = [h.strip() for h in rows[0]]
        data_rows = rows[1:]
        num_rows = len(data_rows)
        num_cols = len(header)

        matrix = []
        for r_idx, row in enumerate(data_rows):
            row_vals = []
            for c_idx in range(num_cols):
                val = row[c_idx].strip() if c_idx < len(row) else ""
                row_vals.append(val)
            matrix.append(row_vals)

        col_null_counts = []
        cleaned_columns = []
        removed_columns = []
        encoded_columns = []
        scaled_columns = []

        processed_data = [[] for _ in range(num_rows)]

        for c_idx in range(num_cols):
            col_name = header[c_idx]
            raw_vals = [matrix[r][c_idx] for r in range(num_rows)]

            non_null_vals = [v for v in raw_vals if v not in ("", "null", "NaN", "nan", "None", "NONE")]
            null_count = num_rows - len(non_null_vals)
            col_null_counts.append(null_count)

            is_num = True
            numeric_vals = []
            for v in non_null_vals:
                try:
                    numeric_vals.append(float(v))
                except ValueError:
                    is_num = False
                    break

            if is_num and len(numeric_vals) > 0:
                median_val = float(np.median(numeric_vals)) if len(numeric_vals) > 0 else 0.0
                full_num = []
                for v in raw_vals:
                    try:
                        full_num.append(float(v))
                    except ValueError:
                        full_num.append(median_val)

                arr = np.array(full_num, dtype=float)
                variance = float(np.var(arr))
                
                # Check near-constant frequency ratio
                _, counts = np.unique(arr, return_counts=True)
                max_freq_ratio = float(np.max(counts)) / float(len(arr)) if len(arr) > 0 else 0.0

                # REMOVE LOW VARIANCE OR NEAR-CONSTANT NUMERIC FEATURES
                if variance < variance_threshold or max_freq_ratio >= max_identical_ratio:
                    removed_columns.append(col_name)
                    continue

                mean = float(np.mean(arr))
                std = float(np.std(arr))
                if std > 0:
                    scaled_arr = (arr - mean) / std
                else:
                    scaled_arr = arr

                scaled_columns.append(col_name)
                for r in range(num_rows):
                    processed_data[r].append(f"{scaled_arr[r]:.4f}")

            else:
                # REMOVE NEAR-CONSTANT CATEGORICAL FEATURES (>95% identical)
                if len(non_null_vals) > 0:
                    cat_counts = Counter(non_null_vals)
                    most_common_freq = cat_counts.most_common(1)[0][1]
                    if (most_common_freq / num_rows) >= max_identical_ratio:
                        removed_columns.append(col_name)
                        continue

                unique_labels = sorted(list(set(non_null_vals)))
                label_map = {lbl: idx for idx, lbl in enumerate(unique_labels)}
                mode_val = unique_labels[0] if unique_labels else "Unknown"

                encoded_columns.append(col_name)
                for r in range(num_rows):
                    val = matrix[r][c_idx]
                    if val not in label_map:
                        val = mode_val
                    code = label_map.get(val, 0)
                    processed_data[r].append(str(code))

            cleaned_columns.append(col_name)

        base_dir = os.path.dirname(file_path)
        base_name = os.path.basename(file_path)
        name_part, _ = os.path.splitext(base_name)
        cleaned_path = os.path.join(base_dir, f"{name_part}_cleaned.csv")

        with open(cleaned_path, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(cleaned_columns)
            for r in range(num_rows):
                writer.writerow(processed_data[r])

        res = {
            "status": "success",
            "original_file": file_path,
            "cleaned_file": cleaned_path,
            "original_rows": num_rows,
            "original_cols": num_cols,
            "cleaned_cols": len(cleaned_columns),
            "removed_low_variance_cols": removed_columns,
            "scaled_numeric_cols": scaled_columns,
            "encoded_categorical_cols": encoded_columns,
            "nulls_imputed_count": sum(col_null_counts)
        }
        return json.dumps(res)

    except Exception as e:
        return json.dumps({"error": str(e)})


def generate_plot(file_path, x_col, y_col, plot_type):
    """
    Generates a plot Base64 PNG image string for 11 visualization types.
    """
    if not os.path.exists(file_path):
        return json.dumps({"error": f"File not found: {file_path}"})

    try:
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            reader = csv.reader(f)
            rows = list(reader)

        if not rows:
            return json.dumps({"error": "Empty file"})

        header = [h.strip() for h in rows[0]]
        data_rows = rows[1:]
        num_rows = len(data_rows)
        num_cols = len(header)

        x_idx = header.index(x_col) if x_col in header else 0
        y_idx = header.index(y_col) if y_col in header else min(1, max(0, num_cols - 1))

        x_vals = [r[x_idx].strip() if x_idx < len(r) else "" for r in data_rows[:1000]]
        y_vals = [r[y_idx].strip() if y_idx < len(r) else "" for r in data_rows[:1000]]

        if HAS_MATPLOTLIB:
            plt.close('all')
            fig, ax = plt.subplots(figsize=(7.2, 4.5), dpi=110)
            p_type = plot_type.lower()

            # 1. CORRELATION HEATMAP
            if "heatmap" in p_type or "correlation" in p_type:
                num_cols_data = {}
                for c_i, c_name in enumerate(header):
                    col_raw = [r[c_i].strip() if c_i < len(r) else "" for r in data_rows[:500]]
                    col_num = []
                    for v in col_raw:
                        try: col_num.append(float(v))
                        except ValueError: pass
                    if len(col_num) > len(col_raw) * 0.5:
                        num_cols_data[c_name] = col_num[:500]

                if len(num_cols_data) >= 2 and HAS_PANDAS:
                    df = pd.DataFrame(num_cols_data)
                    corr = df.corr().values
                    cols_list = list(df.columns)
                    im = ax.imshow(corr, cmap='Purples', vmin=-1, vmax=1)
                    fig.colorbar(im, ax=ax)
                    ax.set_xticks(range(len(cols_list)))
                    ax.set_yticks(range(len(cols_list)))
                    ax.set_xticklabels(cols_list, rotation=45, ha='right', fontsize=8)
                    ax.set_yticklabels(cols_list, fontsize=8)
                    ax.set_title("Correlation Heatmap", fontsize=12, fontweight='bold', color='#111729')

                    for i in range(len(cols_list)):
                        for j in range(len(cols_list)):
                            val = corr[i, j]
                            if not np.isnan(val):
                                ax.text(j, i, f"{val:.2f}", ha='center', va='center',
                                        color='white' if abs(val) > 0.5 else 'black', fontsize=7)
                else:
                    ax.text(0.5, 0.5, "Requires numeric features for Heatmap", ha='center', va='center', fontsize=11)

            # 2. SCATTER MATRIX
            elif "scatter matrix" in p_type or "matrix" in p_type:
                num_cols_data = {}
                for c_i, c_name in enumerate(header[:5]):
                    col_num = []
                    for r in data_rows[:200]:
                        v = r[c_i].strip() if c_i < len(r) else ""
                        try: col_num.append(float(v))
                        except ValueError: pass
                    if len(col_num) > 50:
                        num_cols_data[c_name] = col_num[:200]

                if len(num_cols_data) >= 2 and HAS_PANDAS:
                    plt.close(fig)
                    df = pd.DataFrame(num_cols_data)
                    sm = pd.plotting.scatter_matrix(df, figsize=(7.2, 4.5), diagonal='kde', color='#6547FF', alpha=0.6)
                    fig = plt.gcf()
                else:
                    ax.text(0.5, 0.5, "Scatter Matrix requires numeric features", ha='center', va='center')

            # 3. MISSING VALUE CHART
            elif "missing" in p_type:
                null_counts = []
                for c_i, c_name in enumerate(header):
                    raw = [r[c_i].strip() if c_i < len(r) else "" for r in data_rows]
                    nulls = sum(1 for v in raw if v in ("", "null", "nan", "NaN", "None"))
                    pct = (nulls / num_rows) * 100.0 if num_rows > 0 else 0
                    null_counts.append((c_name, pct))

                names = [n[0] for n in null_counts]
                pcts = [n[1] for n in null_counts]
                colors = ['#FF4D4D' if p > 0 else '#0DB979' for p in pcts]
                ax.bar(names, pcts, color=colors)
                ax.set_ylabel("Missing Data %", fontsize=9, color='#71809F')
                ax.set_title("Missing Value Chart per Feature", fontsize=11, fontweight='bold', color='#111729')
                ax.set_xticklabels(names, rotation=45, ha='right', fontsize=8)

            # 4. FEATURE DISTRIBUTION GRID
            elif "grid" in p_type or "feature distribution" in p_type:
                num_cols_data = []
                for c_i, c_name in enumerate(header):
                    col_num = []
                    for r in data_rows[:300]:
                        v = r[c_i].strip() if c_i < len(r) else ""
                        try: col_num.append(float(v))
                        except ValueError: pass
                    if len(col_num) > 50:
                        num_cols_data.append((c_name, col_num))

                plt.close(fig)
                n = min(6, len(num_cols_data))
                if n > 0:
                    fig, axes = plt.subplots(2, 3, figsize=(7.2, 4.5), dpi=100)
                    axes_flat = axes.flatten()
                    for idx in range(n):
                        c_name, col_vals = num_cols_data[idx]
                        axes_flat[idx].hist(col_vals, bins=12, color='#148DF5', edgecolor='white')
                        axes_flat[idx].set_title(c_name, fontsize=8, fontweight='bold')
                    fig.tight_layout()
                else:
                    fig, ax = plt.subplots(figsize=(7.2, 4.5))
                    ax.text(0.5, 0.5, "No numeric features found for grid", ha='center', va='center')

            # 5. CLASS DISTRIBUTION CHART
            elif "class" in p_type or "target" in p_type:
                counts = {}
                for v in y_vals:
                    counts[v] = counts.get(v, 0) + 1
                classes = list(counts.keys())[:10]
                freqs = [counts[k] for k in classes]
                ax.bar(classes, freqs, color='#6547FF', edgecolor='white')
                ax.set_title(f"Class Distribution: {y_col}", fontsize=11, fontweight='bold', color='#111729')
                ax.set_xlabel(y_col, fontsize=9, color='#71809F')
                ax.set_ylabel("Frequency Count", fontsize=9, color='#71809F')

            # 6. OUTLIER PLOT
            elif "outlier" in p_type:
                num_x = []
                for v in x_vals:
                    try: num_x.append(float(v))
                    except ValueError: pass
                if num_x:
                    arr = np.array(num_x)
                    mean = np.mean(arr)
                    std = np.std(arr)
                    z_scores = np.abs((arr - mean) / (std if std > 0 else 1.0))
                    outliers = z_scores > 2.5
                    normal_idx = np.where(~outliers)[0]
                    outlier_idx = np.where(outliers)[0]

                    ax.scatter(normal_idx, arr[normal_idx], color='#148DF5', label='Normal', alpha=0.7, s=25)
                    ax.scatter(outlier_idx, arr[outlier_idx], color='#FF4D4D', label='Outliers (|Z|>2.5)', s=40, zorder=5)
                    ax.legend(fontsize=8)
                    ax.set_title(f"Outlier Detection Plot: {x_col}", fontsize=11, fontweight='bold', color='#111729')
                else:
                    ax.text(0.5, 0.5, "Numeric column required for outlier plot", ha='center', va='center')

            # 7. VIOLIN PLOT
            elif "violin" in p_type:
                num_x = []
                for v in x_vals:
                    try: num_x.append(float(v))
                    except ValueError: pass
                if not num_x: num_x = [1.0, 2.0, 3.0, 4.0, 5.0]
                ax.violinplot(num_x, vert=True, showmedians=True)
                ax.set_title(f"Violin Plot: {x_col}", fontsize=11, fontweight='bold', color='#111729')

            # 8. BAR CHART
            elif "bar" in p_type:
                counts = {}
                for v in x_vals:
                    counts[v] = counts.get(v, 0) + 1
                cats = list(counts.keys())[:15]
                freqs = [counts[k] for k in cats]
                ax.bar(cats, freqs, color='#6547FF')
                ax.set_title(f"Bar Chart: {x_col}", fontsize=11, fontweight='bold', color='#111729')
                ax.set_xticklabels(cats, rotation=45, ha='right', fontsize=8)

            # 9. BOX PLOT
            elif "box" in p_type:
                num_x = []
                for v in x_vals:
                    try: num_x.append(float(v))
                    except ValueError: pass
                if not num_x: num_x = [1.0, 2.0, 3.0]
                ax.boxplot(num_x, vert=True, patch_artist=True,
                           boxprops=dict(facecolor='#148DF5', color='#148DF5'),
                           medianprops=dict(color='white', linewidth=2))
                ax.set_title(f"Box Plot: {x_col}", fontsize=11, fontweight='bold', color='#111729')

            # 10. LINE PLOT
            elif "line" in p_type:
                num_y = []
                for v in y_vals:
                    try: num_y.append(float(v))
                    except ValueError: num_y.append(0.0)
                ax.plot(range(len(num_y)), num_y, color='#6547FF', linewidth=2)
                ax.set_title(f"Line Plot: {x_col} vs {y_col}", fontsize=11, fontweight='bold', color='#111729')

            # 11. HISTOGRAM / DEFAULT
            elif "histogram" in p_type:
                num_x = []
                for v in x_vals:
                    try: num_x.append(float(v))
                    except ValueError: pass
                if not num_x: num_x = [1.0, 2.0, 3.0]
                ax.hist(num_x, bins=15, color='#148DF5', edgecolor='white')
                ax.set_title(f"Histogram: {x_col}", fontsize=11, fontweight='bold', color='#111729')

            else:
                num_x = []
                num_y = []
                for xv, yv in zip(x_vals, y_vals):
                    try:
                        num_x.append(float(xv))
                        num_y.append(float(yv))
                    except ValueError:
                        pass
                if num_x and num_y:
                    ax.scatter(num_x, num_y, color='#6547FF', alpha=0.7, edgecolors='none', s=30)
                else:
                    ax.scatter(range(len(x_vals)), range(len(x_vals)), color='#6547FF', alpha=0.7, s=30)
                ax.set_title(f"Scatter Plot: {x_col} vs {y_col}", fontsize=11, fontweight='bold', color='#111729')

            ax.set_xlabel(x_col, fontsize=9, color='#71809F')
            ax.set_ylabel(y_col, fontsize=9, color='#71809F')
            ax.grid(True, linestyle='--', alpha=0.3)

            png_buf = io.BytesIO()
            plt.savefig(png_buf, format='png', bbox_inches='tight', dpi=150)
            png_buf.seek(0)
            img_b64 = base64.b64encode(png_buf.read()).decode('utf-8')

            pdf_buf = io.BytesIO()
            plt.savefig(pdf_buf, format='pdf', bbox_inches='tight')
            plt.close('all')
            pdf_buf.seek(0)
            pdf_b64 = base64.b64encode(pdf_buf.read()).decode('utf-8')

            return json.dumps({
                "status": "success",
                "x_col": x_col,
                "y_col": y_col,
                "plot_type": plot_type,
                "image_base64": img_b64,
                "pdf_base64": pdf_b64
            })
        else:
            return json.dumps({"error": "matplotlib not installed"})

    except Exception as e:
        return json.dumps({"error": str(e)})

if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "clean":
        print(clean_and_preprocess(sys.argv[2]))
    elif len(sys.argv) > 4 and sys.argv[1] == "plot":
        print(generate_plot(sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5]))
