using System;
using System.Collections.Generic;
using System.Linq;

namespace CovidCertificate.Backend.Utils.Extensions
{
    public static class LinqExtensions
    {
        public static bool NullOrEmpty<T>(this IEnumerable<T> t)
        {
            return t == null ? true : !t.Any();
        }

        /// <summary>
        /// Returns all unique elements based upon a custom lambda selector used to group duplicates with the propertyPreference
        /// allowing a property to be used as a decider between choosing the duplicate to use. The duplicate with lower value of
        /// the propertyPreference is chosen.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="propertyPreference"></param>
        /// <returns> An IEnumerable<T> where all elements are unique based upon the selector.</returns>
        public static IEnumerable<T> DistinctWithPreference<T>(this IEnumerable<T> enumerable, Func<T, dynamic> selector, string propertyPreference)
        {
            var distinctEnumerable = enumerable.GroupBy(selector)
                                               .Select(group => group.OrderBy(x => x.GetType()
                                                                                    .GetProperty(propertyPreference)
                                                                                    .GetValue(x))
                                                                     .First());

            return distinctEnumerable;
        }
    }
}
