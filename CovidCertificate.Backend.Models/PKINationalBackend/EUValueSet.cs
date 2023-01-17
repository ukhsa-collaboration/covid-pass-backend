using System.Collections.Generic;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.PKINationalBackend
{
    public class EUValueSet
    {
        public Dictionary<string, string> VaccineTypes { get; set; }
        public Dictionary<string, string> VaccineManufacturers { get; set; }
        public Dictionary<string, string> DiseasesTargeted { get; set; }
        public Dictionary<string, string> VaccineNames { get; set; }
        public Dictionary<string, string> TestTypes { get; set; }
        public Dictionary<string, string> TestResults { get; set; }
        public Dictionary<string, string> TestManufacturers { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ReadableVaccineNames { get; set; }

        public EUValueSet()
        {

        }

        //WARNING- only use if property is property dictionary keys and values are not reference types.
        public EUValueSet(EUValueSet valueSet)
        {
            VaccineTypes = CopyDictionaryValue(valueSet.VaccineTypes);
            VaccineManufacturers = CopyDictionaryValue(valueSet.VaccineManufacturers);
            DiseasesTargeted = CopyDictionaryValue(valueSet.DiseasesTargeted);
            VaccineNames = CopyDictionaryValue(valueSet.VaccineNames);
            TestTypes = CopyDictionaryValue(valueSet.TestTypes);
            TestResults = CopyDictionaryValue(valueSet.TestResults);
            TestManufacturers = CopyDictionaryValue(valueSet.TestManufacturers);
            ReadableVaccineNames = CopyDictionaryValue(valueSet.ReadableVaccineNames);
        }

        private Dictionary<string, string> CopyDictionaryValue(Dictionary<string, string> inDict)
        {
            return inDict == null ? null : new Dictionary<string, string>(inDict);
        }
    }
}
