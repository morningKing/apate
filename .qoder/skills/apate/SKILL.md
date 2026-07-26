---
name: apate
description: 调用 apate 命令行工具完成文件格式伪装处理，包括伪装文件、还原文件、检测伪装、批量改后缀、LZ4压缩解压。当用户提到“伪装文件”、“还原文件”、“检测伪装”、“改后缀”、“LZ4压缩”、“文件格式处理”、“apate”时使用此技能。
---

# Apate 文件格式伪装工具

## 环境配置

apate 是基于 .NET 6.0 的 Windows 桌面工具，支持 GUI 和 CLI 双模式。

**定位可执行文件：**
- 发布版：`publish/apate.exe` 或 `apate/bin/Release/net6.0-windows/apate.exe`
- 运行环境：需要 .NET Desktop Runtime 6.0，或配置 `DOTNET_ROOT` 指向本地 SDK

**执行前置（PowerShell）：**
```powershell
# 如果使用本地SDK（非系统安装）
$env:DOTNET_ROOT = "<项目根目录>/.dotnet"
$exe = "<项目根目录>/apate/bin/Release/net6.0-windows/apate.exe"

# 如果系统已安装 .NET Runtime，直接调用
$exe = "apate.exe"
```

> 提示：项目根目录即包含 `apate.sln` 的目录。若未编译，先执行：
> `dotnet build apate.sln -c Release`

## 命令总览

```
apate <命令> <路径> [选项]
```

| 命令 | 缩写 | 功能 | 用法 |
|------|------|------|------|
| disguise | d | 伪装文件 | `apate d <路径> [--mode <模式>] [--mask <面具文件>]` |
| reveal | r | 还原文件 | `apate r <路径> [--force]` |
| detect | - | 检测伪装 | `apate detect <路径>` |
| suffix | s | 批量添加后缀 | `apate s <路径> <后缀>` |
| compress | c | LZ4压缩 | `apate c <路径>` |
| decompress | dc | LZ4解压 | `apate dc <路径>` |
| help | -h | 帮助 | `apate help` |

## 操作指南

### 1. 伪装文件 (disguise)

将文件头部覆盖为面具数据，末尾追加原始文件头（反转）+ 长度标记，并添加对应扩展名。

```powershell
# 一键伪装（默认预置MP4面具，适用最广）
&"$exe" disguise "C:\target.zip"
# 输出: target.zip.mp4

# 简易伪装为指定格式（仅替换文件头，不拼接面具文件）
&"$exe" disguise "C:\target.zip" --mode exe   # 可选: exe/jpg/mp4/mov

# 自定义面具文件（建议在一键伪装失效时尝试）
&"$exe" disguise "C:\folder" --mask "C:\mask.mp4"
```

**模式说明：**
| 模式 | 说明 |
|------|------|
| onekey | 默认，使用预置完整MP4面具文件，伪装效果最好 |
| exe/jpg/mp4/mov | 仅替换文件头特征字节，文件无法实际执行/播放 |
| mask | 通过 --mask 指定自定义面具文件 |

### 2. 还原文件 (reveal)

从伪装文件末尾读取原始文件头并恢复，移除面具数据。

```powershell
# 安全还原（默认，自动预检）
&"$exe" reveal "C:\target.zip.mp4"
# 输出: target.zip

# 批量还原文件夹
&"$exe" reveal "C:\folder"

# 强制还原（跳过预检，慎用！可能损坏未伪装文件）
&"$exe" reveal "C:\folder" --force
```

**安全机制：**
- 默认先调用检测逻辑验证文件是否被伪装
- 未检测到伪装的文件自动跳过，不会被破坏
- 多次还原同一文件不会损坏（第二次会被跳过）
- 仅 `--force` 模式会跳过预检

### 3. 检测伪装 (detect)

只读操作，不修改文件。分析文件末尾标记结构，判断是否被本工具伪装，并尝试识别原始格式。

```powershell
&"$exe" detect "C:\suspect.mp4"
&"$exe" detect "C:\folder"    # 递归检测
```

**输出示例：**
```
[伪装] C:\suspect.mp4
       检测到伪装！原始格式可能为: ZIP，面具长度: 1175741 字节
------------------------------------------------------------
检测完成！共 5 个文件，伪装: 1，正常: 4
```

**支持识别的原始格式：** ZIP、EXE/DLL、JPG、PNG、GIF、PDF、MP4、MOV、RAR、7Z、MP3、DOC/OLE

### 4. 批量添加后缀 (suffix)

仅修改文件名（追加扩展名），不修改文件内容。

```powershell
&"$exe" suffix "C:\folder" .mp4     # 递归添加
&"$exe" suffix "C:\file.zip" .7z    # 单个文件
&"$exe" s "C:\folder" mp4           # 缩写，可省略点号
```

### 5. LZ4 压缩/解压 (compress / decompress)

```powershell
# 压缩（生成 .lz4 文件）
&"$exe" compress "C:\data.bin"
# 输出: data.bin.lz4

# 解压（仅处理 .lz4 后缀文件）
&"$exe" decompress "C:\data.bin.lz4"
# 输出: data.bin
```

## 重要说明

- **路径**支持文件或文件夹，文件夹会递归处理所有子文件
- 所有操作均为**并行处理**（CPU核心数×2 并行度）
- **退出码**：0=全部成功，1=存在失败
- 不带任何参数运行 `apate.exe` 启动 GUI 图形界面
- 伪装/还原操作会修改文件，建议提前备份

## 典型工作流

```powershell
# 完整流程：伪装 → 验证 → 还原
&"$exe" disguise "C:\secret.pdf"           # → secret.pdf.mp4
&"$exe" detect "C:\secret.pdf.mp4"         # 确认伪装成功
&"$exe" reveal "C:\secret.pdf.mp4"         # → secret.pdf

# 批量处理整个文件夹
&"$exe" disguise "C:\batch_folder" --mode exe
&"$exe" detect "C:\batch_folder"
&"$exe" reveal "C:\batch_folder"

# 压缩后伪装（双重处理）
&"$exe" compress "C:\large_file.dat"       # → large_file.dat.lz4
&"$exe" disguise "C:\large_file.dat.lz4"   # → large_file.dat.lz4.mp4
```
