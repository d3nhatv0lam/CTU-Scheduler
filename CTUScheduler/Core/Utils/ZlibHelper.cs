using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CTUScheduler.Core.Utils;

public static class ZlibHelper
{
    public static string DecompressZlib(string base64String)
    {
        try
        {
            byte[] compressedBytes = Convert.FromBase64String(base64String);
            using var msInput = new MemoryStream(compressedBytes);

            using var zlibStream = new ZLibStream(msInput, CompressionMode.Decompress);
            using var msOutput = new MemoryStream();

            zlibStream.CopyTo(msOutput);

            return Encoding.UTF8.GetString(msOutput.ToArray());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Giải nén thông tin sinh viên (Zlib) thất bại.", ex);
        }
    }
}