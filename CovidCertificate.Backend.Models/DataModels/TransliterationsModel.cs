namespace CovidCertificate.Backend.Models.DataModels
{
    [Collection("Transliterations-Collection")]
    public class TransliterationsModel
    {
        public string Unicode { get; set; }
        public string Description { get; set; }
        public string RecommendedTransliteration { get; set; }

        public TransliterationsModel()
        {
        }

        public TransliterationsModel(string unicode, string description, string recommendedTransliteration)
        {
            Unicode = unicode;
            Description = description;
            RecommendedTransliteration = recommendedTransliteration;
        }
    }
}