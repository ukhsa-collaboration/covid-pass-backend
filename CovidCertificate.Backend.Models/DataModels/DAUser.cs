using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;
using FluentValidation;
using System;
using System.Threading.Tasks;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class DAUser : IUserBaseInformation, IUserCBORInformation
    {
        public string Name { get; }
        public string FamilyName { get; }
        public string GivenName { get; }
        public DateTime DateOfBirth { get; }

        public DAUser(string name, string familyName, string givenName, DateTime dateOfBirth)
        {
            Name = name;
            FamilyName = familyName;
            GivenName = givenName;
            DateOfBirth = dateOfBirth;
        }
        
    }
}
