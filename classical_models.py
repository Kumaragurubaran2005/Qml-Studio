import numpy as np
from scipy.optimize import minimize


# ---------------------------------------------------------
# 1. Classical K-Nearest Neighbors (KNN)
# ---------------------------------------------------------
def classical_knn(X_train, y_train, query, k=3):
    X_train = np.asarray(X_train, dtype=float)
    y_train = np.asarray(y_train, dtype=int)
    query = np.asarray(query, dtype=float)

    # Compute Euclidean distance to all training samples
    distances = np.sqrt(np.sum((X_train - query) ** 2, axis=1))

    # Pair distance with label
    paired = list(zip(distances, y_train))
    paired.sort(key=lambda x: x[0])

    neighbors = paired[:k]
    neighbor_labels = [label for _, label in neighbors]

    # Majority vote
    prediction = max(set(neighbor_labels), key=neighbor_labels.count)

    neighbors_list = [{"distance": float(d), "label": int(l)} for d, l in neighbors]
    return int(prediction), neighbors_list


def run_classical_knn_wrapper_paths(train_data_path, train_labels_path, query_path, k=3):
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)

    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    pred, neighbors = classical_knn(X_train, y_train, query_arr, k=int(k))
    return {
        "prediction": pred,
        "neighbors": neighbors
    }


# ---------------------------------------------------------
# 2. Classical Support Vector Machine (SVM) - RBF Kernel
# ---------------------------------------------------------
def rbf_kernel(x1, x2, gamma=0.5):
    dist_sq = np.sum((np.asarray(x1) - np.asarray(x2)) ** 2)
    return np.exp(-gamma * dist_sq)


def classical_svm(X_train, y_train, query, C=1.0, gamma=0.5):
    N = len(X_train)
    K = np.zeros((N, N))
    for i in range(N):
        for j in range(i, N):
            val = rbf_kernel(X_train[i], X_train[j], gamma=gamma)
            K[i, j] = val
            K[j, i] = val

    unique_labels = np.unique(y_train)
    if len(unique_labels) < 2:
        return int(unique_labels[0]), 0.0, []

    y_svm = np.where(y_train == unique_labels[0], -1.0, 1.0)

    reg_K = K + (1.0 / float(C)) * np.eye(N)
    alphas = np.linalg.solve(reg_K, y_svm)

    K_query = np.zeros(N)
    for i in range(N):
        K_query[i] = rbf_kernel(X_train[i], query, gamma=gamma)

    decision_score = float(np.dot(alphas, K_query))
    pred_label = int(unique_labels[0] if decision_score <= 0 else unique_labels[1])

    scores = [{"train_index": i, "kernel_value": float(K_query[i]), "alpha": float(alphas[i])} for i in range(N)]
    return pred_label, decision_score, scores


def run_classical_svm_wrapper_paths(train_data_path, train_labels_path, query_path, c_param=1.0, gamma=0.5):
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)

    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    pred, score, scores = classical_svm(X_train, y_train, query_arr, C=float(c_param), gamma=float(gamma))
    return {
        "prediction": pred,
        "decision_score": score,
        "kernel_scores": scores
    }


# ---------------------------------------------------------
# 3. Classical Multi-Layer Perceptron (MLP) Neural Network
# ---------------------------------------------------------
def sigmoid(z):
    return 1.0 / (1.0 + np.exp(-np.clip(z, -15, 15)))


def mlp_forward(x, weights):
    # weights: W1 (n_in, n_hidden), b1 (n_hidden), W2 (n_hidden, 1), b2 (1)
    W1, b1, W2, b2 = weights
    h = np.tanh(np.dot(x, W1) + b1)
    out = sigmoid(np.dot(h, W2) + b2)
    return out


def mlp_loss(params_flat, n_in, n_hidden, X_train, y_train):
    # Unpack parameters
    w1_size = n_in * n_hidden
    b1_size = n_hidden
    w2_size = n_hidden * 1

    W1 = params_flat[:w1_size].reshape(n_in, n_hidden)
    b1 = params_flat[w1_size:w1_size + b1_size]
    W2 = params_flat[w1_size + b1_size:w1_size + b1_size + w2_size].reshape(n_hidden, 1)
    b2 = params_flat[w1_size + b1_size + w2_size:]

    weights = (W1, b1, W2, b2)

    loss = 0.0
    for x_i, y_i in zip(X_train, y_train):
        prob = mlp_forward(x_i, weights)[0]
        p = np.clip(prob, 1e-6, 1.0 - 1e-6)
        target = 1.0 if y_i == 1 else 0.0
        loss += - (target * np.log(p) + (1.0 - target) * np.log(1.0 - p))
    return loss / len(X_train)


def classical_mlp(X_train, y_train, query, epochs=25, n_hidden=4):
    n_in = X_train.shape[1]
    n_params = (n_in * n_hidden) + n_hidden + (n_hidden * 1) + 1

    np.random.seed(42)
    init_params = np.random.normal(0, 0.5, size=n_params)

    res = minimize(
        mlp_loss,
        init_params,
        args=(n_in, n_hidden, X_train, y_train),
        method='BFGS',
        options={'maxiter': epochs}
    )

    opt_flat = res.x
    w1_size = n_in * n_hidden
    b1_size = n_hidden
    w2_size = n_hidden * 1

    W1 = opt_flat[:w1_size].reshape(n_in, n_hidden)
    b1 = opt_flat[w1_size:w1_size + b1_size]
    W2 = opt_flat[w1_size + b1_size:w1_size + b1_size + w2_size].reshape(n_hidden, 1)
    b2 = opt_flat[w1_size + b1_size + w2_size:]

    weights = (W1, b1, W2, b2)

    prob = float(mlp_forward(query, weights)[0])
    pred_class = 1 if prob >= 0.5 else 0
    final_loss = float(res.fun)

    return pred_class, prob, final_loss


def run_classical_mlp_wrapper_paths(train_data_path, train_labels_path, query_path, epochs=25, n_hidden=4):
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)

    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    pred, prob, loss = classical_mlp(X_train, y_train, query_arr, epochs=int(epochs), n_hidden=int(n_hidden))
    return {
        "prediction": pred,
        "probability": prob,
        "loss": loss
    }


# ---------------------------------------------------------
# 4. Classical Logistic Regression
# ---------------------------------------------------------
def logreg_loss(weights, X_train, y_train):
    w = weights[:-1]
    b = weights[-1]

    z = np.dot(X_train, w) + b
    probs = sigmoid(z)
    probs = np.clip(probs, 1e-6, 1.0 - 1e-6)

    targets = np.where(y_train == 1, 1.0, 0.0)
    loss = - np.mean(targets * np.log(probs) + (1.0 - targets) * np.log(1.0 - probs))
    return loss


def classical_logreg(X_train, y_train, query, max_iter=30):
    n_features = X_train.shape[1]
    init_weights = np.zeros(n_features + 1)

    res = minimize(
        logreg_loss,
        init_weights,
        args=(X_train, y_train),
        method='BFGS',
        options={'maxiter': max_iter}
    )

    opt_w = res.x[:-1]
    opt_b = res.x[-1]

    prob = float(sigmoid(np.dot(query, opt_w) + opt_b))
    pred = 1 if prob >= 0.5 else 0

    return pred, prob, float(res.fun)


def run_classical_logreg_wrapper_paths(train_data_path, train_labels_path, query_path, max_iter=30):
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)

    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    pred, prob, loss = classical_logreg(X_train, y_train, query_arr, max_iter=int(max_iter))
    return {
        "prediction": pred,
        "probability": prob,
        "loss": loss
    }


# ---------------------------------------------------------
# 5. Classical Kernel Estimation (Kernel Ridge)
# ---------------------------------------------------------
def classical_kernel_estimation(X_train, y_train, query, gamma=0.5, alpha=1e-3):
    N = len(X_train)
    K_matrix = np.zeros((N, N))

    for i in range(N):
        for j in range(i, N):
            val = rbf_kernel(X_train[i], X_train[j], gamma=gamma)
            K_matrix[i, j] = val
            K_matrix[j, i] = val

    unique_labels = np.unique(y_train)
    if len(unique_labels) < 2:
        return int(unique_labels[0]), K_matrix.tolist(), [1.0] * N

    y_target = np.where(y_train == unique_labels[0], -1.0, 1.0)
    K_reg = K_matrix + alpha * np.eye(N)
    weights = np.linalg.solve(K_reg, y_target)

    query_sims = np.zeros(N)
    for i in range(N):
        query_sims[i] = rbf_kernel(X_train[i], query, gamma=gamma)

    score = float(np.dot(weights, query_sims))
    pred = int(unique_labels[0] if score <= 0 else unique_labels[1])

    return pred, score, K_matrix.tolist(), query_sims.tolist()


def run_classical_kernel_wrapper_paths(train_data_path, train_labels_path, query_path, gamma=0.5):
    X_train = np.loadtxt(train_data_path, delimiter=',')
    y_train = np.loadtxt(train_labels_path, delimiter=',')
    query_arr = np.loadtxt(query_path, delimiter=',')

    if X_train.ndim == 1:
        X_train = X_train.reshape(1, -1)

    y_train = np.atleast_1d(y_train).astype(int)
    query_arr = np.atleast_1d(query_arr).astype(float)

    pred, score, K_mat, query_sims = classical_kernel_estimation(
        X_train, y_train, query_arr, gamma=float(gamma)
    )

    return {
        "prediction": pred,
        "score": score,
        "kernel_matrix": K_mat,
        "query_similarities": query_sims
    }
