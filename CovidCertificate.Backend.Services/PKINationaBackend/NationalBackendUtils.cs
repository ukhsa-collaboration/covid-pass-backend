using System.Collections.Generic;
using CovidCertificate.Backend.Models.PKINationalBackend;

namespace CovidCertificate.Backend.Services.PKINationaBackend
{
    public static class NationalBackendUtils
    {
        /// <summary>
        /// Combine 2 EUValueSets. The values from the API value set are used if the key also exists
        /// in the blob storage value set.
        /// </summary>
        /// <param name="apiValueSet">The value set received from the EUDGCG API. All values from this
        /// are used.
        /// </param>
        /// <param name="blobValueSet">The value set received from internal blob storage. Values are only
        /// used from this value set if the key does not clash with any from the API value set.
        /// </param>
        /// <returns>One EUValueSet with all values combined.</returns>
        public static EUValueSet CombineValueSets(EUValueSet apiValueSet, EUValueSet blobValueSet)
        {
            var newValueSet = new EUValueSet(apiValueSet);

            foreach (var prop in blobValueSet.GetType().GetProperties())
            {
                var blobValueSetPropValue = (Dictionary<string, string>)typeof(EUValueSet).GetProperty(prop.Name).GetValue(blobValueSet);
                if (blobValueSetPropValue != null)
                {
                    var newValueSetPropValue = (Dictionary<string, string>)typeof(EUValueSet).GetProperty(prop.Name).GetValue(newValueSet);
                    typeof(EUValueSet).GetProperty(prop.Name).SetValue(newValueSet, CombineProperties(newValueSetPropValue, blobValueSetPropValue));
                }
            }

            return newValueSet;
        }

        private static Dictionary<string, string> CombineProperties(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            if (dict1 == null)
            {
                return dict2;
            }

            if (dict2 == null)
            {
                return dict1;
            }

            foreach (var key in dict2.Keys)
            {
                if (!dict1.ContainsKey(key))
                {
                    dict1.Add(key, dict2[key]);
                }
            }

            return dict1;
        }
    }
}
