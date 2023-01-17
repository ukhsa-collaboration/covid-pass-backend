using System.Collections.Generic;
using CovidCertificate.Backend.DASigningService.ErrorHandling;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.DASigningService.Responses
{
    public class BarcodeResults
    {
        public List<BarcodeResult> Barcodes { get; set; }
        public List<Error> Errors { get; set; }

        [JsonIgnore]
        public string UVCI { get; set; }
    }
}
