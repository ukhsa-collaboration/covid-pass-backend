using CovidCertificate.Backend.Models.Interfaces;
using CovidCertificate.Backend.Utils;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;
using CovidCertificate.Backend.Models.ResponseDtos;
using CovidCertificate.Backend.Models.Enums;
using CovidCertificate.Backend.Models.Pocos;

namespace CovidCertificate.Backend.Models.DataModels
{
    public class CovidPassportUser : IUser, IJwtTokenData, IUserCBORInformation
    {
        [JsonConstructor]
        public CovidPassportUser(string name, DateTime dateOfBirth, string emailAddress, string phoneNumber,
                                 string nhsNumber = null, string familyName = null, string givenName = null,
                                 string identityProofingLevel = null)
        {
            this.Name = name;
            this.DateOfBirth = dateOfBirth;
            this.EmailAddress = emailAddress;
            this.PhoneNumber = phoneNumber;
            this.NhsNumber = nhsNumber;
            this.FamilyName = familyName;
            this.GivenName = givenName;
            if (identityProofingLevel != null)
            {
                this.IdentityProofingLevel = (IdentityProofingLevel)Enum.Parse(typeof(IdentityProofingLevel), identityProofingLevel);
            }
        }

        public CovidPassportUser(ValidationResponsePoco tokenValidationResult)
        {
            var claimsPrincipal = tokenValidationResult.TokenClaims;
            var userProperties = tokenValidationResult.UserProperties;

            var isLong = long.TryParse(claimsPrincipal.FindFirst(ClaimTypes.DateOfBirth)?.Value, out var dobResult);
            if (isLong)
                DateOfBirth = DateTime.FromFileTimeUtc(dobResult);

            Name = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value;
            EmailAddress = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
            PhoneNumber = claimsPrincipal.FindFirst(ClaimTypes.MobilePhone)?.Value;
            NhsNumber = claimsPrincipal.FindFirst("NHSNumber")?.Value;
            FamilyName = claimsPrincipal.FindFirst(ClaimTypes.Surname)?.Value;
            GivenName = claimsPrincipal.FindFirst(ClaimTypes.GivenName)?.Value;
            IdentityProofingLevel = (IdentityProofingLevel)(userProperties?.IdentityProofingLevel);
            GracePeriod = userProperties?.GracePeriod;
            Country = userProperties?.Country;
            DomesticAccessLevel = userProperties?.DomesticAccessLevel;
        }

        public string Name { get; }
        public DateTime DateOfBirth { get; }
        public string EmailAddress { get; }
        public string PhoneNumber { get; }
        public string NhsNumber { get; }
        public string FamilyName { get; }
        public string GivenName { get; }
        public IdentityProofingLevel IdentityProofingLevel { get; set; }
        public string Country { get; set; }

        public GracePeriodResponse GracePeriod { get; set; }
        public DomesticAccessLevel? DomesticAccessLevel { get; set; }

        public virtual IEnumerable<Claim> GetClaims()
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, Name));
            if (!string.IsNullOrEmpty(EmailAddress))
                claims.Add(new Claim(ClaimTypes.Email, EmailAddress));

            if (!string.IsNullOrEmpty(PhoneNumber))
                claims.Add(new Claim(ClaimTypes.MobilePhone, PhoneNumber));

            if (DateOfBirth != default)
                claims.Add(new Claim(ClaimTypes.DateOfBirth, DateOfBirth.ToFileTimeUtc().ToString()));

            if (NhsNumber != default)
                claims.Add(new Claim("NHSNumber", NhsNumber));

            if (!string.IsNullOrEmpty(FamilyName))
                claims.Add(new Claim(ClaimTypes.Surname, FamilyName));

            if (!string.IsNullOrEmpty(GivenName))
                claims.Add(new Claim(ClaimTypes.GivenName, GivenName));


            return claims;
        }

        /// <summary>
        /// Gets the <see cref="Expression{TDelegate}"/> used to calculate the data identifier for database operations concerning objects of type <typeparamref name="T"/> for the instance.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown on both <see cref="this.EmailAddress"/> , <see cref="this.PhoneNumber"/> and <see cref="this.NhsNumber"/> being null or empty.</exception>
        /// <returns>An <see cref="Expression{TDelegate}"/> of type <see cref="Func{T, TResult}"/> that compiles() to the calculation of the data identifier for the instance.</returns>

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name:").Append(this.Name ?? "").AppendLine();
            sb.AppendLine("EmailAddress:").Append(this.EmailAddress ?? "").AppendLine();
            sb.AppendLine("FamilyName:").Append(this.FamilyName ?? "").AppendLine();
            sb.AppendLine("GivenName").Append(this.GivenName ?? "").AppendLine();
            sb.AppendLine("NhsNumber:").Append(this.NhsNumber ?? "").AppendLine();
            sb.AppendLine("PhoneNumber:").Append(this.PhoneNumber ?? "").AppendLine();
            sb.AppendLine("DOB:").Append(this.DateOfBirth).AppendLine();

            return base.ToString();
        }

        public string ToNhsNumberAndDobHashKey()
        {
            if (NhsNumber == null || DateOfBirth == default)
            {
                return "Unknown";
            }

            return HashUtils.GenerateHash(NhsNumber, DateOfBirth);
        }
    }
}
