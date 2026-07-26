## 📑工具简介  
  apate是一款能够简洁、快速地对文件进行格式伪装的工具，支持GUI图形界面和CLI命令行两种操作方式。  
  开源项目主页：[**_Github: rippod/apate_**](https://github.com/rippod/apate)  
  
## ⭐软件特性  
  1. 支持超大文件，可以做到瞬间伪装/还原完毕，无需任何等待。  
  2. 针对文件的还原做了优化，无需知道文件的伪装面具即可一键还原。  
  3. 针对真身文件的原始文件头做了加密处理，不易被检测出原始格式。  
  4. 支持批量拖拽，支持文件夹拖拽，支持递归处理子目录。  
  5. 支持伪装检测，可判断文件是否被本工具伪装过并识别原始格式。  
  6. 支持CLI命令行操作，方便脚本化和批量处理。  
  7. 内置LZ4压缩/解压，可大幅减小文件体积。  
  8. 所有操作均为并行处理，充分利用多核CPU性能。  
  
## 📥下载方法
  1. 安装运行环境：根据自己的操作系统，安装.NET桌面运行时6.0：[**_64位安装包_**](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-6.0.16-windows-x64-installer) 或者 [**_32位安装包_**](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/runtime-desktop-6.0.16-windows-x86-installer)  
  2. 下载apate：最新版v1.5.0：[**_from Github_**](https://github.com/rippod/apate/releases) 或者 [**_from 蓝奏云_**](https://wwve.lanzoup.com/iEaSU0ymznza)  
  
## 📗使用说明（GUI模式）  
  双击运行 `apate.exe`（不带任何参数）即启动图形界面。  
  
  ### 1. 一键伪装  
  使用预置面具文件，对真身文件进行伪装。伪装后，真身文件看起来与面具文件一样。适用大部分应用场景。  
  ### 2. 面具伪装  
  使用自定义面具文件，对真身文件进行伪装。伪装后，真身文件看起来与面具文件一样。适用范围取决于面具文件的选择，建议在一键伪装失效时尝试使用。  
  ### 3. 简易伪装  
  不使用面具文件，而是使用指定格式的二进制特征文件头，对真身文件进行伪装。伪装后，真身文件对于操作系统来说已经是指定格式，只是无法被双击执行或播放。支持格式：EXE、JPG、MP4、MOV。  
  ### 4. 添加后缀  
  仅修改文件扩展名，不修改文件内容。支持批量添加MP4或ZIP后缀。  
  ### 5. 伪装检测  
  检测拖入的文件是否被本工具伪装过，不会修改文件内容。可识别原始文件格式（ZIP、EXE、JPG、PNG、PDF、MP4等）。  
  ### 6. LZ4压缩/解压  
  使用LZ4算法压缩文件以减小体积，或解压还原。  
  
## 🖥️使用说明（CLI模式）  
  在命令行中运行 `apate.exe` 并带上参数即进入CLI模式。  
  
  ```
  用法：apate <命令> <路径> [选项]
  
  命令：
    disguise, d     伪装文件
    reveal, r       还原文件
    detect          检测文件是否被伪装
    suffix, s       批量添加文件后缀
    compress, c     LZ4压缩
    decompress, dc  LZ4解压
    help, -h        显示帮助
  ```
  
  ### 伪装选项  
  | 选项 | 说明 |
  |------|------|
  | `--mask, -m <文件>` | 指定自定义面具文件 |
  | `--mode <模式>` | 伪装模式：onekey(默认)/exe/jpg/mp4/mov |
  
  ### CLI示例  
  ```bash
  # 一键伪装（默认MP4面具）
  apate disguise C:\secret.zip
  
  # 简易伪装为EXE
  apate disguise C:\secret.zip --mode exe
  
  # 自定义面具伪装整个文件夹
  apate disguise C:\folder --mask C:\mask.mp4
  
  # 还原
  apate reveal C:\secret.zip.mp4
  
  # 检测是否被伪装
  apate detect C:\folder
  
  # 批量添加后缀
  apate suffix C:\folder .mp4
  
  # LZ4压缩/解压
  apate compress C:\data.bin
  apate decompress C:\data.bin.lz4
  ```
  
  > 说明：路径支持文件或文件夹，文件夹会递归处理所有子文件。所有操作均为并行处理。  
  
## ❗注意事项  
  1. 使用前请务必做好数据备份。  
  2. 本软件不得用于商业用途，仅做学习交流。  
  3. 本软件不得用于非法用途，用户使用本软件导致的任何后果均由用户本人承担，软件作者不承担任何责任。  
  
## 🙋FAQ  
### 1. copy /b a.jpg+b.zip 原理是这个吗？  
  技术上不同，但有类似之处。比copy命令伪装更快速、还原更方便，具体特性请参阅[软件特性](#软件特性)章节。  
### 2. 一键伪装只支持单一的mp4格式，建议增加更多选项  
  由于mp4格式适用范围最广，所以暂不考虑增加其他选项。如果需要使用其他格式，可以使用面具伪装模式，自定义面具文件。  
### 3. 如何判断一个文件是否被伪装过？  
  使用伪装检测功能：GUI模式下选择「选项→伪装检测」后拖入文件；CLI模式下运行 `apate detect <路径>`。  
  
## 🆕更新记录  
  ### v1.5.0  
    feat: 新增伪装检测功能，支持识别原始文件格式。  
    feat: 新增CLI命令行接口，支持全部功能的命令行操作。  
    feat: 新增批量添加后缀功能。  
    feat: 新增LZ4压缩/解压功能。  
    perf: 所有操作改为并行处理，支持多核CPU加速。  
  ### v1.4.2  
    bugfix: 优化界面布局，修复DPI改变时界面布局混乱的bug。  
