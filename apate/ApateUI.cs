namespace apate
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public partial class ApateUI : Form
    {
        private byte[] maskBytes = new byte[] { };
        private string maskExtension = "";
        private string customExtension = "";//用户自定义后缀
        //private string maskFilePath;
        public ApateUI()
        {
            InitializeComponent();
            ModeSelect(ModeEnum.OneKey);
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutUI aboutUI = new AboutUI();
            //如果主窗口是置顶，则弹窗也置顶
            if (TopMost == true)
            {
                aboutUI.TopMost = true;
            }
            else
            {
                aboutUI.TopMost = false;
            }
            aboutUI.ShowDialog();
        }

        private void MaskFileLabel_DragDrop(object sender, DragEventArgs e)
        {
            toolStripStatusLabel1.Text = "正在处理";
            Array fileObjectArray = (Array)e.Data.GetData(DataFormats.FileDrop);
            List<string> filePathList = new List<string>();
            for (int i = 0; i < fileObjectArray.Length; i++)
            {
                filePathList.Add(fileObjectArray.GetValue(i).ToString());
            }
            if (filePathList.Count > 1)//如果拖入了多份文件
            {
                toolStripStatusLabel1.Text = "仅可拖入1份面具文件，请更换其他面具文件并重新拖入";
                return;
            }
            else if (Directory.Exists(filePathList[0]))//如果拖入的是文件夹
            {
                toolStripStatusLabel1.Text = "不支持将文件夹作为面具，请更换其他面具文件并重新拖入";
                return;
            }
            if (new FileInfo(filePathList[0]).Length > Program.maximumMaskLength)//文件长度检测
            {
                toolStripStatusLabel1.Text = "面具文件过大，请更换其他面具文件（小于" + Program.maximumMaskLength / 1024 / 1024 + "MB）并重新拖入";
                return;
            }
            maskBytes = Program.FileToBytes(filePathList[0]);//将文件转换为字节数组
            maskExtension = new FileInfo(filePathList[0]).Extension;//保存文件扩展名
            if (maskBytes.Length == 0)
            {
                toolStripStatusLabel1.Text = "未知错误，请更换其他面具文件并重新拖入";
            }
            toolStripStatusLabel1.Text = "完成！";
            //激活伪装区
            MaskFileDragLabel.Image = Properties.Resources.accept;
            MaskFileDragLabel.Text = "";
            TrueFileDragLabel.AllowDrop = true;
            TrueFileDragLabel.Image = Properties.Resources.drag;
            TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n进行伪装";
        }

        private void MaskFileLabel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void TrueFileLabel_DragDrop(object sender, DragEventArgs e)
        {
            toolStripStatusLabel1.Text = "正在处理";
            Array fileObjectArray = (Array)e.Data.GetData(DataFormats.FileDrop);
            List<string> filePathList = new List<string>();
            for (int i = 0; i < fileObjectArray.Length; i++)
            {
                filePathList.AddRange(Program.GetAllFilesRecursively(fileObjectArray.GetValue(i).ToString()));//递归遍历文件夹，并将文件添加到filePathList
            }

            // 伪装检测模式
            if (伪装检测ToolStripMenuItem.Checked)
            {
                int disguisedCount = 0;
                int normalCount = 0;
                List<string> results = new List<string>();
                
                object lockObj = new object();
                
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                };
                
                Parallel.ForEach(filePathList, options, (filePath) =>
                {
                    try
                    {
                        string result = Program.DetectDisguise(filePath);
                        lock (lockObj)
                        {
                            if (result != null)
                            {
                                disguisedCount++;
                                results.Add("[伪装] " + Path.GetFileName(filePath) + " - " + result);
                            }
                            else
                            {
                                normalCount++;
                                results.Add("[正常] " + Path.GetFileName(filePath));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        lock (lockObj)
                        {
                            normalCount++;
                            results.Add("[错误] " + Path.GetFileName(filePath) + " - 无法读取");
                        }
                    }
                });
                
                // 显示检测结果
                string summary = "检测完成！共" + filePathList.Count + "个文件，检测到伪装: " + disguisedCount + "个，正常: " + normalCount + "个";
                toolStripStatusLabel1.Text = summary;
                
                // 如果有伪装文件，弹窗显示详细结果
                if (disguisedCount > 0)
                {
                    string detail = summary + "\r\n\r\n详细信息：\r\n" + string.Join("\r\n", results.FindAll(r => r.StartsWith("[伪装]") || r.StartsWith("[错误]")));
                    InfoBox resultBox = new InfoBox("伪装检测结果", detail);
                    if (TopMost == true)
                    {
                        resultBox.TopMost = true;
                    }
                    resultBox.ShowDialog();
                }
                return;
            }

            // LZ4压缩模式
            if (lZ4压缩ToolStripMenuItem.Checked)
            {
                int successCount = 0;
                int failCount = 0;
                
                // 使用锁对象保证线程安全
                object lockObj = new object();
                
                // 设置并行处理选项
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                };
                
                // 使用并行处理
                Parallel.ForEach(filePathList, options, (filePath) =>
                {
                    try
                    {
                        if (Program.CompressWithLZ4(filePath) == 1)
                        {
                            // 添加.lz4后缀
                            File.Move(filePath, filePath + ".lz4");
                            
                            // 线程安全地增加计数
                            lock (lockObj)
                            {
                                successCount++;
                            }
                        }
                        else
                        {
                            // 线程安全地增加计数
                            lock (lockObj)
                            {
                                failCount++;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 线程安全地增加计数
                        lock (lockObj)
                        {
                            failCount++;
                        }
                    }
                });
                
                toolStripStatusLabel1.Text = "完成！成功" + successCount + "个，失败" + failCount + "个";
                return;
            }

            // LZ4解压模式
            if (lZ4解压ToolStripMenuItem.Checked)
            {
                int successCount = 0;
                int failCount = 0;
                
                // 使用锁对象保证线程安全
                object lockObj = new object();
                
                // 设置并行处理选项
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                };
                
                // 使用并行处理
                Parallel.ForEach(filePathList, options, (filePath) =>
                {
                    try
                    {
                        // 检查文件是否有.lz4后缀
                        if (filePath.ToLower().EndsWith(".lz4"))
                        {
                            string newFilePath = filePath.Substring(0, filePath.Length - 4); // 移除.lz4后缀
                            
                            // 先复制一个副本进行解压，成功后再重命名
                            File.Copy(filePath, newFilePath, true);
                            
                            if (Program.DecompressWithLZ4(newFilePath) == 1)
                            {
                                // 线程安全地增加计数
                                lock (lockObj)
                                {
                                    successCount++;
                                }
                            }
                            else
                            {
                                // 解压失败，删除副本
                                File.Delete(newFilePath);
                                
                                // 线程安全地增加计数
                                lock (lockObj)
                                {
                                    failCount++;
                                }
                            }
                        }
                        else
                        {
                            // 文件没有.lz4后缀
                            lock (lockObj)
                            {
                                failCount++;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 线程安全地增加计数
                        lock (lockObj)
                        {
                            failCount++;
                        }
                    }
                });
                
                toolStripStatusLabel1.Text = "完成！成功" + successCount + "个，失败" + failCount + "个";
                return;
            }

            // 添加后缀模式 - 包括MP4、ZIP和自定义后缀
            if (添加MP4后缀ToolStripMenuItem.Checked || 添加ZIP后缀ToolStripMenuItem.Checked || 自定义后缀ToolStripMenuItem.Checked)
            {
                int successCount = 0;
                int failCount = 0;
                
                // 使用锁对象保证线程安全
                object lockObj = new object();
                
                // 设置并行处理选项
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                };
                
                // 使用并行处理
                Parallel.ForEach(filePathList, options, (filePath) =>
                {
                    try
                    {
                        File.Move(filePath, filePath + maskExtension);
                        
                        // 线程安全地增加计数
                        lock (lockObj)
                        {
                            successCount++;
                        }
                    }
                    catch (Exception)
                    {
                        // 线程安全地增加计数
                        lock (lockObj)
                        {
                            failCount++;
                        }
                    }
                });
                
                toolStripStatusLabel1.Text = "完成！成功" + successCount + "个，失败" + failCount + "个";
                return;
            }

            if (maskBytes.Length > 0)//如果面具文件有效
            {
                int successCount = 0;
                int failCount = 0;
                
                // 使用锁对象保证线程安全
                object lockObj = new object();
                
                // 设置并行处理选项
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                };
                
                // 使用并行处理
                Parallel.ForEach(filePathList, options, (filePath) =>
                {
                    try
                    {
                        if (Program.Disguise(filePath, maskBytes) == 1)//如果伪装成功
                        {
                            File.Move(filePath, filePath + maskExtension);
                            
                            // 线程安全地增加计数
                            lock (lockObj)
                            {
                                successCount++;
                            }
                        }
                        else//如果伪装失败
                        {
                            // 线程安全地增加计数
                            lock (lockObj)
                            {
                                failCount++;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 捕获可能的异常并安全地增加失败计数
                        lock (lockObj)
                        {
                            failCount++;
                        }
                    }
                });
                
                toolStripStatusLabel1.Text = "完成！成功" + successCount + "个，失败" + failCount + "个";
            }
            else
            {
                toolStripStatusLabel1.Text = "尚未拖入面具文件";
            }
        }

        private void TrueFileLabel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void RevealMaskLabel_DragDrop(object sender, DragEventArgs e)
        {
            Activate();
            //是否开启强制还原（跳过安全预检）
            bool forceReveal = 强制还原ToolStripMenuItem.Checked;
            //弹窗确认（根据是否强制显示不同的提示）
            string warningText;
            if (forceReveal)
            {
                warningText = "当前已开启《强制还原》，将跳过安全预检，对所有拖入的文件强制执行还原！\r\n未伪装的文件可能被严重损坏且无法恢复！\r\n是否继续？";
            }
            else
            {
                warningText = "还原操作将自动检测文件是否被伪装，未检测到伪装的文件会被跳过。\r\n但仍建议做好数据备份！\r\n是否继续？";
            }
            InfoBox infoBox = new InfoBox("注意！", warningText);
            //如果主窗口是置顶，则弹窗也置顶
            if (TopMost == true)
            {
                infoBox.TopMost = true;
            }
            else
            {
                infoBox.TopMost = false;
            }
            if (infoBox.ShowDialog() == DialogResult.Yes)
            {
                toolStripStatusLabel1.Text = "正在处理";
                Array fileObjectArray = (Array)e.Data.GetData(DataFormats.FileDrop);
                List<string> filePathList = new List<string>();
                for (int i = 0; i < fileObjectArray.Length; i++)
                {
                    filePathList.AddRange(Program.GetAllFilesRecursively(fileObjectArray.GetValue(i).ToString()));//递归遍历文件夹，并将文件添加到filePathList
                }
                
                int successCount = 0;
                int failCount = 0;
                int skipCount = 0;
                
                // 使用锁对象保证线程安全
                object lockObj = new object();
                
                // 设置并行处理选项，控制最大并行度
                // 使用处理器核心数量×2作为最大并行度
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                };
                
                // 使用并行处理
                Parallel.ForEach(filePathList, options, (filePath) =>
                {
                    try
                    {
                        // 安全预检：先检测文件是否被伪装，防止误操作损坏文件（强制模式下跳过）
                        if (!forceReveal)
                        {
                            string detectResult = Program.DetectDisguise(filePath);
                            if (detectResult == null)
                            {
                                lock (lockObj)
                                {
                                    skipCount++;
                                }
                                return;
                            }
                        }

                        if (Program.Reveal(filePath) == 1)
                        {
                            string newPath = filePath.Substring(0, filePath.LastIndexOf('.'));
                            File.Move(filePath, newPath);
                            
                            // 线程安全地增加计数
                            lock (lockObj)
                            {
                                successCount++;
                            }
                        }
                        else
                        {
                            // 线程安全地增加计数
                            lock (lockObj)
                            {
                                failCount++;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 捕获可能的异常并安全地增加失败计数
                        lock (lockObj)
                        {
                            failCount++;
                        }
                    }
                });
                
                toolStripStatusLabel1.Text = "完成！成功" + successCount + "个，失败" + failCount + "个，跳过(未检测到伪装)" + skipCount + "个";
            }
        }

        private void RevealMaskLabel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ModeSelect(ModeEnum mode)
        {
            一键伪装ToolStripMenuItem.Checked = false;
            面具伪装ToolStripMenuItem.Checked = false;
            简易伪装ToolStripMenuItem.Checked = false;
            eXEToolStripMenuItem.Checked = false;
            jPGToolStripMenuItem.Checked = false;
            mP4ToolStripMenuItem.Checked = false;
            mOVToolStripMenuItem.Checked = false;
            添加后缀ToolStripMenuItem.Checked = false;
            添加MP4后缀ToolStripMenuItem.Checked = false;
            添加ZIP后缀ToolStripMenuItem.Checked = false;
            自定义后缀ToolStripMenuItem.Checked = false;
            lZ4操作ToolStripMenuItem.Checked = false;
            lZ4压缩ToolStripMenuItem.Checked = false;
            lZ4解压ToolStripMenuItem.Checked = false;
            伪装检测ToolStripMenuItem.Checked = false;
            maskBytes = new byte[] { };
            maskExtension = "";
            MaskFileDragLabel.AllowDrop = false;
            MaskFileDragLabel.Image = Properties.Resources.cancel;
            MaskFileDragLabel.Text = "";
            TrueFileDragLabel.AllowDrop = false;
            TrueFileDragLabel.Image = Properties.Resources.cancel;
            TrueFileDragLabel.Text = "";
            switch (mode)
            {
                case ModeEnum.OneKey://一键伪装
                    一键伪装ToolStripMenuItem.Checked = true;
                    maskBytes = Properties.Resources.mask;
                    maskExtension = ".mp4";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n一键伪装";
                    break;
                case ModeEnum.Mask://面具伪装
                    面具伪装ToolStripMenuItem.Checked = true;
                    MaskFileDragLabel.AllowDrop = true;
                    MaskFileDragLabel.Image = Properties.Resources.drag;
                    MaskFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n面具文件";
                    break;
                case ModeEnum.Exe://EXE
                    简易伪装ToolStripMenuItem.Checked = true;
                    eXEToolStripMenuItem.Checked = true;
                    maskBytes = Program.exeHead;
                    maskExtension = ".exe";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n伪装为EXE";
                    break;
                case ModeEnum.Jpg://JPG
                    简易伪装ToolStripMenuItem.Checked = true;
                    jPGToolStripMenuItem.Checked = true;
                    maskBytes = Program.jpgHead;
                    maskExtension = ".jpg";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n伪装为JPG";
                    break;
                case ModeEnum.Mp4://MP4
                    简易伪装ToolStripMenuItem.Checked = true;
                    mP4ToolStripMenuItem.Checked = true;
                    maskBytes = Program.mp4Head;
                    maskExtension = ".mp4";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n伪装为MP4";
                    break;
                case ModeEnum.Mov://MOV
                    简易伪装ToolStripMenuItem.Checked = true;
                    mOVToolStripMenuItem.Checked = true;
                    maskBytes = Program.movHead;
                    maskExtension = ".mov";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n伪装为MOV";
                    break;
                case ModeEnum.AddMp4Extension://添加MP4后缀
                    添加后缀ToolStripMenuItem.Checked = true;
                    添加MP4后缀ToolStripMenuItem.Checked = true;
                    maskExtension = ".mp4";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n添加MP4后缀";
                    break;
                case ModeEnum.AddZipExtension://添加ZIP后缀
                    添加后缀ToolStripMenuItem.Checked = true;
                    添加ZIP后缀ToolStripMenuItem.Checked = true;
                    maskExtension = ".zip";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n添加ZIP后缀";
                    break;
                case ModeEnum.AddCustomExtension://添加自定义后缀
                    添加后缀ToolStripMenuItem.Checked = true;
                    自定义后缀ToolStripMenuItem.Checked = true;
                    maskExtension = customExtension;
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n添加" + customExtension + "后缀";
                    break;
                case ModeEnum.LZ4Compress://LZ4压缩
                    lZ4操作ToolStripMenuItem.Checked = true;
                    lZ4压缩ToolStripMenuItem.Checked = true;
                    maskExtension = ".lz4";
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\nLZ4压缩";
                    break;
                case ModeEnum.LZ4Decompress://LZ4解压
                    lZ4操作ToolStripMenuItem.Checked = true;
                    lZ4解压ToolStripMenuItem.Checked = true;
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\nLZ4解压";
                    break;
                case ModeEnum.Detect://伪装检测
                    伪装检测ToolStripMenuItem.Checked = true;
                    TrueFileDragLabel.AllowDrop = true;
                    TrueFileDragLabel.Image = Properties.Resources.drag;
                    TrueFileDragLabel.Text = "\r\n\r\n\r\n拖入\r\n检测伪装";
                    break;
            }
        }

        private void 一键伪装ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.OneKey);
        }

        private void 拼接伪装模式ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.Mask);
        }

        private void eXEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.Exe);

        }

        private void jPGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.Jpg);
        }

        private void mP4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.Mp4);
        }

        private void mOVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.Mov);
        }

        private void 添加MP4后缀ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.AddMp4Extension);
        }

        private void 添加ZIP后缀ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.AddZipExtension);
        }

        private void 自定义后缀ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //弹出输入框让用户输入自定义后缀
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入要添加的文件后缀（例如 .7z 或 docx）：",
                "自定义后缀",
                "");
            //用户取消或输入为空
            if (string.IsNullOrWhiteSpace(input))
            {
                return;
            }
            string ext = input.Trim();
            //确保后缀以点开头
            if (!ext.StartsWith("."))
            {
                ext = "." + ext;
            }
            customExtension = ext;
            ModeSelect(ModeEnum.AddCustomExtension);
            toolStripStatusLabel1.Text = "已设置自定义后缀：" + ext + "，拖入文件即可批量添加";
        }

        private void 窗口置顶ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopMost = !TopMost;
            窗口置顶ToolStripMenuItem.Checked = !窗口置顶ToolStripMenuItem.Checked;
        }

        private void lZ4压缩ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.LZ4Compress);
        }

        private void lZ4解压ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.LZ4Decompress);
        }

        private void 伪装检测ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ModeSelect(ModeEnum.Detect);
        }

    }
}
