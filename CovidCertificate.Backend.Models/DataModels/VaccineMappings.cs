using System.Collections.Generic;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class VaccineMappings
    {
        public IDictionary<string, VaccineMap> VaccineMaps { get; set; }
        public IEnumerable<string> BoosterProcedureCodes { get; set; }
        public IEnumerable<string> FreezeFollowingBoosterVaccinationSNOMEDs { get; set; }
        public IEnumerable<string> BoosterOnlyVaccineCodes { get; set; }

        public VaccineMappings(IDictionary<string, VaccineMap> vaccineMaps,
                               IEnumerable<string> boosterProcedureCodes,
                               IEnumerable<string> freezeFollowingBoosterVaccinationSNOMEDs,
                               IEnumerable<string> boosterOnlyVaccineCodes)
        {
            VaccineMaps = vaccineMaps;
            BoosterProcedureCodes = boosterProcedureCodes;
            FreezeFollowingBoosterVaccinationSNOMEDs = freezeFollowingBoosterVaccinationSNOMEDs;
            BoosterOnlyVaccineCodes = boosterOnlyVaccineCodes;
        }

        public VaccineMappings()
        {
        }
    }
}
