import numpy as np
from scipy.optimize import minimize
from qiskit import QuantumCircuit
from qiskit_aer import AerSimulator


def build_qnn_circuit(x, params, n_layers=2):
    """
    Constructs a Parameterized Quantum Neural Network circuit:
    - Input Feature Encoding: Ry(x_i)
    - Variational Layers: Rx(theta), Rz(theta) followed by CNOT entangling ladder.
    """
    n_qubits = len(x)
    qc = QuantumCircuit(n_qubits, n_qubits)
    
    # Input Angle Encoding
    for i in range(n_qubits):
        qc.ry(x[i], i)
        
    param_idx = 0
    for layer in range(n_layers):
        # Single Qubit Parameterized Rotations
        for i in range(n_qubits):
            qc.rx(params[param_idx], i)
            param_idx += 1
            qc.rz(params[param_idx], i)
            param_idx += 1
            
        # Entangling Layer
        for i in range(n_qubits - 1):
            qc.cx(i, i + 1)
        if n_qubits > 2:
            qc.cx(n_qubits - 1, 0)
            
    qc.measure(range(n_qubits), range(n_qubits))
    return qc


def evaluate_qnn(x, params, n_layers=2, shots=1024):
    """
    Evaluates expectation value <Z_0> of the first qubit in the QNN output state.
    Returns activation value bounded in [-1, 1] and probability in [0, 1].
    """
    qc = build_qnn_circuit(x, params, n_layers=n_layers)
    simulator = AerSimulator()
    result = simulator.run(qc, shots=shots).result()
    counts = result.get_counts()
    
    # Measure expectation value of qubit 0: <Z_0>
    p0 = 0
    p1 = 0
    for bitstring, count in counts.items():
        # Qiskit bitstring order is qubit_n-1 ... qubit_0
        if bitstring[-1] == '0':
            p0 += count
        else:
            p1 += count
            
    total = p0 + p1
    exp_z0 = (p0 - p1) / float(total) if total > 0 else 0.0
    prob_class1 = 0.5 * (1.0 - exp_z0) # Map exp to probability range [0, 1]
    return exp_z0, prob_class1


def qnn_loss(params, X_train, y_train, n_layers=2, shots=1024):
    """
    Binary Cross-Entropy / Mean Squared Error loss for QNN parameters.
    """
    loss = 0.0
    for x_i, y_i in zip(X_train, y_train):
        _, prob1 = evaluate_qnn(x_i, params, n_layers=n_layers, shots=shots)
        # Clip prob for numerical stability
        p = np.clip(prob1, 1e-6, 1.0 - 1e-6)
        target = 1.0 if y_i == 1 else 0.0
        loss += - (target * np.log(p) + (1.0 - target) * np.log(1.0 - p))
    return loss / len(X_train)


def train_and_predict_qnn(X_train, y_train, query, epochs=15, shots=1024, n_layers=2):
    """
    Trains QNN parameters using SciPy COBYLA optimizer and predicts class for query sample.
    """
    n_qubits = X_train.shape[1]
    n_params = 2 * n_qubits * n_layers
    
    np.random.seed(42)
    init_params = np.random.uniform(0, 2 * np.pi, size=n_params)
    
    # Train QNN
    res = minimize(
        qnn_loss,
        init_params,
        args=(X_train, y_train, n_layers, shots),
        method='COBYLA',
        options={'maxiter': epochs}
    )
    
    opt_params = res.x
    final_loss = float(res.fun)
    
    # Evaluate Query
    exp_z0, prob1 = evaluate_qnn(query, opt_params, n_layers=n_layers, shots=shots)
    pred_class = 1 if prob1 >= 0.5 else 0
    
    return pred_class, float(prob1), final_loss, opt_params.tolist()


def run_qnn_wrapper_paths(train_data_path, train_labels_path, query_path, epochs=15, lr=0.1, shots=1024):
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

    pred, prob, loss, trained_params = train_and_predict_qnn(
        X_train, y_train, query_arr, epochs=int(epochs), shots=int(shots)
    )

    return {
        "prediction": pred,
        "probability": prob,
        "training_loss": loss,
        "parameters": trained_params
    }
