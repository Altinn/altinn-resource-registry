#nullable enable
using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Enums;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Model describing a complete resource from the resrouce registry
    /// </summary>
    
    /// Kommentar: det er overlapp her med /app-localtest/testdata/
    /// /authorization/resources/Appid_400.json
    /// Mangler fra testdata: bool Delegable, bool Visible
    /// Keywords har uklar struktur: står som "null" i Appid_400.json,
    /// mangler fra acc-man.json, nav...json har array med et objekt,
    // to key:value par "word": "xxx", "language": "nb", som passer med
    // Core.Model.Keywor.cs fil, såvidt jeg kan forstå. 
    // ResourceType er Enum med array... uklart hvordan dette blir sendt videre
    // MainLanguage mangler fra alle tre .json eksempler i testdata.
    public class ServiceResource
    {
        /// <summary>
        /// The identifier of the resource
        /// </summary>
        /// identifier finnes i testdata: e.g. filen Appid_400.json
        /// har "identifier": "appid-400"
        public string Identifier { get; set; }



        /// <summary>
        /// The title of service
        /// </summary>
        /// title finnes i testdata: har struktur 
        /// "title" : { 
        ///     "en": "Humbug Registry", 
        ///     "nb": "Tulleregisteret",
        ///     "nn": "Tulleregisteret
        /// }
        public Dictionary<string, string> Title { get; set; }



        /// <summary>
        /// Description
        /// </summary>
        /// description finnes i testdata: har struktur 
        /// "description" : { 
        ///     "en": "Humbug Registry", 
        ///     "nb": "Tulleregisteret",
        ///     "nn": "Tulleregisteret
        /// } 
        public Dictionary<string, string> Description { get; set; }




        /// <summary>
        /// Description explaining the rights a recipient will receive if given access to the resource
        /// </summary>
        /// rightsDescription finnes i testdata: har struktur 
        /// "rightsDescription" : { 
        ///     "en": "Gives access to silly things.",
        ///     "nb": "Gir tilgang til tullete ting.",
        ///     "nn": "Gir tilgang til tullete ting."
        /// } 
        public Dictionary<string, string> RightDescription { get; set;  }




        /// <summary>
        /// The homepage
        /// </summary>
        /// finnes i testdata: "homepage": null, i Appid_400.json
        // men i altinn_access_management.json står denne som
        // "homepage": "www.altinn.no",
        public string Homepage { get; set; }    



        /// <summary>
        /// The status
        /// </summary>
        /// finnes i testdata: i altinn_access_management.json står denne som
        /// "status": "Completed", Appid_400.json har "status": null,
        public string Status { get; set; }


        /// <summary>
        /// When the resource is available from
        /// </summary>
        /// finnes i testdata: "validFrom": "2020-03-04T18:04:27.27",
        public DateTime ValidFrom { get; set; } 


        /// <summary>
        /// When the resource is available to
        /// </summary>
        /// finnes i testdata: "validTo": "9999-12-31T23:59:59.997",
        public DateTime ValidTo { get; set; }



        /// <summary>
        /// IsPartOf
        /// </summary>
        /// finnes i testdata: Appid_400.json har "isPartOf": null,
        // acc-man har "isPartOf": "altiinnportal",
        public string IsPartOf { get; set; }



        /// <summary>
        /// IsPublicService
        /// </summary>
        /// finnes i testdata: "isPublicService": true,
        public bool IsPublicService { get; set; }




        /// <summary>
        /// ThematicArea
        /// </summary>
        /// Kommentar 27.02.23: EU standard Eurovoc skal muligens
        /// brukes i Ressurs-prosjektet: se møtenotater Grooming 27.02.23
        // finnes i testdata: "thematicArea": null,
        /// acc-man har "thematicArea": "http://publications.europa.eu/resource/authority/eurovoc/2468",
        public string? ThematicArea { get; set; }




        /// <summary>
        /// ResourceReference
        /// </summary>
        /// Merk! typen ResourceReference er tatt fra Core.Models katalog
        /// altså samme.
        /// finnes i testdata: Appid_400.json har følgende struktur:
        /// "resourceReferences": [
        /// { 
        ///     "reference": "8f08210a-d792-48f5-9e27-0f029e41111e",
        ///     "referenceType": "DelegationSchemeId",
        ///     "referenceSource": "Altinn2"
        /// },
        /// { 
        ///     "reference": "nav:paa/v1/luring",
        ///     "referenceType": "MaskinportenScope",
        ///     "referenceSource": "Altinn2"
        /// },
        /// { 
        ///     "reference": "AppId:400",
        ///     "referenceType": "ServiceCode",
        ///     "referenceSource": "Altinn2"
        /// },
         /// { 
        ///     "reference": "1",
        ///     "referenceType": "ServiceEditionCode",
        ///     "referenceSource": "Altinn2"
        /// }
        /// ]
        public List<ResourceReference>? ResourceReferences { get; set;  }




        /// <summary>
        /// IsComplete
        /// </summary>
        /// finnes i testdata: "isComplete": false,
        public bool? IsComplete { get; set; }




        /// <summary>
        /// Is this resource possible to delegate to others or not
        /// </summary>
        /// MERK!!! Delegable mangler fra alle tre .json filer: Appid_400, altinn_access_management.json,
        /// og nav_tiltakAvtaleOmArbeidstrening.json ---> bør spørres om.
        public bool Delegable { get; set; } = true;
        



        /// <summary>
        /// The visibility of the resource
        /// </summary>
        /// MERK!!! Visible mangler fra alle tre .json filer: Appid_400, altinn_access_management.json,
        /// og nav_tiltakAvtaleOmArbeidstrening.json ---> bør spørres om.
        public bool Visible { get; set; } = true; 





        /// <summary>
        /// HasCompetentAuthority
        /// </summary>
        /// Kommentar: type er her CompetentAuthority, i samme namespace
        /// Det er også interessant at Dhana har oppdatert CompetentAuthority.cs 
        /// i issue #91 (closed: jeg går gjennom disse for å forstå prosjektet): 
        /// timeline viser der at Jon Kjetil sier
        /// "Documentation is still far from an acceptable quality level, 
        /// and will need iterative updates" ---> så C# har dokumentasjonskrav

        /// hasCompetentAuthority finnes i testdata: har struktur:
        /// "hasCompetentAuthority" : { 
        ///     "orgcode": "PAA",
        ///     "organization": "985399077",
        ///     "name": { 
        ///         "en": "DEPARTMENT OF HUMBUG",
        ///         "nb": "PÅFUNNSETATEN",
        ///         "nn": "PÅFUNNSETATEN"
        ///     }
        /// } 
        public CompetentAuthority HasCompetentAuthority { get; set; }




        /// <summary>
        /// Keywords
        /// </summary>
        /// finnes i testdata: "keywords": null, mangler fra acc-man.json
        /// nav...json har struktur:
        /// "keywords": [
        ///    {
        ///     "word": "Dødsbo",
        ///     "language": "nb"
        ///     }
        /// ]
        /// Denne strukturen passer med Core.Model.Keyword.cs fil
        public List<Keyword> Keywords { get; set; }




        /// <summary>
        /// Sector
        /// </summary>
        /// finnes i testdata: "sector": null,
        // acc-man.json og nav...json har "sector": [],

        public List<string> Sector { get; set; }



        /// <summary>
        /// ResourceType
        /// </summary>
        /// finnes i testdata: "resourceType": "MaskinportenSchema",
        /// mangler fra acc-man.json og nav...json
        /// Core.Enums.ResourceType.cs har e.g.  [PgName("maskinportenschema")]
        /// som et av flere valg
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResourceType ResourceType { get; set; }




        /// <summary>
        /// The fallback language of the resource
        /// </summary>
        /// MainLanguage mangler fra alle tre .json eksempler i testdata.
        public string MainLanguage { get; set; } = "nb";




        /// <summary>
        /// Writes key information when this object is written to Log.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Identifier: {Identifier}, ResourceType: {ResourceType}";
        }
    }
}
