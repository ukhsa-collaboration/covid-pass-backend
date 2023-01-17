using CovidCertificate.Backend.Models.DataModels;
using CovidCertificate.Backend.Models.Enums;
using System;

namespace CovidCertificate.Backend.Models.ResponseDtos
{
    public class DomesticCertificateResponse
	{
		public Certificate Certificate { get; }
		public bool CertificateEverExisted { get; }
		public int? ErrorCode { get; }
		public DateTime? WaitPeriod { get; }
		public MandatoryToggle TwoPassStatus { get; }

		public DomesticCertificateResponse(Certificate certificate, bool certificateEverExisted, MandatoryToggle twoPassStatus, int? errorCode = null, DateTime? waitPeriod = null)
		{
			Certificate = certificate;
			CertificateEverExisted = certificateEverExisted;
			ErrorCode = errorCode;
			WaitPeriod = waitPeriod;
			TwoPassStatus = twoPassStatus;
		}
	}
}
