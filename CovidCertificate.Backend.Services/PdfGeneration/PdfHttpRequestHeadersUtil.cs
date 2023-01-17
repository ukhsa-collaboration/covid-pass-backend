using System;
using CovidCertificate.Backend.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace CovidCertificate.Backend.Services.PdfGeneration
{
    public static class PdfHttpRequestHeadersUtil
    {
        public static (PDFType PdfType, int DoseNumber) GetPdfHeaderValues(HttpRequest req)
        {
            var pdfType = GetPDFType(req);
            var doseNumber = -1;

            if (pdfType == PDFType.Vaccine)
            {
                doseNumber = GetDoseNumber(req);
            }
            return (pdfType, doseNumber);
        }

        private static PDFType GetPDFType(HttpRequest req)
        {
            var pdfType = req.Headers["PDFType"];
            int.TryParse(pdfType, out var result);

            if (Enum.IsDefined(typeof(PDFType), result))
            {
                return (PDFType)result;
            }

            throw new Exception("Invalid PDFType");
        }

        private static int GetDoseNumber(HttpRequest req)
        {
            var doseNumber = req.Headers["DoseNumber"];
            int.TryParse(doseNumber, out var result);
            if (result > 0)
            {
                return result;
            }
            throw new Exception("Invalid DoseNumber");
        }
    }
}
