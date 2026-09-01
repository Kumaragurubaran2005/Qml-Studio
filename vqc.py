import numpy as np
from scipy.optimize import minimize
from qiskit import QuantumCircuit
from qiskit_aer import AerSimulator


def vqc_circuit(x, params, n_reps=2):
    """
    Constructs a Variational Quantum Classifier (VQC) circuit:
    - Feature Map: H + Rz(x)
    - Variational Ansatz: Ry(theta) + CNOT entanglers
    """
    n_qubits = len(x)
    qc = QuantumCircuit(n_qubits, n_qubits)
    
    # 1. Feature Map
    for i in range(n_qubits):
        qc.h(i)
        qc.rz(2.0 * x[i], i)
        
    # 2. Variational Ansatz
    param_idx = 0
    for r in range(n_reps):
        for i in range(n_qubits):
            qc.ry(params[param_idx], i)
            param_idx += 1
            
        for i in range(n_qubits - 1):
            qc.cx(i, i + 1)
            
    # Final layer of Ry rotations
    for i in range(n_qubits):
        qc.ry(params[param_idx], i)
        param_idx += 1
        
    qc.measure(range(n_qubits), range(n_qubits))
    return qc


def evaluate_vqc(x, params, n_reps=2, shots=1024):
    """
    Evaluates parity of measurement string: P(even parity) vs P(odd parity)
    to classify into binary classes 0 and 1.
    """
    qc = vqc_circuit(x, params, n_reps=n_reps)
    simulator = AerSimulator()
    result = simulator.run(qc, shots=shots).result()
    counts = result.get_counts()
    
    even_counts = 0
    odd_counts = 0
    
    for bitstr, count in counts.items():
        # Count 1s in bitstring
        ones = bitstr.count('1')
        if ones % 2 == 0:
            even_counts += count
        else:
            odd_counts += count
            
    total = even_counts + odd_counts
    prob_class0 = even_counts / float(total) if total > 0 else 0.5
    prob_class1 = odd_counts / float(total) if total > 0 else 0.5
    
    return prob_class0, prob_class1


def vqc_objective(params, X_train, y_train, n_reps=2, shots=1024):
    """
    Objective function for VQC parameter optimization using Cross-Entropy loss.
    """
    loss = 0.0
    for x_i, y_i in zip(X_train, y_train):
        p0, p1 = evaluate_vqc(x_i, params, n_reps=n_reps, shots=shots)
        p = p1 if y_i == 1 else p0
        p = np.clip(p, 1e-6, 1.0 - 1e-6)
        loss += - np.log(p)
    return loss / len(X_train)


def train_vqc(X_train, y_train, query, max_iter=20, shots=1024, n_reps=2):
    """
    Trains VQC ansatz parameters and classifies query vector.
    """
    n_qubits = X_train.shape[1]
    # Total params = (n_reps + 1) * n_qubits
    n_params = (n_reps + 1) * n_qubits
    
    np.random.seed(123)
    initial_params = np.random.uniform(-np.pi, np.pi, size=n_params)
    
    res = minimize(
        vqc_objective,
        initial_params,
        args=(X_train, y_train, n_reps, shots),
        method='COBYLA',
        options={'maxiter': max_iter}
    )
    
    opt_params = res.x
    final_loss = float(res.fun)
    
    p0, p1 = evaluate_vqc(query, opt_params, n_reps=n_reps, shots=shots)
    pred_label = 1 if p1 >= p0 else 0
    
    return pred_label, {"class_0": float(p0), "class_1": float(p1)}, final_loss, opt_params.tolist()


def run_vqc_wrapper_paths(train_data_path, train_labels_path, query_path, max_iter=20, shots=1024):
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

    pred, probs, loss, params = train_vqc(
        X_train, y_train, query_arr, max_iter=int(max_iter), shots=int(shots)
    )

    return {
        "prediction": pred,
        "probabilities": probs,
        "final_loss": loss,
        "optimized_parameters": params
    }
