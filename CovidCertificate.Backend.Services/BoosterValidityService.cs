using System;
using System.Collections.Generic;
using System.Linq;
using CovidCertificate.Backend.Interfaces;
using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Utils.Extensions;
using Microsoft.Extensions.Configuration;

namespace CovidCertificate.Backend.Services
{
    public class BoosterValidityService : IBoosterValidityService
    {
        private IConfiguration configuration;

        public BoosterValidityService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }


        public bool IsBoosterWithinCorrectTimeFrame(IEnumerable<IGenericResult> results)
        {
            var vaccines = new List<Vaccine>();
            foreach (var result in results)
            {
                if (result is Vaccine)
                {
                    vaccines.Add((Vaccine)result);
                }
            }

            if (vaccines.NullOrEmpty())
            {
                return false;
            }

            vaccines = vaccines.OrderBy(x => x.VaccinationDate).ToList();
            var boosters = vaccines.Where(x => x.IsBooster);
            if (boosters.NullOrEmpty())
            {
                return false;
            }

            var booster = boosters.OrderByDescending(x => x.VaccinationDate).First();
            var nonBoosterVaccines = vaccines.Except(boosters);
            if (nonBoosterVaccines.NullOrEmpty())
            {
                return false;
            }

            var lastVacc = nonBoosterVaccines.Last();

            //Booster needs to be applied 8 weeks/ 56 days after primary course
            if (IsTimePeriodAfterPrimaryCourseAndBoosterInvalid(booster, lastVacc))
            {
                return false;
            }

            //If someone got booster dose within 122 days since primary course they are eligible for certificate
            if (IsTimePeriodAfterPrimaryCourseAndBoosterWithinGracePeriod(booster, lastVacc))
            {
                return true;
            }

            //If booster dose's date is later than 122 days since primary course then 10 days wait period applies
            if (booster.VaccinationDate.AddDays(10) < DateTime.UtcNow)
            {
                return true;
            }

            return false;
        }

        private bool IsTimePeriodAfterPrimaryCourseAndBoosterWithinGracePeriod(Vaccine booster, Vaccine lastVacc)
        {
            return lastVacc.VaccinationDate.AddDays(configuration.GetValue<int>("GracePeriodBetweenPrimaryCourseAndBooster")) > booster.VaccinationDate;
        }

        private bool IsTimePeriodAfterPrimaryCourseAndBoosterInvalid(Vaccine booster, Vaccine lastVacc)
        {
            return lastVacc.VaccinationDate.AddDays(configuration.GetValue<int>("MinimumPeriodBetweenPrimaryCourseAndBooster")) > booster.VaccinationDate;
        }

        public IEnumerable<IGenericResult> RemoveBoosters(IEnumerable<IGenericResult> results)
        {
            var vaccines = new List<Vaccine>();

            if (results.NullOrEmpty())
            {
                return results;
            }

            foreach (var result in results)
            {
                if (result is Vaccine)
                {
                    vaccines.Add((Vaccine)result);
                }
            }

            var boosters = vaccines.Where(x => x.IsBooster);
            if (boosters.NullOrEmpty())
            {
                return results;
            }
            return results.Except(boosters);
        }
    }
}
