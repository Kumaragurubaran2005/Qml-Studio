using System;
using System.Runtime.InteropServices;

namespace QML_Studio
{
    public static class QnnBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_qnn_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int epochs,
            double lr,
            int shots
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        public static string RunQnnPaths(string trainDataPath, string trainLabelsPath, string queryPath, int epochs = 15, double lr = 0.1, int shots = 1024)
        {
            IntPtr ptr = run_qnn_paths(trainDataPath, trainLabelsPath, queryPath, epochs, lr, shots);

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
