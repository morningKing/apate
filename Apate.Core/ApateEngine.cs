using System;
using System.Collections.Generic;
using System.IO;
using K4os.Compression.LZ4;

namespace Apate.Core
{
    /// <summary>
    /// Apate 文件格式伪装引擎 - 核心SDK类
    /// 提供文件伪装、还原、检测、压缩等功能
    /// </summary>
    public static class ApateEngine
    {
        /// <summary>面具最大长度限制（约300MB）</summary>
        public const int MaximumMaskLength = 2147483647 / 7;

        /// <summary>面具长度标记的字节数（4字节，可表示4GB）</summary>
        public const int MaskLengthIndicatorLength = 4;

        /// <summary>JPG文件头</summary>
        public static readonly byte[] JpgHead = new byte[] { 0xff, 0xd8, 0xff, 0xe1 };

        /// <summary>MOV文件头</summary>
        public static readonly byte[] MovHead = new byte[] { 0x6d, 0x6f, 0x6f, 0x76 };

        /// <summary>MP4文件头</summary>
        public static readonly byte[] Mp4Head = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69,
            0x73, 0x6F, 0x6D, 0x00, 0x00, 0x02, 0x00, 0x69, 0x73, 0x6F, 0x6D, 0x69, 0x73, 0x6F, 0x32, 0x61,
            0x76, 0x63, 0x31, 0x6D, 0x70, 0x34, 0x31 };

        /// <summary>EXE文件头</summary>
        public static readonly byte[] ExeHead = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04,
            0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD, 0x21,
            0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68, 0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72, 0x61,
            0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F, 0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E, 0x20,
            0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20, 0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A, 0x24,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// 伪装文件：将文件头部替换为面具字节，末尾追加反转的原始文件头和面具长度标记
        /// </summary>
        /// <param name="filePath">要伪装的文件路径</param>
        /// <param name="maskHead">面具字节数组</param>
        /// <returns>成功返回1，失败返回-1</returns>
        public static int Disguise(string filePath, byte[] maskHead)
        {
            FileStream myStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter myWriter = new BinaryWriter(myStream);
            BinaryReader myReader = new BinaryReader(myStream);
            try
            {
                byte[] originalHead;
                if (new FileInfo(filePath).Length >= maskHead.Length)
                {
                    originalHead = myReader.ReadBytes(maskHead.Length);
                }
                else
                {
                    originalHead = myReader.ReadBytes(Convert.ToInt32(new FileInfo(filePath).Length));
                }
                myWriter.Seek(0, SeekOrigin.Begin);
                myWriter.Write(maskHead);
                myWriter.Seek(0, SeekOrigin.End);
                myWriter.Write(ReverseByteArray(originalHead));
                myWriter.Write(IntToBytes(maskHead.Length));
                myWriter.Close();
                myReader.Close();
                myStream.Close();
                return 1;
            }
            catch (Exception)
            {
                myWriter.Close();
                myReader.Close();
                myStream.Close();
                return -1;
            }
        }

        /// <summary>
        /// 还原文件：恢复被伪装的文件
        /// </summary>
        /// <param name="filePath">经过伪装的文件路径</param>
        /// <returns>成功返回1，失败返回-1</returns>
        public static int Reveal(string filePath)
        {
            FileInfo disguisedFileInfo = new FileInfo(filePath);
            FileStream myStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter myWriter = new BinaryWriter(myStream);
            BinaryReader myReader = new BinaryReader(myStream);
            try
            {
                myReader.BaseStream.Position = disguisedFileInfo.Length - MaskLengthIndicatorLength;
                int maskHeadLength = BytesToInt(myReader.ReadBytes(MaskLengthIndicatorLength));
                byte[] originalHead;
                if (maskHeadLength <= (disguisedFileInfo.Length - MaskLengthIndicatorLength - maskHeadLength))
                {
                    myReader.BaseStream.Position = disguisedFileInfo.Length - MaskLengthIndicatorLength - maskHeadLength;
                    originalHead = myReader.ReadBytes(maskHeadLength);
                }
                else
                {
                    myReader.BaseStream.Position = maskHeadLength;
                    originalHead = myReader.ReadBytes(Convert.ToInt32(disguisedFileInfo.Length - MaskLengthIndicatorLength - maskHeadLength));
                }
                myWriter.BaseStream.SetLength(myWriter.BaseStream.Length - maskHeadLength - MaskLengthIndicatorLength);
                myWriter.Seek(0, SeekOrigin.Begin);
                myWriter.Write(ReverseByteArray(originalHead));
                myWriter.Close();
                myReader.Close();
                myStream.Close();
                return 1;
            }
            catch (Exception)
            {
                myWriter.Close();
                myReader.Close();
                myStream.Close();
                return -1;
            }
        }

        /// <summary>
        /// 检测文件是否被伪装
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <returns>检测结果描述字符串，未检测到伪装返回null</returns>
        public static string? DetectDisguise(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < MaskLengthIndicatorLength + 2)
                {
                    return null;
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    fs.Position = fileInfo.Length - MaskLengthIndicatorLength;
                    int maskHeadLength = BytesToInt(reader.ReadBytes(MaskLengthIndicatorLength));

                    if (maskHeadLength <= 0 || maskHeadLength > MaximumMaskLength)
                    {
                        return null;
                    }

                    if (fileInfo.Length <= maskHeadLength + MaskLengthIndicatorLength)
                    {
                        return null;
                    }

                    int originalHeadActualLength = (int)(fileInfo.Length - MaskLengthIndicatorLength - maskHeadLength);
                    if (originalHeadActualLength <= 0)
                    {
                        return null;
                    }

                    fs.Position = fileInfo.Length - MaskLengthIndicatorLength - originalHeadActualLength;
                    byte[] reversedOriginalHead = reader.ReadBytes(originalHeadActualLength);
                    byte[] originalHead = ReverseByteArray(reversedOriginalHead);

                    string? detectedFormat = IdentifyFileFormat(originalHead);

                    if (detectedFormat != null)
                    {
                        return "检测到伪装！原始格式可能为: " + detectedFormat + "，面具长度: " + maskHeadLength + " 字节";
                    }
                    else
                    {
                        fs.Position = 0;
                        byte[] fileStart = reader.ReadBytes(Math.Min(32, (int)fileInfo.Length));
                        string? maskFormat = IdentifyFileFormat(fileStart);
                        if (maskFormat != null)
                        {
                            return "疑似伪装文件，面具格式: " + maskFormat + "，面具长度: " + maskHeadLength + " 字节（无法识别原始格式）";
                        }
                        if (originalHeadActualLength < maskHeadLength)
                        {
                            return "疑似伪装文件，面具长度: " + maskHeadLength + " 字节（无法识别原始格式）";
                        }
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 使用LZ4算法压缩文件
        /// </summary>
        /// <param name="filePath">要压缩的文件路径</param>
        /// <returns>成功返回1，失败返回-1</returns>
        public static int CompressWithLZ4(string filePath)
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);

                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(compressedStream))
                    {
                        writer.Write(fileData.Length);
                        byte[] compressedData = LZ4Pickler.Pickle(fileData);
                        writer.Write(compressedData);
                    }

                    File.WriteAllBytes(filePath, compressedStream.ToArray());
                }

                return 1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// 使用LZ4算法解压文件
        /// </summary>
        /// <param name="filePath">要解压的文件路径</param>
        /// <returns>成功返回1，失败返回-1</returns>
        public static int DecompressWithLZ4(string filePath)
        {
            try
            {
                byte[] compressedFileData = File.ReadAllBytes(filePath);

                using (MemoryStream compressedStream = new MemoryStream(compressedFileData))
                {
                    using (BinaryReader reader = new BinaryReader(compressedStream))
                    {
                        int originalSize = reader.ReadInt32();
                        byte[] compressedData = reader.ReadBytes((int)compressedStream.Length - 4);
                        byte[] decompressedData = LZ4Pickler.Unpickle(compressedData);

                        if (decompressedData.Length != originalSize)
                        {
                            return -1;
                        }

                        File.WriteAllBytes(filePath, decompressedData);
                    }
                }

                return 1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// 递归获取目标路径下的所有文件
        /// </summary>
        /// <param name="path">文件或文件夹路径</param>
        /// <returns>所有文件的完整路径列表</returns>
        public static List<string> GetAllFilesRecursively(string path)
        {
            List<string> files = new List<string>();
            if (Directory.Exists(path))
            {
                files.AddRange(Directory.GetFiles(path));
                foreach (string subDir in Directory.GetDirectories(path))
                {
                    files.AddRange(GetAllFilesRecursively(subDir));
                }
            }
            else if (File.Exists(path))
            {
                files.Add(path);
            }
            return files;
        }

        /// <summary>
        /// 将文件转换为字节数组（受MaximumMaskLength限制）
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <returns>文件字节数组，超出限制返回空数组</returns>
        public static byte[] FileToBytes(string filePath)
        {
            byte[] bytes = Array.Empty<byte>();
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 0 && fileInfo.Length < MaximumMaskLength)
            {
                bytes = File.ReadAllBytes(filePath);
            }
            return bytes;
        }

        /// <summary>
        /// 根据文件头识别文件格式
        /// </summary>
        /// <param name="fileHead">文件头字节数组</param>
        /// <returns>文件格式描述，无法识别返回null</returns>
        public static string? IdentifyFileFormat(byte[] fileHead)
        {
            if (fileHead == null || fileHead.Length < 2)
                return null;

            // ZIP: PK (50 4B 03 04)
            if (fileHead.Length >= 4 && fileHead[0] == 0x50 && fileHead[1] == 0x4B && fileHead[2] == 0x03 && fileHead[3] == 0x04)
                return "ZIP";

            // EXE/DLL: MZ (4D 5A)
            if (fileHead[0] == 0x4D && fileHead[1] == 0x5A)
                return "EXE/DLL";

            // JPG: FF D8 FF
            if (fileHead.Length >= 3 && fileHead[0] == 0xFF && fileHead[1] == 0xD8 && fileHead[2] == 0xFF)
                return "JPG";

            // PNG: 89 50 4E 47
            if (fileHead.Length >= 4 && fileHead[0] == 0x89 && fileHead[1] == 0x50 && fileHead[2] == 0x4E && fileHead[3] == 0x47)
                return "PNG";

            // GIF: 47 49 46 38
            if (fileHead.Length >= 4 && fileHead[0] == 0x47 && fileHead[1] == 0x49 && fileHead[2] == 0x46 && fileHead[3] == 0x38)
                return "GIF";

            // PDF: 25 50 44 46 (%PDF)
            if (fileHead.Length >= 4 && fileHead[0] == 0x25 && fileHead[1] == 0x50 && fileHead[2] == 0x44 && fileHead[3] == 0x46)
                return "PDF";

            // MP4: ftyp标记（偏移4-7字节）
            if (fileHead.Length >= 8 && fileHead[4] == 0x66 && fileHead[5] == 0x74 && fileHead[6] == 0x79 && fileHead[7] == 0x70)
                return "MP4";

            // MOV: moov (6D 6F 6F 76)
            if (fileHead.Length >= 4 && fileHead[0] == 0x6D && fileHead[1] == 0x6F && fileHead[2] == 0x6F && fileHead[3] == 0x76)
                return "MOV";

            // RAR: 52 61 72 21
            if (fileHead.Length >= 4 && fileHead[0] == 0x52 && fileHead[1] == 0x61 && fileHead[2] == 0x72 && fileHead[3] == 0x21)
                return "RAR";

            // 7Z: 37 7A BC AF
            if (fileHead.Length >= 4 && fileHead[0] == 0x37 && fileHead[1] == 0x7A && fileHead[2] == 0xBC && fileHead[3] == 0xAF)
                return "7Z";

            // MP3: ID3 (49 44 33) 或 FF FB/F3/F2
            if (fileHead.Length >= 3 && fileHead[0] == 0x49 && fileHead[1] == 0x44 && fileHead[2] == 0x33)
                return "MP3";
            if (fileHead.Length >= 2 && fileHead[0] == 0xFF && (fileHead[1] == 0xFB || fileHead[1] == 0xF3 || fileHead[1] == 0xF2))
                return "MP3";

            // Word(.doc)/OLE: D0 CF 11 E0
            if (fileHead.Length >= 4 && fileHead[0] == 0xD0 && fileHead[1] == 0xCF && fileHead[2] == 0x11 && fileHead[3] == 0xE0)
                return "DOC/OLE";

            return null;
        }

        #region 私有辅助方法

        private static byte[] ReverseByteArray(byte[] buffer)
        {
            byte[] result = new byte[buffer.Length];
            Array.Copy(buffer, result, buffer.Length);
            Array.Reverse(result);
            return result;
        }

        private static byte[] IntToBytes(int intLength)
        {
            return BitConverter.GetBytes(intLength);
        }

        private static int BytesToInt(byte[] byteLength)
        {
            return BitConverter.ToInt32(byteLength, 0);
        }

        #endregion
    }
}
