using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class TestMappings
    {
        public IDictionary<string, string> Result { get; set; }
        public IDictionary<string, string> Type { get; set; }
        public IDictionary<string, string> StaticValues { get; set; }
        public IDictionary<string, string> SelfTestValues { get; set; }
        public IEnumerable<string> IsNAAT { get; set; }

        public TestMappings(IDictionary<string, string> result, 
                            IDictionary<string, string> type,
                            IDictionary<string, string> staticValues,
                            IDictionary<string, string> selfTestValues, 
                            IEnumerable<string> isNAAT)
        {
            Result = result;
            Type = type;
            StaticValues = staticValues;
            SelfTestValues = selfTestValues;
            IsNAAT = isNAAT;
        }

        public TestMappings()
        {
        }
    }
}
