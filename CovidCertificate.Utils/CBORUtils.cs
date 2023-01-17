using PeterO.Cbor;

namespace CovidCertificate.Backend.Utils
{
    public static class CBORUtils
    {
        public static byte[] JsonToCbor(string jsonString)
        {
            //Convert from jsonString to CBORObject then to bytes
            CBORObject cborFormatedJson = CBORObject.FromJSONString(jsonString);
            byte[] cborDataFormatBytes = cborFormatedJson.EncodeToBytes();

            return cborDataFormatBytes;
        }

        public static string CborToJson(byte[] cborDataFormatBytes)
        {
            // Convert from bytes to CBORObject then to jsonString
            CBORObject cborObjectFromBytes = CBORObject.DecodeFromBytes(cborDataFormatBytes);
            string jsonString = cborObjectFromBytes.ToJSONString();
            return jsonString;
        }
    }
}
