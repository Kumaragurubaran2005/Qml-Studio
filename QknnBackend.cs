using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace QML_Studio
{
    public static class QknnBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_qknn_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int k,
            int shots
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        public static string RunQknnPaths(string trainDataPath, string trainLabelsPath, string queryPath, int k, int shots)
        {
            IntPtr ptr = run_qknn_paths(trainDataPath, trainLabelsPath, queryPath, k, shots);

            try
            {
                string result = Marshal.PtrToStringAnsi(ptr) 
                                ?? throw new Exception("Rust returned null");
                return result;
            }
            finally
            {
                free_string(ptr);
            }
        }
    }
}
