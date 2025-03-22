using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apate
{
    public enum ModeEnum
    {
        OneKey,
        Mask,
        Exe,
        Jpg,
        Mp4,
        Mov,
        AddMp4Extension,
        AddZipExtension,
        LZ4Compress,    // LZ4压缩封装
        LZ4Decompress   // LZ4解压还原
    }
}
