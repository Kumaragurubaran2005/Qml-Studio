use std::ffi::{CStr, CString};
use std::os::raw::c_char;
use std::fs::File;
use std::collections::HashSet;
use std::process::Command;
use serde::{Serialize, Deserialize};

fn sanitize_json_str(s: &str) -> String {
    s.replace('\\', "\\\\")
     .replace('"', "\\\"")
     .replace('\r', "\\r")
     .replace('\n', "\\n")
     .replace('\t', "\\t")
}

fn get_script_path(script_name: &str) -> String {
    if std::path::Path::new(script_name).exists() {
        return script_name.to_string();
    }
    if let Ok(mut exe_path) = std::env::current_exe() {
        exe_path.pop();
        let target = exe_path.join(script_name);
        if target.exists() {
            return target.to_string_lossy().to_string();
        }
    }
    let hardcoded = std::path::Path::new(r"C:\Users\kumar\source\repos\QML Studio\QML Studio").join(script_name);
    if hardcoded.exists() {
        return hardcoded.to_string_lossy().to_string();
    }
    script_name.to_string()
}

#[derive(Serialize, Deserialize, Debug)]
pub struct ColumnSummary {
    pub name: String,
    pub data_type: String,
    pub non_null_count: usize,
    pub null_count: usize,
    pub null_percentage: f64,
    pub unique_count: usize,
    pub sample_val: String,
}

#[derive(Serialize, Deserialize, Debug)]
pub struct DatasetAnalysisResult {
    pub file_name: String,
    pub file_path: String,
    pub total_rows: usize,
    pub total_columns: usize,
    pub total_nulls: usize,
    pub numeric_cols_count: usize,
    pub categorical_cols_count: usize,
    pub columns: Vec<ColumnSummary>,
}

#[no_mangle]
pub unsafe extern "C" fn analyze_dataset(path_ptr: *const c_char) -> *mut c_char {
    if path_ptr.is_null() {
        return CString::new("{\"error\":\"Null file path provided\"}").unwrap().into_raw();
    }

    let c_str = CStr::from_ptr(path_ptr);
    let path_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return CString::new("{\"error\":\"Invalid UTF-8 in file path\"}").unwrap().into_raw(),
    };

    let file_name = std::path::Path::new(path_str)
        .file_name()
        .map(|n| n.to_string_lossy().to_string())
        .unwrap_or_else(|| "dataset.csv".to_string());

    let file = match File::open(path_str) {
        Ok(f) => f,
        Err(e) => {
            let err_msg = format!("{{\"error\":\"Could not open file: {}\"}}", sanitize_json_str(&e.to_string()));
            return CString::new(err_msg).unwrap().into_raw();
        }
    };

    let mut rdr = csv::ReaderBuilder::new()
        .has_headers(true)
        .flexible(true)
        .from_reader(file);

    let headers = match rdr.headers() {
        Ok(h) => h.clone(),
        Err(e) => {
            let err_msg = format!("{{\"error\":\"Could not read CSV headers: {}\"}}", sanitize_json_str(&e.to_string()));
            return CString::new(err_msg).unwrap().into_raw();
        }
    };

    let col_count = headers.len();
    let mut rows_count = 0usize;
    let mut null_counts = vec![0usize; col_count];
    let mut non_null_counts = vec![0usize; col_count];
    let mut is_numeric = vec![true; col_count];
    let mut unique_sets: Vec<HashSet<String>> = vec![HashSet::new(); col_count];
    let mut sample_vals = vec![String::new(); col_count];

    for result in rdr.records() {
        let record = match result {
            Ok(r) => r,
            Err(_) => continue,
        };
        rows_count += 1;

        for i in 0..col_count {
            let val = record.get(i).unwrap_or("").trim();
            if val.is_empty() || val.eq_ignore_ascii_case("null") || val.eq_ignore_ascii_case("nan") || val.eq_ignore_ascii_case("none") {
                null_counts[i] += 1;
            } else {
                non_null_counts[i] += 1;
                if sample_vals[i].is_empty() {
                    sample_vals[i] = val.to_string();
                }
                if is_numeric[i] && val.parse::<f64>().is_err() {
                    is_numeric[i] = false;
                }
                if unique_sets[i].len() < 500 {
                    unique_sets[i].insert(val.to_string());
                }
            }
        }
    }

    let mut total_nulls = 0usize;
    let mut numeric_cols_count = 0usize;
    let mut categorical_cols_count = 0usize;
    let mut columns = Vec::new();

    for i in 0..col_count {
        let col_name = headers.get(i).unwrap_or(&format!("Col_{}", i)).to_string();
        let nulls = null_counts[i];
        let non_nulls = non_null_counts[i];
        total_nulls += nulls;

        let data_type = if is_numeric[i] {
            numeric_cols_count += 1;
            "Numeric".to_string()
        } else {
            categorical_cols_count += 1;
            "Categorical".to_string()
        };

        let null_percentage = if rows_count > 0 {
            (nulls as f64 / rows_count as f64) * 100.0
        } else {
            0.0
        };

        columns.push(ColumnSummary {
            name: col_name,
            data_type,
            non_null_count: non_nulls,
            null_count: nulls,
            null_percentage: (null_percentage * 10.0).round() / 10.0,
            unique_count: unique_sets[i].len(),
            sample_val: sample_vals[i].clone(),
        });
    }

    let result_obj = DatasetAnalysisResult {
        file_name,
        file_path: path_str.to_string(),
        total_rows: rows_count,
        total_columns: col_count,
        total_nulls,
        numeric_cols_count,
        categorical_cols_count,
        columns,
    };

    let json_str = serde_json::to_string(&result_obj).unwrap_or_else(|_| "{\"error\":\"Serialization error\"}".to_string());
    CString::new(json_str).unwrap().into_raw()
}

#[no_mangle]
pub unsafe extern "C" fn clean_dataset(path_ptr: *const c_char) -> *mut c_char {
    if path_ptr.is_null() {
        return CString::new("{\"error\":\"Null file path\"}").unwrap().into_raw();
    }
    let c_str = CStr::from_ptr(path_ptr);
    let path_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return CString::new("{\"error\":\"Invalid UTF-8 path\"}").unwrap().into_raw(),
    };

    let script = get_script_path("data_processor.py");
    let output = Command::new("python")
        .args(&[script.as_str(), "clean", path_str])
        .output();

    match output {
        Ok(out) => {
            let res_str = String::from_utf8_lossy(&out.stdout).to_string();
            if res_str.trim().is_empty() {
                let err_str = String::from_utf8_lossy(&out.stderr).to_string();
                let json_err = format!("{{\"error\":\"Python execution failed: {}\"}}", sanitize_json_str(err_str.trim()));
                CString::new(json_err).unwrap().into_raw()
            } else {
                CString::new(res_str.trim()).unwrap().into_raw()
            }
        }
        Err(e) => {
            let json_err = format!("{{\"error\":\"Could not launch Python: {}\"}}", sanitize_json_str(&e.to_string()));
            CString::new(json_err).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn generate_dataset_plot(
    path_ptr: *const c_char,
    x_ptr: *const c_char,
    y_ptr: *const c_char,
    plot_type_ptr: *const c_char,
) -> *mut c_char {
    if path_ptr.is_null() || x_ptr.is_null() || y_ptr.is_null() || plot_type_ptr.is_null() {
        return CString::new("{\"error\":\"Null arguments\"}").unwrap().into_raw();
    }

    let path_str = CStr::from_ptr(path_ptr).to_string_lossy();
    let x_str = CStr::from_ptr(x_ptr).to_string_lossy();
    let y_str = CStr::from_ptr(y_ptr).to_string_lossy();
    let plot_type_str = CStr::from_ptr(plot_type_ptr).to_string_lossy();

    let script = get_script_path("data_processor.py");
    let output = Command::new("python")
        .args(&[script.as_str(), "plot", path_str.as_ref(), x_str.as_ref(), y_str.as_ref(), plot_type_str.as_ref()])
        .output();

    match output {
        Ok(out) => {
            let res_str = String::from_utf8_lossy(&out.stdout).to_string();
            if res_str.trim().is_empty() {
                let err_str = String::from_utf8_lossy(&out.stderr).to_string();
                let json_err = format!("{{\"error\":\"Python plot failed: {}\"}}", sanitize_json_str(err_str.trim()));
                CString::new(json_err).unwrap().into_raw()
            } else {
                CString::new(res_str.trim()).unwrap().into_raw()
            }
        }
        Err(e) => {
            let json_err = format!("{{\"error\":\"Could not launch Python: {}\"}}", sanitize_json_str(&e.to_string()));
            CString::new(json_err).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn run_hybrid_cross_study(
    path_ptr: *const c_char,
    qml_ptr: *const c_char,
    classical_ptr: *const c_char,
    qubits: i32,
    shots: i32,
) -> *mut c_char {
    if path_ptr.is_null() || qml_ptr.is_null() || classical_ptr.is_null() {
        return CString::new("{\"error\":\"Null arguments\"}").unwrap().into_raw();
    }

    let path_str = CStr::from_ptr(path_ptr).to_string_lossy();
    let qml_str = CStr::from_ptr(qml_ptr).to_string_lossy();
    let classical_str = CStr::from_ptr(classical_ptr).to_string_lossy();

    let script = get_script_path("hybrid_benchmark.py");
    let q_str = qubits.to_string();
    let s_str = shots.to_string();

    let output = Command::new("python")
        .args(&[script.as_str(), path_str.as_ref(), qml_str.as_ref(), classical_str.as_ref(), q_str.as_str(), s_str.as_str()])
        .output();

    match output {
        Ok(out) => {
            let res_str = String::from_utf8_lossy(&out.stdout).to_string();
            if res_str.trim().is_empty() {
                let err_str = String::from_utf8_lossy(&out.stderr).to_string();
                let json_err = format!("{{\"error\":\"Hybrid cross-study failed: {}\"}}", sanitize_json_str(err_str.trim()));
                CString::new(json_err).unwrap().into_raw()
            } else {
                CString::new(res_str.trim()).unwrap().into_raw()
            }
        }
        Err(e) => {
            let json_err = format!("{{\"error\":\"Could not launch Python: {}\"}}", sanitize_json_str(&e.to_string()));
            CString::new(json_err).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn train_qml_model(
    path_ptr: *const c_char,
    target_ptr: *const c_char,
    model_ptr: *const c_char,
    qubits: i32,
    shots: i32,
    fmap_ptr: *const c_char,
    ansatz_ptr: *const c_char,
    opt_ptr: *const c_char,
) -> *mut c_char {
    if path_ptr.is_null() || target_ptr.is_null() || model_ptr.is_null() || fmap_ptr.is_null() || ansatz_ptr.is_null() || opt_ptr.is_null() {
        return CString::new("{\"error\":\"Null arguments\"}").unwrap().into_raw();
    }

    let path_str = CStr::from_ptr(path_ptr).to_string_lossy();
    let target_str = CStr::from_ptr(target_ptr).to_string_lossy();
    let model_str = CStr::from_ptr(model_ptr).to_string_lossy();
    let fmap_str = CStr::from_ptr(fmap_ptr).to_string_lossy();
    let ansatz_str = CStr::from_ptr(ansatz_ptr).to_string_lossy();
    let opt_str = CStr::from_ptr(opt_ptr).to_string_lossy();

    let script = get_script_path("qml_trainer.py");
    let q_str = qubits.to_string();
    let s_str = shots.to_string();

    let output = Command::new("python")
        .args(&[
            script.as_str(),
            path_str.as_ref(),
            target_str.as_ref(),
            model_str.as_ref(),
            q_str.as_str(),
            s_str.as_str(),
            fmap_str.as_ref(),
            ansatz_str.as_ref(),
            opt_str.as_ref(),
        ])
        .output();

    match output {
        Ok(out) => {
            let res_str = String::from_utf8_lossy(&out.stdout).to_string();
            if res_str.trim().is_empty() {
                let err_str = String::from_utf8_lossy(&out.stderr).to_string();
                let json_err = format!("{{\"error\":\"QML training failed: {}\"}}", sanitize_json_str(err_str.trim()));
                CString::new(json_err).unwrap().into_raw()
            } else {
                CString::new(res_str.trim()).unwrap().into_raw()
            }
        }
        Err(e) => {
            let json_err = format!("{{\"error\":\"Could not launch Python: {}\"}}", sanitize_json_str(&e.to_string()));
            CString::new(json_err).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn predict_sample_ffi(
    path_ptr: *const c_char,
    features_json_ptr: *const c_char,
) -> *mut c_char {
    if path_ptr.is_null() || features_json_ptr.is_null() {
        return CString::new("{\"error\":\"Null arguments\"}").unwrap().into_raw();
    }

    let path_str = CStr::from_ptr(path_ptr).to_string_lossy();
    let json_str = CStr::from_ptr(features_json_ptr).to_string_lossy();

    let script = get_script_path("qml_inference.py");
    let output = Command::new("python")
        .args(&[script.as_str(), path_str.as_ref(), json_str.as_ref()])
        .output();

    match output {
        Ok(out) => {
            let res_str = String::from_utf8_lossy(&out.stdout).to_string();
            if res_str.trim().is_empty() {
                let err_str = String::from_utf8_lossy(&out.stderr).to_string();
                let json_err = format!("{{\"error\":\"Prediction failed: {}\"}}", sanitize_json_str(err_str.trim()));
                CString::new(json_err).unwrap().into_raw()
            } else {
                CString::new(res_str.trim()).unwrap().into_raw()
            }
        }
        Err(e) => {
            let json_err = format!("{{\"error\":\"Could not launch Python: {}\"}}", sanitize_json_str(&e.to_string()));
            CString::new(json_err).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn predict_batch_ffi(
    path_ptr: *const c_char,
    test_csv_ptr: *const c_char,
) -> *mut c_char {
    if path_ptr.is_null() || test_csv_ptr.is_null() {
        return CString::new("{\"error\":\"Null arguments\"}").unwrap().into_raw();
    }

    let path_str = CStr::from_ptr(path_ptr).to_string_lossy();
    let test_csv_str = CStr::from_ptr(test_csv_ptr).to_string_lossy();

    let script = get_script_path("qml_inference.py");
    let output = Command::new("python")
        .args(&[script.as_str(), "batch", path_str.as_ref(), test_csv_str.as_ref()])
        .output();

    match output {
        Ok(out) => {
            let res_str = String::from_utf8_lossy(&out.stdout).to_string();
            if res_str.trim().is_empty() {
                let err_str = String::from_utf8_lossy(&out.stderr).to_string();
                let json_err = format!("{{\"error\":\"Batch prediction failed: {}\"}}", sanitize_json_str(err_str.trim()));
                CString::new(json_err).unwrap().into_raw()
            } else {
                CString::new(res_str.trim()).unwrap().into_raw()
            }
        }
        Err(e) => {
            let json_err = format!("{{\"error\":\"Could not launch Python: {}\"}}", sanitize_json_str(&e.to_string()));
            CString::new(json_err).unwrap().into_raw()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn free_string(ptr: *mut c_char) {
    if !ptr.is_null() {
        let _ = CString::from_raw(ptr);
    }
}
