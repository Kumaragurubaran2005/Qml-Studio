using System;
using System.Runtime.InteropServices;

namespace QML_Studio
{
    public static class QuantumKernelBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_quantum_kernel_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int shots
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        public static string RunQuantumKernelPaths(string trainDataPath, string trainLabelsPath, string queryPath, int shots = 2048)
        {
            IntPtr ptr = run_quantum_kernel_paths(trainDataPath, trainLabelsPath, queryPath, shots);

            try
            {
                string result = Marshal.PtrToStringAnsi(ptr) 
                                ?? throw new Exception("Rust returned null pointer");
                return result;
            }
            finally
            {
                free_string(ptr);
            }
        }
    }
}
