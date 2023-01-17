using System.Collections.Generic;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.OdsModels
{
    public class OdsApiOrganisationsLastChangeDateResponse
    {
        [JsonProperty("Organisations")]
        public List<GPOrganisation> Organisations { get; set; }
    }

    public class GPOrganisation
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("OrgId")]
        public string OrgId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("OrgRecordClass")]
        public string OrgRecordClass { get; set; }

        [JsonProperty("PostCode")]
        public string PostCode { get; set; }

        [JsonProperty("LastChangeDate")]
        public string LastChangeDate { get; set; }

        [JsonProperty("PrimaryRoleId")]
        public string PrimaryRoleId { get; set; }

        [JsonProperty("PrimaryRoleDescription")]
        public string PrimaryRoleDescription { get; set; }

        [JsonProperty("OrgLink")]
        public string OrgLink { get; set; }
    }
}
