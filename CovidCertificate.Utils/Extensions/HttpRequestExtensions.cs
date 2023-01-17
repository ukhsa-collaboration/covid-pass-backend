using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CovidCertificate.Backend.Utils.Extensions
{
    public static class HttpRequestExtensions
    {

        public static T GetObjectFromRequest<T>(this IActionResult okObjectResult)
        {
            if (okObjectResult.GetType() != typeof(OkObjectResult))
            {
                return default;
            }

            var resultObject = (OkObjectResult)okObjectResult;
            if (resultObject.Value.GetType() != typeof(T))
            {
                return default;
            }

            var dto = (T)resultObject.Value;
            return dto;
        }


        public static string GetContentLanguage(this HttpRequest req)
        {
            if (req.Headers.TryGetValue("Content-Language", out var templateName))
                return templateName;

            return "en";
        }
    }
}