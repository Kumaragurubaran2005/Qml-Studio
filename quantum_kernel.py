import numpy as np
from qiskit import QuantumCircuit
from qiskit_aer import AerSimulator


def quantum_feature_map(x):
    """
    Constructs quantum feature map for vector x.
    Uses Pauli-Z and CNOT entangling gates.
    """
    n = len(x)
    qc = QuantumCircuit(n)
    for i in range(n):
        qc.h(i)
        qc.rz(2.0 * x[i], i)
    for i in range(n - 1):
        qc.cx(i, i + 1)
        qc.rz(2.0 * (np.pi - x[i]) * (np.pi - x[i+1]), i + 1)
        qc.cx(i, i + 1)
    return qc


def compute_fidelity(x1, x2, shots=2048):
    """
    Computes fidelity overlap |<Phi(x1)|Phi(x2)>|^2 using AerSimulator.
    """
    n = max(len(x1), len(x2))
    if len(x1) < n:
        x1 = np.pad(x1, (0, n - len(x1)))
    if len(x2) < n:
        x2 = np.pad(x2, (0, n - len(x2)))

    fm1 = quantum_feature_map(x1)
    fm2 = quantum_feature_map(x2).inverse()

    qc = QuantumCircuit(n, n)
    qc.compose(fm1, inplace=True)
    qc.compose(fm2, inplace=True)
    qc.measure(range(n), range(n))

    simulator = AerSimulator()
    result = simulator.run(qc, shots=shots).result()
    counts = result.get_counts()

    zero_state = "0" * n
    return counts.get(zero_state, 0) / float(shots)


def quantum_kernel_estimation(X_train, y_train, query, shots=2048, alpha=1e-3):
    """
    Calculates full quantum kernel matrix K for training data and query point.
    Classifies query point via Kernel Ridge Regression.
    """
    N = len(X_train)
    K_matrix = np.zeros((N, N))

    for i in range(N):
        for j in range(i, N):
            val = compute_fidelity(X_train[i], X_train[j], shots=shots)
            K_matrix[i, j] = val
            K_matrix[j, i] = val

    # Target encoding {-1, 1}
    unique_labels = np.unique(y_train)
    if len(unique_labels) < 2:
        return int(unique_labels[0]), K_matrix.tolist(), [1.0] * N

    y_target = np.where(y_train == unique_labels[0], -1.0, 1.0)

    # Solve dual weights: (K + alpha * I) w = y
    K_reg = K_matrix + alpha * np.eye(N)
    weights = np.linalg.solve(K_reg, y_target)

    # Compute query similarities
    query_similarities = np.zeros(N)
    for i in range(N):
        query_similarities[i] = compute_fidelity(X_train[i], query, shots=shots)

    score = float(np.dot(weights, query_similarities))
    pred_label = int(unique_labels[0] if score <= 0 else unique_labels[1])

    return pred_label, score, K_matrix.tolist(), query_similarities.tolist()


def run_quantum_kernel_wrapper_paths(train_data_path, train_labels_path, query_path, shots=2048):
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

    pred, score, K_mat, query_sims = quantum_kernel_estimation(
        X_train, y_train, query_arr, shots=int(shots)
    )

    return {
        "prediction": pred,
        "score": score,
        "kernel_matrix": K_mat,
        "query_similarities": query_sims
    }
