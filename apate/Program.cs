using System;
using System.IO;
using Apate.Core;

namespace apate
{
    internal static class Program
    {
        // 委托到 ApateEngine，保持向后兼容
        public static byte[] fileHead = Array.Empty<byte>();
        public static int maximumMaskLength => ApateEngine.MaximumMaskLength;
        public static int maskLengthIndicatorLength => ApateEngine.MaskLengthIndicatorLength;
        public static byte[] jpgHead => ApateEngine.JpgHead;
        public static byte[] movHead => ApateEngine.MovHead;
        public static byte[] mp4Head => ApateEngine.Mp4Head;
        public static byte[] exeHead => ApateEngine.ExeHead;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // 如果有命令行参数，进入CLI模式
            if (args.Length > 0)
            {
                return CliHandler.Run(args);
            }

            // 无参数，启动GUI模式
            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.Run(new ApateUI());
            return 0;
        }

        public static int Disguise(string filePath, byte[] maskHead) => ApateEngine.Disguise(filePath, maskHead);

        public static int Reveal(string filePath) => ApateEngine.Reveal(filePath);

        public static List<string> GetAllFilesRecursively(string path) => ApateEngine.GetAllFilesRecursively(path);

        public static byte[] FileToBytes(string filePath) => ApateEngine.FileToBytes(filePath);

        public static string DetectDisguise(string filePath) => ApateEngine.DetectDisguise(filePath);

        public static int CompressWithLZ4(string filePath) => ApateEngine.CompressWithLZ4(filePath);

        public static int DecompressWithLZ4(string filePath) => ApateEngine.DecompressWithLZ4(filePath);
    }
}
