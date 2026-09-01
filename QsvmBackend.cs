using System;
using System.Runtime.InteropServices;

namespace QML_Studio
{
    public static class QsvmBackend
    {
        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr run_qsvm_paths(
            string trainDataPath,
            string trainLabelsPath,
            string queryPath,
            int shots,
            double cParam
        );

        [DllImport("backend_qml.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void free_string(IntPtr ptr);

        public static string RunQsvmPaths(string trainDataPath, string trainLabelsPath, string queryPath, int shots = 2048, double cParam = 1.0)
        {
            IntPtr ptr = run_qsvm_paths(trainDataPath, trainLabelsPath, queryPath, shots, cParam);

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
