using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CovidCertificate.Backend.Models.EuCertJsonSchema
{
    public class EuCovidCert
    {
        [JsonProperty("1")]
        public string _1 { get; set; }
        [JsonProperty("4")]
        public long _4 { get; set; }
        [JsonProperty("6")]
        public long _6 { get; set; }
        [JsonProperty("-260")]
        public _260 _260 { get; set; }
    }

    public class _260
    {
        [JsonProperty("1")]
        public _1 _1 { get; set; }
    }

    public class _1
    {
        public List<V> v { get; set; }
        public List<R> r { get; set; }
        public List<T> t { get; set; }
        public string dob { get; set; }
        public Nam nam { get; set; }
        public object ver { get; set; }
    }

    public class Nam
    {
        public string fn { get; set; }
        public string gn { get; set; }
        public string fnt { get; set; }
        public string gnt { get; set; }
    }

    public class V
    {
        public string ci { get; set; }
        public string co { get; set; }
        public int dn { get; set; }
        public string dt { get; set; }
        [JsonProperty("is")]
        public string _is { get; set; }
        public string ma { get; set; }
        public string mp { get; set; }
        public int sd { get; set; }
        public string tg { get; set; }
        public string vp { get; set; }
    }

    public class R
    {
        public string tg { get; set; }
        public string fr { get; set; }
        public string co { get; set; }
        [JsonProperty("is")]
        public string _is { get; set; }
        public string df { get; set; }
        public string du { get; set; }
        public string ci { get; set; }
    }

    public class T
    {
        public string tg { get; set; }
        public string fr { get; set; }
        public string ma { get; set; }
        public DateTime sc { get; set; }
        public string tr { get; set; }
        public string tc { get; set; }
        public string co { get; set; }
        [JsonProperty("is")]
        public string _is { get; set; }
        public string ci { get; set; }
    }
}


