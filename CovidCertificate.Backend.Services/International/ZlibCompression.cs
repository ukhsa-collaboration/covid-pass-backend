using System.IO;
using System.Threading.Tasks;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace CovidCertificate.Backend.Services.International
{
    public static class ZlibCompression
    {
        public static async Task<byte[]> CompressData(byte[] inData)
        {
            using (MemoryStream outMemoryStream2 = new MemoryStream())
            using (ZlibStream outZStream2 = new ZlibStream(outMemoryStream2, CompressionMode.Compress, CompressionLevel.Default))
            using (var inMemoryStream2 = new MemoryStream(inData))
            {
                outZStream2.FlushMode = FlushType.Finish;
                await inMemoryStream2.CopyToAsync(outZStream2);
                outZStream2.Flush();
                byte[] outData = outMemoryStream2.ToArray();
                return outData;
            }
        }

        public static byte[] DecompressData(byte[] inData)
        {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZlibStream outZStream = new ZlibStream(outMemoryStream, CompressionMode.Decompress))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                CopyStream(inMemoryStream, outZStream);
                outZStream.Flush();
                byte[] outData = outMemoryStream.ToArray();
                return outData;
            }
        }

        private static void CopyStream(Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
