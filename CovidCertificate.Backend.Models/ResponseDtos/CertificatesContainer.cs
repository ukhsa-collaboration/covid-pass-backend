using CovidCertificate.Backend.Models.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class CertificatesContainer
    {
		public IEnumerable<Certificate> Certificates { get; }
		public int? ErrorCode { get; set; }
		public DateTime? WaitPeriod { get; set; }

		public CertificatesContainer(IEnumerable<Certificate> certificates, int? errorCode = null, DateTime? waitPeriod = null)
		{
			Certificates = certificates;
			ErrorCode = errorCode;
			WaitPeriod = waitPeriod;
		}

		public CertificatesContainer(Certificate certificate, int? errorCode = null, DateTime? waitPeriod = null)
		{
			Certificates = new List<Certificate>() { certificate };
			ErrorCode = errorCode;
			WaitPeriod = waitPeriod;
		}

		public CertificatesContainer(int? errorCode = null, DateTime? waitPeriod = null)
		{
			Certificates = new List<Certificate>();
			ErrorCode = errorCode;
			WaitPeriod = waitPeriod;
		}
		public CertificatesContainer()
		{
			Certificates = new List<Certificate>();
			ErrorCode = null;
			WaitPeriod = null;
		}
        public Certificate GetSingleCertificateOrNull()
        {
            return Certificates.FirstOrDefault();
        }
    }
}
