using System;
using System.Runtime.InteropServices;

namespace QML_Studio
{
    public static class ClassicalBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_classical_knn_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int k
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_classical_svm_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            double cParam,
            double gamma
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_classical_mlp_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int epochs,
            int nHidden
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_classical_logreg_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int maxIter
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_classical_kernel_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            double gamma
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        public static string RunClassicalKnnPaths(string trainDataPath, string trainLabelsPath, string queryPath, int k = 3)
        {
            IntPtr ptr = run_classical_knn_paths(trainDataPath, trainLabelsPath, queryPath, k);
            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? throw new Exception("Rust returned null");
            }
            finally
            {
                free_string(ptr);
            }
        }

        public static string RunClassicalSvmPaths(string trainDataPath, string trainLabelsPath, string queryPath, double cParam = 1.0, double gamma = 0.5)
        {
            IntPtr ptr = run_classical_svm_paths(trainDataPath, trainLabelsPath, queryPath, cParam, gamma);
            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? throw new Exception("Rust returned null");
            }
            finally
            {
                free_string(ptr);
            }
        }

        public static string RunClassicalMlpPaths(string trainDataPath, string trainLabelsPath, string queryPath, int epochs = 25, int nHidden = 4)
        {
            IntPtr ptr = run_classical_mlp_paths(trainDataPath, trainLabelsPath, queryPath, epochs, nHidden);
            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? throw new Exception("Rust returned null");
            }
            finally
            {
                free_string(ptr);
            }
        }

        public static string RunClassicalLogRegPaths(string trainDataPath, string trainLabelsPath, string queryPath, int maxIter = 30)
        {
            IntPtr ptr = run_classical_logreg_paths(trainDataPath, trainLabelsPath, queryPath, maxIter);
            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? throw new Exception("Rust returned null");
            }
            finally
            {
                free_string(ptr);
            }
        }

        public static string RunClassicalKernelPaths(string trainDataPath, string trainLabelsPath, string queryPath, double gamma = 0.5)
        {
            IntPtr ptr = run_classical_kernel_paths(trainDataPath, trainLabelsPath, queryPath, gamma);
            try
            {
                return Marshal.PtrToStringAnsi(ptr) ?? throw new Exception("Rust returned null");
            }
            finally
            {
                free_string(ptr);
            }
        }
    }
}
