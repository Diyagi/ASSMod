using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ValheimDiyagi
{
    internal static class Utils
    {
        public static byte[] Compress(byte[] data)
        {
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            var input = new MemoryStream(data);
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }

            return output.ToArray();
        }

        public static byte[] GenerateMD5(byte[] data)
        {
            byte[] md5hash;
            using (var md5 = MD5.Create())
            {
                md5.TransformFinalBlock(data, 0, data.Length);
                md5hash = md5.Hash;
            }

            return md5hash;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (var b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] DecompressAndVerify(ZPackage pkg)
        {
            var rcdatamd5 = pkg.ReadByteArray();
            var rdatamd5 = pkg.ReadByteArray();
            var cdata = pkg.ReadByteArray();

            var cdatamd5 = GenerateMD5(cdata);

            ZLog.Log("Compressed Data MD5: " + ByteArrayToString(cdatamd5) + " | Expected: " +
                     ByteArrayToString(rcdatamd5));
            if (cdatamd5.SequenceEqual(rcdatamd5))
            {
                var data = Decompress(cdata);
                var datamd5 = GenerateMD5(data);

                ZLog.Log("Decompressed Data MD5: " + ByteArrayToString(datamd5) + " | Expected: " +
                         ByteArrayToString(rdatamd5));

                ZLog.Log("Data Package Size: " + pkg.Size() + "Bs of " + data.Length + "Bs");
                if (datamd5.SequenceEqual(rdatamd5))
                {
                    return data;
                }

                ZLog.Log("Decompressed Data MD5 Check Failed !");
                return null;
            }

            ZLog.Log("Compressed Data MD5 Check Failed !");
            return null;
        }

        public static ZPackage CompressWithMD5(byte[] data)
        {
            var cdata = Compress(data);

            var cdatamd5 = GenerateMD5(cdata);
            var datamd5 = GenerateMD5(data);

            var pkg = new ZPackage();
            pkg.Write(cdatamd5);
            pkg.Write(datamd5);
            pkg.Write(cdata);

            ZLog.Log("Data Package Size: " + pkg.Size() + "Bs of " + data.Length + "Bs");

            return pkg;
        }
    }
}