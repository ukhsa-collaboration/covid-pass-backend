using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CovidCertificate.Backend.Models.Pocos
{
    public class ValidationResponsePoco
    {
        public IActionResult Response { get; set; }
        public ClaimsPrincipal TokenClaims { get; set; }
        public UserProperties UserProperties { get; set; }
        public bool IsValid { get; set; }
        public bool IsForbidden { get; set; }

        public ValidationResponsePoco(string invalidMessage,UserProperties userProperties)
        {
            Response = new UnauthorizedObjectResult(invalidMessage);
            IsValid = false;
            UserProperties = userProperties;
        }

        public ValidationResponsePoco(bool isForbidden, string message)
        {
            Response = new ObjectResult(message) { StatusCode = 403 };
            IsValid = true;
            IsForbidden = isForbidden;
        }

        public ValidationResponsePoco(ClaimsPrincipal claims, UserProperties userProperties)
        {
            if (claims == default)
            {
                Response = new UnauthorizedObjectResult("Invalid Claims in token");
                IsValid = false;
                return;
            }

            TokenClaims = claims;
            UserProperties = userProperties;
            IsValid = true;
        }
    }
}
