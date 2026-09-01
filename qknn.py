import numpy as np
from qiskit import QuantumCircuit
from qiskit_aer import AerSimulator


# ---------------------------------------------------------
# Normalize a vector
# ---------------------------------------------------------
def normalize(x):
    x = np.asarray(x, dtype=float)
    norm = np.linalg.norm(x)

    if norm == 0:
        raise ValueError("Zero vector cannot be normalized.")

    return x / norm


# ---------------------------------------------------------
# SWAP Test
# Estimates similarity between two quantum states
# ---------------------------------------------------------
def swap_test(x, y, shots=2048):
    x = normalize(x)
    y = normalize(y)

    # Number of qubits required for the data
    n = int(np.log2(len(x)))

    if 2 ** n != len(x):
        raise ValueError(
            "Vector length must be a power of 2 "
            "(e.g. 2, 4, 8, 16...)."
        )

    # Qubits:
    # 0       -> ancilla
    # 1..n    -> state x
    # n+1..2n -> state y
    qc = QuantumCircuit(2 * n + 1, 1)

    ancilla = 0
    x_qubits = list(range(1, n + 1))
    y_qubits = list(range(n + 1, 2 * n + 1))

    # Encode x and y
    qc.initialize(x.tolist(), x_qubits)
    qc.initialize(y.tolist(), y_qubits)

    # Hadamard on ancilla
    qc.h(ancilla)

    # Controlled SWAP
    for qx, qy in zip(x_qubits, y_qubits):
        qc.cswap(ancilla, qx, qy)

    # Final Hadamard
    qc.h(ancilla)

    # Measure ancilla
    qc.measure(ancilla, 0)

    # Simulate
    simulator = AerSimulator()
    result = simulator.run(qc, shots=shots).result()

    counts = result.get_counts()

    p0 = counts.get("0", 0) / shots

    # SWAP test:
    # P(0) = (1 + |<x|y>|²) / 2
    similarity = 2 * p0 - 1

    return similarity


# ---------------------------------------------------------
# Quantum distance
# ---------------------------------------------------------
def quantum_distance(x, y, shots=2048):
    similarity = swap_test(x, y, shots)

    # Similarity = |<x|y>|²
    # Convert to distance
    distance = np.sqrt(max(0, 2 - 2 * np.sqrt(max(0, similarity))))

    return distance


# ---------------------------------------------------------
# QKNN
# ---------------------------------------------------------
def qknn(X_train, y_train, query, k=3, shots=2048):

    distances = []

    for i, x in enumerate(X_train):

        distance = quantum_distance(
            x,
            query,
            shots
        )

        distances.append((distance, y_train[i]))

    # Sort by distance
    distances.sort(key=lambda x: x[0])

    # Select k nearest neighbors
    neighbors = distances[:k]

    # Majority voting
    labels = [label for _, label in neighbors]

    prediction = max(
        set(labels),
        key=labels.count
    )

    return prediction, neighbors


def run_qknn_wrapper(train_data, train_labels, query, k, shots):
    """
    Wrapper function to be called from Rust PyO3.
    Expects basic Python lists/ints, runs qknn, and returns a dictionary
    that can easily be serialized to JSON.
    """
    import numpy as np
    
    X_train = np.array(train_data, dtype=float)
    y_train = np.array(train_labels, dtype=int)
    query_arr = np.array(query, dtype=float)

    prediction, neighbors = qknn(
        X_train,
        y_train,
        query_arr,
        k=k,
        shots=shots
    )

    # Convert prediction to standard int (from np.int64)
    pred_int = int(prediction)

    # Convert neighbors list of tuples into serializable list of dicts
    neighbors_list = []
    for distance, label in neighbors:
        neighbors_list.append({
            "distance": float(distance),
            "label": int(label)
        })

    return {
        "prediction": pred_int,
        "neighbors": neighbors_list
    }

def run_qknn_wrapper_paths(train_data_path, train_labels_path, query_path, k, shots):
    """
    Wrapper function to be called from Rust PyO3 that loads data from CSV paths.
    """
    import numpy as np

    # Load data from paths (assuming comma-separated values)
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    # Ensure correct dimensions
    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)
    
    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    prediction, neighbors = qknn(
        X_train,
        y_train,
        query_arr,
        k=k,
        shots=shots
    )

    pred_int = int(prediction)

    neighbors_list = []
    for distance, label in neighbors:
        neighbors_list.append({
            "distance": float(distance),
            "label": int(label)
        })

    return {
        "prediction": pred_int,
        "neighbors": neighbors_list
    }