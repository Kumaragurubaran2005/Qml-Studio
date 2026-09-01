import numpy as np
from qiskit import QuantumCircuit
from qiskit_aer import AerSimulator


def feature_map_circuit(x):
    """
    Constructs a ZZ-Feature Map quantum circuit for a 1D vector x.
    Encodes feature values into quantum states using single-qubit H and Rz gates,
    and two-qubit entangling Rzz (CNOT-Rz-CNOT) interactions.
    """
    n = len(x)
    qc = QuantumCircuit(n)
    
    # Layer 1: Hadamard gates
    for i in range(n):
        qc.h(i)
        
    # Layer 2: Single qubit rotations encoding features
    for i in range(n):
        qc.rz(2.0 * x[i], i)
        
    # Layer 3: Two-qubit entangling interactions (ZZ feature map)
    for i in range(n):
        for j in range(i + 1, n):
            qc.cx(i, j)
            qc.rz(2.0 * (np.pi - x[i]) * (np.pi - x[j]), j)
            qc.cx(i, j)
            
    return qc


def compute_quantum_kernel_element(x1, x2, shots=2048):
    """
    Computes fidelity/overlap |<Phi(x1)|Phi(x2)>|^2 between two data samples
    by running U_Phi(x1) followed by U_Phi(x2)^dagger and measuring the all-zero state probability.
    """
    n = max(len(x1), len(x2))
    
    # Pad to equal length if needed
    if len(x1) < n:
        x1 = np.pad(x1, (0, n - len(x1)))
    if len(x2) < n:
        x2 = np.pad(x2, (0, n - len(x2)))

    fm1 = feature_map_circuit(x1)
    fm2 = feature_map_circuit(x2)
    
    # Invert second feature map
    fm2_inv = fm2.inverse()
    
    # Combine: U(x1) + U_inv(x2)
    qc = QuantumCircuit(n, n)
    qc.compose(fm1, inplace=True)
    qc.compose(fm2_inv, inplace=True)
    qc.measure(range(n), range(n))
    
    simulator = AerSimulator()
    result = simulator.run(qc, shots=shots).result()
    counts = result.get_counts()
    
    all_zero_str = "0" * n
    zero_counts = counts.get(all_zero_str, 0)
    fidelity = zero_counts / float(shots)
    return fidelity


def qsvm_predict(X_train, y_train, query, shots=2048, C=1.0):
    """
    Trains a Quantum Support Vector Machine by constructing the Quantum Kernel Matrix K_ij
    and predicting the class label of a query sample.
    """
    N = len(X_train)
    K_train = np.zeros((N, N))
    
    for i in range(N):
        for j in range(i, N):
            val = compute_quantum_kernel_element(X_train[i], X_train[j], shots=shots)
            K_train[i, j] = val
            K_train[j, i] = val

    unique_labels = np.unique(y_train)
    if len(unique_labels) < 2:
        return int(unique_labels[0]), 0.0, []

    y_svm = np.where(y_train == unique_labels[0], -1.0, 1.0)
    
    reg_K = K_train + (1.0 / float(C)) * np.eye(N)
    alphas = np.linalg.solve(reg_K, y_svm)
    
    K_query = np.zeros(N)
    for i in range(N):
        K_query[i] = compute_quantum_kernel_element(X_train[i], query, shots=shots)
        
    decision_score = float(np.dot(alphas, K_query))
    pred_label = int(unique_labels[0] if decision_score <= 0 else unique_labels[1])
    
    kernel_scores = [{"train_index": i, "kernel_value": float(K_query[i]), "alpha": float(alphas[i])} for i in range(N)]
    
    return pred_label, decision_score, kernel_scores


def run_qsvm_wrapper_paths(train_data_path, train_labels_path, query_path, shots=2048, c_param=1.0):
    """
    Wrapper function to be called from Rust PyO3.
    """
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)
    
    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    pred, score, kernel_scores = qsvm_predict(X_train, y_train, query_arr, shots=int(shots), C=float(c_param))

    return {
        "prediction": pred,
        "decision_score": score,
        "kernel_scores": kernel_scores
    }
