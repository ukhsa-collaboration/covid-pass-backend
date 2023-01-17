﻿using System.Threading.Tasks;
using CovidCertificate.Backend.DASigningService.Responses;
using CovidCertificate.Backend.DASigningService.Services.Commands;

namespace CovidCertificate.Backend.DASigningService.Interfaces
{
    public interface IVaccinationBarcodeGenerator
    {
        Task<BarcodeResults> BarcodesFromFhirBundleAsync(GenerateInternationalBarcodeCommand command);
    }
}
