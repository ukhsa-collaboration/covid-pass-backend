using System;

namespace CovidCertificate.Backend.Models.Interfaces.UserInterfaces
{
    public interface IUserBaseInformation 
    {
        string Name { get; }
        DateTime DateOfBirth { get; }
    }
}
