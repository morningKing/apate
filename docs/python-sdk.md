# Apate Python SDK 使用指南

通过 `Apate.Core.dll` 类库，Python 可以直接调用 apate 的所有核心功能，无需通过命令行子进程。

## 环境要求

- Python 3.8+
- .NET 6.0 Runtime（或本地 SDK）
- pythonnet 库

## 安装依赖

```bash
pip install pythonnet
```

## 初始化

```python
import pythonnet

# 加载 .NET CoreCLR 运行时
# runtime_config 指向 Apate.Core 编译输出的 runtimeconfig.json
pythonnet.load('coreclr', runtime_config=r'<项目路径>/Apate.Core/bin/Release/net6.0/Apate.Core.runtimeconfig.json')

import clr
clr.AddReference(r'<项目路径>/Apate.Core/bin/Release/net6.0/Apate.Core.dll')

from Apate.Core import ApateEngine
```

> **提示**：如果使用本地便携版 .NET SDK，需确保 `DOTNET_ROOT` 环境变量指向 SDK 目录，
> 或在 `pythonnet.load()` 时传入 `dotnet_root` 参数：
> ```python
> pythonnet.load('coreclr',
>     runtime_config=r'...',
>     dotnet_root=r'<项目路径>/.dotnet')
> ```

## API 参考

### 伪装文件

```python
# 使用内置面具
ApateEngine.Disguise(r"C:\secret.zip", ApateEngine.Mp4Head)   # 伪装为 MP4
ApateEngine.Disguise(r"C:\secret.zip", ApateEngine.ExeHead)   # 伪装为 EXE
ApateEngine.Disguise(r"C:\secret.zip", ApateEngine.JpgHead)   # 伪装为 JPG
ApateEngine.Disguise(r"C:\secret.zip", ApateEngine.MovHead)   # 伪装为 MOV

# 使用自定义面具文件
mask = ApateEngine.FileToBytes(r"C:\my_mask.mp4")
ApateEngine.Disguise(r"C:\secret.zip", mask)
```

返回值：`1` 成功，`-1` 失败

### 还原文件

```python
ApateEngine.Reveal(r"C:\secret.zip.mp4")
```

返回值：`1` 成功，`-1` 失败

> **安全建议**：还原前先调用 `DetectDisguise` 确认文件确实被伪装，避免对正常文件造成损坏。

### 检测伪装

```python
result = ApateEngine.DetectDisguise(r"C:\file.mp4")
if result is None:
    print("文件未被伪装")
else:
    print(result)  # 例如："检测到伪装！原始格式可能为: ZIP，面具长度: 1175741 字节"
```

返回值：字符串（检测到伪装）或 `None`（未伪装）

### LZ4 压缩 / 解压

```python
# 压缩（原地压缩，需自行重命名添加 .lz4 后缀）
ApateEngine.CompressWithLZ4(r"C:\data.bin")

# 解压（原地解压，需自行移除 .lz4 后缀）
ApateEngine.DecompressWithLZ4(r"C:\data.bin.lz4")
```

返回值：`1` 成功，`-1` 失败

### 文件工具

```python
# 递归获取目录下所有文件
files = ApateEngine.GetAllFilesRecursively(r"C:\folder")
print(f"共 {files.Count} 个文件")
for f in files:
    print(f)

# 文件转字节数组（受 300MB 大小限制）
data = ApateEngine.FileToBytes(r"C:\mask.mp4")

# 识别文件格式
fmt = ApateEngine.IdentifyFileFormat(data)
print(fmt)  # 例如："ZIP", "MP4", "EXE/DLL", "PNG", None
```

### 内置常量

| 常量 | 说明 |
|------|------|
| `ApateEngine.MaximumMaskLength` | 面具最大长度（约 300MB） |
| `ApateEngine.MaskLengthIndicatorLength` | 面具长度标记字节数（4） |
| `ApateEngine.Mp4Head` | MP4 文件头字节数组 |
| `ApateEngine.ExeHead` | EXE 文件头字节数组 |
| `ApateEngine.JpgHead` | JPG 文件头字节数组 |
| `ApateEngine.MovHead` | MOV 文件头字节数组 |

## 完整示例：批量伪装 + 检测 + 还原

```python
import pythonnet
pythonnet.load('coreclr', runtime_config=r'e:\Code\tool\apate\Apate.Core\bin\Release\net6.0\Apate.Core.runtimeconfig.json')
import clr
clr.AddReference(r'e:\Code\tool\apate\Apate.Core\bin\Release\net6.0\Apate.Core.dll')

from Apate.Core import ApateEngine
import os

folder = r"C:\my_files"

# 1. 获取所有文件
files = ApateEngine.GetAllFilesRecursively(folder)
print(f"找到 {files.Count} 个文件")

# 2. 批量伪装
for f in files:
    path = str(f)
    if ApateEngine.Disguise(path, ApateEngine.Mp4Head) == 1:
        os.rename(path, path + ".mp4")
        print(f"[伪装] {path}")

# 3. 检测
disguised_files = ApateEngine.GetAllFilesRecursively(folder)
for f in disguised_files:
    path = str(f)
    result = ApateEngine.DetectDisguise(path)
    if result:
        print(f"[检测] {path}: {result}")

# 4. 安全还原
for f in disguised_files:
    path = str(f)
    if ApateEngine.DetectDisguise(path) is not None:
        if ApateEngine.Reveal(path) == 1:
            new_path = path.rsplit('.', 1)[0]
            os.rename(path, new_path)
            print(f"[还原] {path}")
```

## 编译 Apate.Core

如果修改了源码，需要重新编译：

```powershell
# 使用本地 SDK
$env:DOTNET_ROOT = "<项目路径>/.dotnet"
&"<项目路径>/.dotnet/dotnet.exe" build "<项目路径>/Apate.Core/Apate.Core.csproj" -c Release

# 或使用系统安装的 dotnet
dotnet build Apate.Core/Apate.Core.csproj -c Release
```

编译输出位于：`Apate.Core/bin/Release/net6.0/`

## 常见问题

**Q: 报错 `InvalidConfigFile` 或找不到 `runtimeconfig.json`？**

确保编译时 csproj 中包含 `<GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>`（已默认配置）。

**Q: 报错找不到 .NET runtime？**

设置 `DOTNET_ROOT` 环境变量或在 `pythonnet.load()` 中传入 `dotnet_root` 参数。

**Q: `GetAllFilesRecursively` 返回的列表如何遍历？**

返回的是 .NET `List<string>`，用 `.Count` 获取长度，用索引或 `for f in list` 遍历，
每个元素用 `str(f)` 转为 Python 字符串。

**Q: 能否在 Linux/macOS 上使用？**

Apate.Core 目标框架为 `net6.0`（非 Windows 专属），理论上支持跨平台。
但文件伪装功能本身不依赖 Windows API，可在任何支持 .NET 6 的平台运行。
