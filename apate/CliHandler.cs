using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace apate
{
    /// <summary>
    /// 命令行接口处理器
    /// </summary>
    internal static class CliHandler
    {
        /// <summary>
        /// 执行CLI命令
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>退出码：0成功，1失败</returns>
        public static int Run(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "disguise":
                case "d":
                    return HandleDisguise(args);
                case "reveal":
                case "r":
                    return HandleReveal(args);
                case "detect":
                    return HandleDetect(args);
                case "compress":
                case "c":
                    return HandleCompress(args);
                case "decompress":
                case "dc":
                    return HandleDecompress(args);
                case "suffix":
                case "s":
                    return HandleSuffix(args);
                case "help":
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
                default:
                    Console.WriteLine($"错误：未知命令 \"{command}\"");
                    Console.WriteLine("使用 \"apate help\" 查看可用命令。");
                    return 1;
            }
        }

        /// <summary>
        /// 处理伪装命令
        /// </summary>
        private static int HandleDisguise(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("错误：请指定要伪装的文件或文件夹路径。");
                Console.WriteLine("用法：apate disguise <路径> [选项]");
                return 1;
            }

            string targetPath = args[1];
            string maskFile = null;
            string mode = "onekey"; // 默认一键伪装

            // 解析选项
            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--mask":
                    case "-m":
                        if (i + 1 < args.Length)
                        {
                            maskFile = args[++i];
                            mode = "mask";
                        }
                        else
                        {
                            Console.WriteLine("错误：--mask 后需指定面具文件路径。");
                            return 1;
                        }
                        break;
                    case "--mode":
                        if (i + 1 < args.Length)
                        {
                            mode = args[++i].ToLower();
                        }
                        else
                        {
                            Console.WriteLine("错误：--mode 后需指定模式（onekey/exe/jpg/mp4/mov）。");
                            return 1;
                        }
                        break;
                    default:
                        Console.WriteLine($"警告：忽略未知选项 \"{args[i]}\"");
                        break;
                }
            }

            // 确定面具字节和扩展名
            byte[] maskBytes;
            string maskExtension;

            if (mode == "mask" && maskFile != null)
            {
                if (!File.Exists(maskFile))
                {
                    Console.WriteLine($"错误：面具文件不存在：{maskFile}");
                    return 1;
                }
                FileInfo maskInfo = new FileInfo(maskFile);
                if (maskInfo.Length > Program.maximumMaskLength)
                {
                    Console.WriteLine($"错误：面具文件过大（最大 {Program.maximumMaskLength / 1024 / 1024}MB）。");
                    return 1;
                }
                maskBytes = Program.FileToBytes(maskFile);
                maskExtension = maskInfo.Extension;
            }
            else
            {
                switch (mode)
                {
                    case "onekey":
                        maskBytes = Properties.Resources.mask;
                        maskExtension = ".mp4";
                        break;
                    case "exe":
                        maskBytes = Program.exeHead;
                        maskExtension = ".exe";
                        break;
                    case "jpg":
                        maskBytes = Program.jpgHead;
                        maskExtension = ".jpg";
                        break;
                    case "mp4":
                        maskBytes = Program.mp4Head;
                        maskExtension = ".mp4";
                        break;
                    case "mov":
                        maskBytes = Program.movHead;
                        maskExtension = ".mov";
                        break;
                    default:
                        Console.WriteLine($"错误：未知伪装模式 \"{mode}\"。可用模式：onekey/exe/jpg/mp4/mov");
                        return 1;
                }
            }

            // 收集文件
            List<string> files = Program.GetAllFilesRecursively(targetPath);
            if (files.Count == 0)
            {
                Console.WriteLine($"错误：未找到文件：{targetPath}");
                return 1;
            }

            Console.WriteLine($"伪装模式：{mode}，目标文件数：{files.Count}");
            Console.WriteLine("正在处理...");

            int successCount = 0;
            int failCount = 0;
            object lockObj = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (filePath) =>
            {
                try
                {
                    if (Program.Disguise(filePath, maskBytes) == 1)
                    {
                        File.Move(filePath, filePath + maskExtension);
                        lock (lockObj) { successCount++; }
                    }
                    else
                    {
                        lock (lockObj) { failCount++; }
                    }
                }
                catch (Exception)
                {
                    lock (lockObj) { failCount++; }
                }
            });

            Console.WriteLine($"完成！成功: {successCount}，失败: {failCount}");
            return failCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 处理还原命令
        /// </summary>
        private static int HandleReveal(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("错误：请指定要还原的文件或文件夹路径。");
                Console.WriteLine("用法：apate reveal <路径> [--force]");
                return 1;
            }

            string targetPath = args[1];
            bool force = false;

            // 解析选项
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--force" || args[i].ToLower() == "-f")
                {
                    force = true;
                }
            }

            List<string> files = Program.GetAllFilesRecursively(targetPath);
            if (files.Count == 0)
            {
                Console.WriteLine($"错误：未找到文件：{targetPath}");
                return 1;
            }

            Console.WriteLine($"目标文件数：{files.Count}");
            if (!force)
            {
                Console.WriteLine("安全模式：仅还原检测到伪装的文件（使用 --force 跳过预检）");
            }
            else
            {
                Console.WriteLine("警告：强制模式，将对所有文件执行还原（可能损坏未伪装的文件）");
            }
            Console.WriteLine("正在还原...");

            int successCount = 0;
            int failCount = 0;
            int skipCount = 0;
            object lockObj = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (filePath) =>
            {
                try
                {
                    // 安全预检：先检测文件是否被伪装
                    if (!force)
                    {
                        string detectResult = Program.DetectDisguise(filePath);
                        if (detectResult == null)
                        {
                            lock (lockObj) { skipCount++; }
                            return;
                        }
                    }

                    if (Program.Reveal(filePath) == 1)
                    {
                        string newPath = filePath.Substring(0, filePath.LastIndexOf('.'));
                        File.Move(filePath, newPath);
                        lock (lockObj) { successCount++; }
                    }
                    else
                    {
                        lock (lockObj) { failCount++; }
                    }
                }
                catch (Exception)
                {
                    lock (lockObj) { failCount++; }
                }
            });

            Console.WriteLine($"完成！成功: {successCount}，失败: {failCount}，跳过(未检测到伪装): {skipCount}");
            return failCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 处理检测命令
        /// </summary>
        private static int HandleDetect(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("错误：请指定要检测的文件或文件夹路径。");
                Console.WriteLine("用法：apate detect <路径>");
                return 1;
            }

            string targetPath = args[1];
            List<string> files = Program.GetAllFilesRecursively(targetPath);
            if (files.Count == 0)
            {
                Console.WriteLine($"错误：未找到文件：{targetPath}");
                return 1;
            }

            Console.WriteLine($"目标文件数：{files.Count}");
            Console.WriteLine("正在检测...");
            Console.WriteLine(new string('-', 60));

            int disguisedCount = 0;
            int normalCount = 0;
            object lockObj = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (filePath) =>
            {
                try
                {
                    string result = Program.DetectDisguise(filePath);
                    lock (lockObj)
                    {
                        if (result != null)
                        {
                            disguisedCount++;
                            Console.WriteLine($"[伪装] {filePath}");
                            Console.WriteLine($"       {result}");
                        }
                        else
                        {
                            normalCount++;
                        }
                    }
                }
                catch (Exception)
                {
                    lock (lockObj)
                    {
                        normalCount++;
                        Console.WriteLine($"[错误] {filePath} - 无法读取");
                    }
                }
            });

            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"检测完成！共 {files.Count} 个文件，伪装: {disguisedCount}，正常: {normalCount}");
            return 0;
        }

        /// <summary>
        /// 处理LZ4压缩命令
        /// </summary>
        private static int HandleCompress(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("错误：请指定要压缩的文件或文件夹路径。");
                Console.WriteLine("用法：apate compress <路径>");
                return 1;
            }

            string targetPath = args[1];
            List<string> files = Program.GetAllFilesRecursively(targetPath);
            if (files.Count == 0)
            {
                Console.WriteLine($"错误：未找到文件：{targetPath}");
                return 1;
            }

            Console.WriteLine($"目标文件数：{files.Count}");
            Console.WriteLine("正在压缩...");

            int successCount = 0;
            int failCount = 0;
            object lockObj = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (filePath) =>
            {
                try
                {
                    if (Program.CompressWithLZ4(filePath) == 1)
                    {
                        File.Move(filePath, filePath + ".lz4");
                        lock (lockObj) { successCount++; }
                    }
                    else
                    {
                        lock (lockObj) { failCount++; }
                    }
                }
                catch (Exception)
                {
                    lock (lockObj) { failCount++; }
                }
            });

            Console.WriteLine($"完成！成功: {successCount}，失败: {failCount}");
            return failCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 处理LZ4解压命令
        /// </summary>
        private static int HandleDecompress(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("错误：请指定要解压的文件或文件夹路径。");
                Console.WriteLine("用法：apate decompress <路径>");
                return 1;
            }

            string targetPath = args[1];
            List<string> files = Program.GetAllFilesRecursively(targetPath);
            if (files.Count == 0)
            {
                Console.WriteLine($"错误：未找到文件：{targetPath}");
                return 1;
            }

            Console.WriteLine($"目标文件数：{files.Count}");
            Console.WriteLine("正在解压...");

            int successCount = 0;
            int failCount = 0;
            object lockObj = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (filePath) =>
            {
                try
                {
                    if (filePath.ToLower().EndsWith(".lz4"))
                    {
                        string newFilePath = filePath.Substring(0, filePath.Length - 4);
                        File.Copy(filePath, newFilePath, true);
                        if (Program.DecompressWithLZ4(newFilePath) == 1)
                        {
                            lock (lockObj) { successCount++; }
                        }
                        else
                        {
                            File.Delete(newFilePath);
                            lock (lockObj) { failCount++; }
                        }
                    }
                    else
                    {
                        lock (lockObj) { failCount++; }
                    }
                }
                catch (Exception)
                {
                    lock (lockObj) { failCount++; }
                }
            });

            Console.WriteLine($"完成！成功: {successCount}，失败: {failCount}");
            return failCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 处理批量添加后缀命令
        /// </summary>
        private static int HandleSuffix(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("错误：请指定文件路径和后缀。");
                Console.WriteLine("用法：apate suffix <路径> <后缀>");
                Console.WriteLine("示例：apate suffix C:\\folder .mp4");
                return 1;
            }

            string targetPath = args[1];
            string extension = args[2];

            // 确保后缀以点开头
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            List<string> files = Program.GetAllFilesRecursively(targetPath);
            if (files.Count == 0)
            {
                Console.WriteLine($"错误：未找到文件：{targetPath}");
                return 1;
            }

            Console.WriteLine($"目标文件数：{files.Count}，添加后缀：{extension}");
            Console.WriteLine("正在处理...");

            int successCount = 0;
            int failCount = 0;
            object lockObj = new object();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, (filePath) =>
            {
                try
                {
                    File.Move(filePath, filePath + extension);
                    lock (lockObj) { successCount++; }
                }
                catch (Exception)
                {
                    lock (lockObj) { failCount++; }
                }
            });

            Console.WriteLine($"完成！成功: {successCount}，失败: {failCount}");
            return failCount > 0 ? 1 : 0;
        }

        /// <summary>
        /// 打印帮助信息
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine(@"apate - 文件格式伪装工具 (CLI模式)

用法：apate <命令> <路径> [选项]

命令：
  disguise, d     伪装文件（在文件头部覆盖面具，末尾追加原始头）
  reveal, r       还原文件（恢复被伪装的文件）
  detect          检测文件是否被伪装
  suffix, s       批量添加文件后缀（不修改文件内容）
  compress, c     使用LZ4算法压缩文件
  decompress, dc  解压LZ4压缩的文件
  help, -h        显示此帮助信息

伪装选项：
  --mask, -m <文件>    指定自定义面具文件（面具伪装模式）
  --mode <模式>        指定伪装模式，可选值：
                         onekey  一键伪装（默认，使用预置MP4面具）
                         exe     简易伪装为EXE
                         jpg     简易伪装为JPG
                         mp4     简易伪装为MP4
                         mov     简易伪装为MOV

还原选项：
  --force, -f          跳过安全预检，强制对所有文件执行还原
                       默认会先检测文件是否被伪装，未检测到伪装的文件将被跳过

示例：
  apate disguise C:\secret.zip
      使用默认MP4面具伪装文件

  apate disguise C:\secret.zip --mode exe
      将文件简易伪装为EXE格式

  apate disguise C:\folder --mask C:\mask.mp4
      使用自定义面具文件伪装整个文件夹

  apate reveal C:\secret.zip.mp4
      还原被伪装的文件（自动预检，未伪装的文件会被跳过）

  apate reveal C:\folder --force
      强制还原，跳过预检（慎用，可能损坏未伪装的文件）

  apate detect C:\folder
      检测文件夹下所有文件是否被伪装

  apate compress C:\data.bin
      LZ4压缩文件

  apate decompress C:\data.bin.lz4
      LZ4解压文件

  apate suffix C:\folder .mp4
      批量给文件夹下所有文件添加.mp4后缀

  apate suffix C:\file.zip .7z
      给文件添加.7z后缀

说明：
  - 路径支持文件或文件夹，文件夹会递归处理所有子文件
  - 无参数运行时启动图形界面（GUI）
  - 所有操作均为并行处理，支持批量操作");
        }
    }
}
