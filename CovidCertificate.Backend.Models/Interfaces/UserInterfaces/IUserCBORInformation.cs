using System;

namespace CovidCertificate.Backend.Models.Interfaces.UserInterfaces
{
    public interface IUserCBORInformation 
    {
        public string Name { get; }
        public string FamilyName { get; }
        public string GivenName { get; }
        public DateTime DateOfBirth { get; }
    }
}
