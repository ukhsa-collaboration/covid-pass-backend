using System;

namespace CovidCertificate.Backend.DASigningService.Models.Exceptions
{
    public class ThumbprintNotAllowedException : Exception
    {
        public ThumbprintNotAllowedException(string message) : base(message) {}
    }
}
