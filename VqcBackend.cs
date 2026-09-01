using System;
using System.Runtime.InteropServices;

namespace QML_Studio
{
    public static class VqcBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_vqc_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int maxIter,
            int shots
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        public static string RunVqcPaths(string trainDataPath, string trainLabelsPath, string queryPath, int maxIter = 20, int shots = 1024)
        {
            IntPtr ptr = run_vqc_paths(trainDataPath, trainLabelsPath, queryPath, maxIter, shots);

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
