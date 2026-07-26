using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using K4os.Compression.LZ4;

namespace apate
{
    internal static class Program
    {
        public static byte[] fileHead = new byte[] { };
        public static int maximumMaskLength = 2147483647/7;//2GB/7=约300MB
        public static int maskLengthIndicatorLength = 4;//存储面具长度的标记，长度为4个字节，可表示4GB的文件长度
        public static byte[] jpgHead = new byte[] { 0xff, 0xd8, 0xff, 0xe1 };
        public static byte[] movHead = new byte[] { 0x6d, 0x6f, 0x6f, 0x76 };
        public static byte[] mp4Head = new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x69,
            0x73, 0x6F, 0x6D, 0x00, 0x00, 0x02, 0x00, 0x69, 0x73, 0x6F, 0x6D, 0x69, 0x73, 0x6F, 0x32, 0x61, 
            0x76, 0x63, 0x31, 0x6D, 0x70, 0x34, 0x31 };
        public static byte[] exeHead = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04,
            0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD, 0x21,
            0xB8, 0x01, 0x4C, 0xCD, 0x21, 0x54, 0x68, 0x69, 0x73, 0x20, 0x70, 0x72, 0x6F, 0x67, 0x72, 0x61,
            0x6D, 0x20, 0x63, 0x61, 0x6E, 0x6E, 0x6F, 0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6E, 0x20,
            0x69, 0x6E, 0x20, 0x44, 0x4F, 0x53, 0x20, 0x6D, 0x6F, 0x64, 0x65, 0x2E, 0x0D, 0x0D, 0x0A, 0x24,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.Run(new ApateUI());
        }

        /// <summary>
        /// 伪装文件
        /// </summary>
        /// <param name="filePath">真身文件路径</param>
        /// <param name="maskHead">面具（字节形式）</param>
        /// <returns>成功返回1，失败返回-1</returns>
        public static int Disguise(string filePath,byte[] maskHead)
        {

            FileStream myStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            BinaryWriter myWriter = new BinaryWriter(myStream);
            BinaryReader myReader = new BinaryReader(myStream);
            try
            {
                byte[] originalHead;
                if (new FileInfo(filePath).Length >= maskHead.Length)//正常情况下：真实文件的长度大于面具长度，以面具的长度读取真实文件的头部信息
                {
                    originalHead = myReader.ReadBytes(maskHead.Length);
                }
                else//非正常情况：真实文件长度还没有面具文件长度大
                {
                    originalHead = myReader.ReadBytes(Convert.ToInt32(new FileInfo(filePath).Length));
                }
                myWriter.Seek(0, SeekOrigin.Begin);
                myWriter.Write(maskHead);
                myWriter.Seek(0, SeekOrigin.End);
                myWriter.Write(ReverseByteArray(originalHead));
                //使用最后的若干字节记录面具文件长度
                myWriter.Write(IntToBytes(maskHead.Length));
                myWriter.Close();
                myReader.Close();
                myStream.Close();
                return 1;
            }catch (Exception) {
                myWriter.Close();
                myReader.Close();
                myStream.Close();
                return -1;
            }
        }
        //还原
        /// <summary>
        /// 还原文件
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
                myReader.BaseStream.Position = disguisedFileInfo.Length - maskLengthIndicatorLength;
                int maskHeadLength = BytesToInt(myReader.ReadBytes(maskLengthIndicatorLength));
                byte[] originalHead;
                //正常情况下，面具长度小于真实文件长度
                if (maskHeadLength <= (disguisedFileInfo.Length - maskLengthIndicatorLength - maskHeadLength))
                {
                    myReader.BaseStream.Position = disguisedFileInfo.Length - maskLengthIndicatorLength - maskHeadLength;
                    originalHead = myReader.ReadBytes(maskHeadLength);
                }
                else//非正常情况下，面具长度大于真实文件长度
                {
                    myReader.BaseStream.Position = maskHeadLength;
                    originalHead = myReader.ReadBytes(Convert.ToInt32(disguisedFileInfo.Length - maskLengthIndicatorLength - maskHeadLength));
                }
                myWriter.BaseStream.SetLength(myWriter.BaseStream.Length - maskHeadLength - maskLengthIndicatorLength);//把文件末尾多余的部分截掉
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
        /// 将字节数组逆序排列
        /// </summary>
        /// <param name="buffer">目标字节数组</param>
        /// <returns>逆序字节数组</returns>
        private static byte[] ReverseByteArray(byte[] buffer)
        {
            byte[] result = new byte[buffer.Length];
            for (int i = 0;i< buffer.Length; i++)
            {
                result[i]= buffer[i];
            }
            Array.Reverse(result);
            return result;

        }
        /// <summary>
        /// 递归遍历目标路径，得到所有的文件
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <returns>目标路径下所有的文件</returns>
        public static List<string> GetAllFilesRecursively(string path)
        {
            List<string> files = new List<string>();
            if(Directory.Exists(path))
            {
                List<string> subDirFiles = new List<string>(Directory.GetFiles(path));
                for(int i = 0;i<subDirFiles.Count; i++)
                {
                    files.Add(subDirFiles[i]);
                }
                List<string> subDirDirectories = new List<string>(Directory.GetDirectories(path));
                for(int i=0;i<subDirDirectories.Count; i++)
                {
                    List<string> tmp = GetAllFilesRecursively(subDirDirectories[i]);
                    files.AddRange(tmp);
                }
                
            }
            else if(File.Exists(path))
            {
                files.Add(path);
            }
            return files;
        }
        /// <summary>
        /// 将int转换为字节数组
        /// </summary>
        /// <param name="intLength"></param>
        /// <returns></returns>
        private static byte[] IntToBytes(int intLength)
        {
            byte[] result = BitConverter.GetBytes(intLength);
            return result;
        }
        /// <summary>
        /// 将字节数组转换为int
        /// </summary>
        /// <param name="byteLength"></param>
        /// <returns></returns>
        private static int BytesToInt(byte[] byteLength)
        {
            int result = BitConverter.ToInt32(byteLength, 0);
            return result;
        }
        /// <summary>
        /// 将文件转换为字节数组，大小受限于maximumMaskLength的限制，如果超出大小限制，则返回的数组长度为0
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <returns>目标文件转换的字节数组，如果超出大小限制，则返回的数组长度为0</returns>
        public static byte[] FileToBytes(string filePath)
        {
            byte[] bytes = new byte[] { };
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 0 && fileInfo.Length < maximumMaskLength)
            {
                bytes = File.ReadAllBytes(filePath);
            }
            return bytes;
        }

        /// <summary>
        /// 检测文件是否被伪装
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <returns>检测结果描述字符串，如果未检测到伪装则返回null</returns>
        public static string DetectDisguise(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                // 文件太小，不可能是伪装文件（至少需要面具+原始头+4字节标记）
                if (fileInfo.Length < maskLengthIndicatorLength + 2)
                {
                    return null;
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // 读取末尾4字节，获取面具长度标记
                    fs.Position = fileInfo.Length - maskLengthIndicatorLength;
                    int maskHeadLength = BytesToInt(reader.ReadBytes(maskLengthIndicatorLength));

                    // 验证面具长度是否合理
                    if (maskHeadLength <= 0 || maskHeadLength > maximumMaskLength)
                    {
                        return null;
                    }

                    // 验证文件总长度是否合理：文件长度应 >= 面具长度 + 原始头长度 + 标记长度
                    // 即 fileInfo.Length >= maskHeadLength + maskHeadLength + 4（正常情况下原始头长度等于面具长度）
                    // 或者至少 fileInfo.Length > maskHeadLength + 4
                    if (fileInfo.Length <= maskHeadLength + maskLengthIndicatorLength)
                    {
                        return null;
                    }

                    // 读取末尾的原始文件头（反转存储的）
                    long originalHeadPosition = fileInfo.Length - maskLengthIndicatorLength - maskHeadLength;
                    if (originalHeadPosition < maskHeadLength)
                    {
                        // 非正常情况：面具长度大于真实文件长度
                        originalHeadPosition = maskHeadLength;
                    }
                    fs.Position = originalHeadPosition;
                    int originalHeadLength = (int)(fileInfo.Length - maskLengthIndicatorLength - originalHeadPosition);
                    if (originalHeadLength <= 0 || originalHeadLength > maskHeadLength)
                    {
                        originalHeadLength = maskHeadLength;
                    }
                    byte[] reversedOriginalHead = reader.ReadBytes(originalHeadLength);
                    byte[] originalHead = ReverseByteArray(reversedOriginalHead);

                    // 尝试识别原始文件格式
                    string detectedFormat = IdentifyFileFormat(originalHead);

                    if (detectedFormat != null)
                    {
                        return "检测到伪装！原始格式可能为: " + detectedFormat + "，面具长度: " + maskHeadLength + " 字节";
                    }
                    else
                    {
                        // 虽然无法识别原始格式，但末尾标记结构合理，仍可能是伪装文件
                        // 进一步检查：面具长度标记是否合理（不超过文件一半）
                        if (maskHeadLength <= (fileInfo.Length - maskLengthIndicatorLength) / 2)
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
        /// 根据文件头识别文件格式
        /// </summary>
        /// <param name="fileHead">文件头字节数组</param>
        /// <returns>文件格式描述，无法识别返回null</returns>
        private static string IdentifyFileFormat(byte[] fileHead)
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

            // MP4: 检查ftyp标记（偏移4-7字节为66 74 79 70）
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

            // Word(.docx)/Excel(.xlsx)/PPT(.pptx) 本质是ZIP
            // 已在ZIP中检测

            // MP3: FF FB 或 FF F3 或 FF F2 或 ID3 (49 44 33)
            if (fileHead.Length >= 3 && fileHead[0] == 0x49 && fileHead[1] == 0x44 && fileHead[2] == 0x33)
                return "MP3";
            if (fileHead.Length >= 2 && fileHead[0] == 0xFF && (fileHead[1] == 0xFB || fileHead[1] == 0xF3 || fileHead[1] == 0xF2))
                return "MP3";

            // Word(.doc): D0 CF 11 E0
            if (fileHead.Length >= 4 && fileHead[0] == 0xD0 && fileHead[1] == 0xCF && fileHead[2] == 0x11 && fileHead[3] == 0xE0)
                return "DOC/OLE";

            return null;
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
                
                // 创建一个内存流用于存储压缩后的数据
                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(compressedStream))
                    {
                        // 写入原始文件大小，用于解压时使用
                        writer.Write(fileData.Length);
                        
                        // 压缩数据
                        byte[] compressedData = LZ4Pickler.Pickle(fileData);
                        
                        // 写入压缩后的数据
                        writer.Write(compressedData);
                    }
                    
                    // 将压缩后的数据写回文件
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
                        // 读取原始文件大小
                        int originalSize = reader.ReadInt32();
                        
                        // 读取压缩后的数据
                        byte[] compressedData = reader.ReadBytes((int)compressedStream.Length - 4); // 4 bytes for int32
                        
                        // 解压数据
                        byte[] decompressedData = LZ4Pickler.Unpickle(compressedData);
                        
                        // 检查解压后的数据长度是否与原始大小匹配
                        if (decompressedData.Length != originalSize)
                        {
                            return -1;
                        }
                        
                        // 将解压后的数据写回文件
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
    }
}
