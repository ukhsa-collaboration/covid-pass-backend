using FluentValidation;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CovidCertificate.Backend.Models.Interfaces.UserInterfaces;

namespace CovidCertificate.Backend.Models.RequestDtos
{
    public abstract class UserRequestDto : IUser
    {
        [JsonConstructor]
        protected UserRequestDto(string name, DateTime dateOfBirth, string emailAddress, string phoneNumber, string nhsNumber = null)
        {
            this.Name = name;
            this.DateOfBirth = dateOfBirth;
            this.EmailAddress = emailAddress;
            this.PhoneNumber = phoneNumber;
            this.NhsNumber = nhsNumber;
        }

        [JsonRequired]
        public virtual string Name
        {
            get { return name; }
            protected set { name = value?.Trim(); }
        }

        [JsonRequired]
        public virtual DateTime DateOfBirth { get; protected set; }

        public virtual string EmailAddress
        {
            get { return email; }
            protected set { email = value?.Trim(); }
        }

        public virtual string PhoneNumber
        {
            get { return phone; }
            protected set { phone = value == null ? null : string.Concat(value.Where(c => !char.IsWhiteSpace(c))); }
        }

        public virtual string NhsNumber { get; protected set; }

        private string name;
        private string email;
        private string phone;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("NhsNumber:").Append(this.NhsNumber??"").AppendLine();
            sb.Append("Name").Append(this.name??"").AppendLine();
            sb.Append("Email:").Append(this.email??"").AppendLine();
            sb.Append("Phone:").Append(this.phone??"").AppendLine();
            sb.Append("DOB:").Append(this.DateOfBirth).AppendLine();

            return sb.ToString();
        }
    }
}
