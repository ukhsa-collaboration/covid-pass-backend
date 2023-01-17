using System;

namespace CovidCertificate.Backend.Models.Exceptions
{
    public class QRCodeTypeException : Exception
    {
        public QRCodeTypeException(string message) : base(message)
        {
        }
    }
}
