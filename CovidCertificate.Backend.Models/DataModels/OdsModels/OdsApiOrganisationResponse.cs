using System.Collections.Generic;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.DataModels.OdsModels
{
    public class OdsApiOrganisationResponse
    {
        [JsonProperty("Organisation")]
        public Organisation Organisation { get; set; }
    }

    public class Organisation
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Date")]
        public List<OrganisationDate> Date { get; set; }

        [JsonProperty("OrgId")]
        public OrgId OrgId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("LastChangeDate")]
        public string LastChangeDate { get; set; }

        [JsonProperty("orgRecordClass")]
        public string OrgRecordClass { get; set; }

        [JsonProperty("GeoLoc")]
        public GeoLoc GeoLoc { get; set; }

        [JsonProperty("Roles")]
        public OrganisationRoles Roles { get; set; }

        [JsonProperty("Rels")]
        public Rels Rels { get; set; }
    }

    public class OrganisationDate
    {
        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Start")]
        public string Start { get; set; }

        [JsonProperty("End")]
        public string End { get; set; }
    }

    public class OrgId
    {
        [JsonProperty("root")]
        public string Root { get; set; }

        [JsonProperty("assigningAuthorityName")]
        public string AssigningAuthorityName { get; set; }

        [JsonProperty("extension")]
        public string Extension { get; set; }
    }

    public class GeoLoc
    {
        [JsonProperty("Location")]
        public Location Location { get; set; }
    }

    public class Location
    {
        [JsonProperty("AddrLn1")]
        public string AddrLn1 { get; set; }

        [JsonProperty("AddrLn2")]
        public string AddrLn2 { get; set; }

        [JsonProperty("Town")]
        public string Town { get; set; }

        [JsonProperty("County")]
        public string County { get; set; }

        [JsonProperty("PostCode")]
        public string PostCode { get; set; }

        [JsonProperty("Country")]
        public string Country { get; set; }
    }

    public class OrganisationRoles
    {
        [JsonProperty("Role")]
        public List<OrganisationRole> Role { get; set; }
    }

    public class OrganisationRole
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uniqueRoleId")]
        public int UniqueRoleId { get; set; }

        [JsonProperty("Date")]
        public List<OrganisationDate> Date { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("primaryRole")]
        public bool? PrimaryRole { get; set; }
    }

    public class Rels
    {
        [JsonProperty("Rel")]
        public List<Rel> Rel { get; set; }
    }

    public class Rel
    {
        [JsonProperty("Date")]
        public List<OrganisationDate> Date { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Target")]
        public Target Target { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uniqueRelId")]
        public int UniqueRelId { get; set; }
    }

    public class Target
    {
        [JsonProperty("OrgId")]
        public OrgId OrgId { get; set; }

        [JsonProperty("PrimaryRoleId")]
        public PrimaryRoleId PrimaryRoleId { get; set; }
    }

    public class PrimaryRoleId
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uniqueRoleId")]
        public int UniqueRoleId { get; set; }
    }
}
