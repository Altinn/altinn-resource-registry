using System.Text.RegularExpressions;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.Helpers
{
    /// <summary>
    /// ServiceResource helper methods
    /// </summary>
    public static class ServiceResourceHelper
    {

        private static readonly Regex ResourceRegex = new Regex("^[a-z0-9_-]+$", RegexOptions.Compiled);

        /// <summary>
        /// Gets resources from the resourcelist that fits the search criteria
        /// </summary>
        /// <param name="resourceList">The resourceList that needs to be searched</param>
        /// <param name="resourceSearch">The search criteria</param>
        public static List<ServiceResource> GetSearchResultsFromResourceList(List<ServiceResource> resourceList, ResourceSearch resourceSearch)
        {
            List<ServiceResource> searchResults = new List<ServiceResource>();

            foreach (ServiceResource serviceResource in resourceList)
            {
                if (MatchingIdentifier(serviceResource, resourceSearch) && MatchingDescription(serviceResource, resourceSearch) && MatchingResourceType(serviceResource, resourceSearch) && MatchingKeywords(serviceResource, resourceSearch))
                {
                    searchResults.Add(serviceResource);
                }
            }

            return searchResults;
        }

        /// <summary>
        /// Method to validate service resource
        /// </summary>
        public static bool ValidateResource(ServiceResource serviceResource, out string message)
        {
            if (serviceResource.Identifier.StartsWith(ResourceConstants.SERVICE_ENGINE_RESOURCE_PREFIX)
                && (serviceResource.ResourceReferences == null
                || !serviceResource.ResourceReferences.Any()
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceCode))
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ServiceEditionCode))))
            {
                // Uses Service engine prefix without it beeing a service engine resource
                message = "Invalid Prefix";
                return false;
            }

            if (serviceResource.Identifier.StartsWith(ResourceConstants.APPLICATION_RESOURCE_PREFIX)
                && (serviceResource.ResourceReferences == null
                || !serviceResource.ResourceReferences.Any()
                || !serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType.HasValue && rf.ReferenceType.Equals(ReferenceType.ApplicationId))))
            {
                // Uses app prefix without it beeing a app resource
                message = "Invalid Prefix. app_ is only for Altinn Studio apps";
                return false;
            }

            if (!ResourceRegex.IsMatch(serviceResource.Identifier))
            {
                message = "Invalid id. Only a-z and 0-9 is allowed together with _ and -";
                return false;
            }

            if (serviceResource.Title == null || serviceResource.Title.Count == 0)
            {
                message = "Missing title";
                return false;
            }

            if (!serviceResource.Title.ContainsKey(ResourceConstants.LANGUAGE_EN))
            {
                message = $"Missing title in english {ResourceConstants.LANGUAGE_EN}";
                return false;
            }

            if (!serviceResource.Title.ContainsKey(ResourceConstants.LANGUAGE_NB))
            {
                message = $"Missing title in bokmal {ResourceConstants.LANGUAGE_NB}";
                return false;
            }

            if (!serviceResource.Title.ContainsKey(ResourceConstants.LANGUAGE_NN))
            {
                message = $"Missing title in nynorsk {ResourceConstants.LANGUAGE_NN}";
                return false;
            }

            if (serviceResource.Delegable && (serviceResource.RightDescription == null || serviceResource.RightDescription.Count == 0))
            {
                message = "Missing RightDescription";
                return false;
            }

            if (serviceResource.Delegable && (!serviceResource.RightDescription.ContainsKey(ResourceConstants.LANGUAGE_EN)))
            {
                message = $"Missing RightDescription in english {ResourceConstants.LANGUAGE_EN}";
                return false;
            }

            if (serviceResource.Delegable && (!serviceResource.RightDescription.ContainsKey(ResourceConstants.LANGUAGE_NB)))
            {
                message = $"Missing RightDescription in bokmal {ResourceConstants.LANGUAGE_NB}";
                return false;
            }

            if (serviceResource.Delegable && (!serviceResource.RightDescription.ContainsKey(ResourceConstants.LANGUAGE_NN)))
            {
                message = $"Missing RightDescription in nynorsk {ResourceConstants.LANGUAGE_NN}";
                return false;
            }

            if (serviceResource.Description == null || serviceResource.Description.Count == 0)
            {
                message = "Missing Description";
                return false;
            }

            if (!serviceResource.Description.ContainsKey(ResourceConstants.LANGUAGE_EN))
            {
                message = $"Missing Description in english {ResourceConstants.LANGUAGE_EN}";
                return false;
            }

            if (!serviceResource.Description.ContainsKey(ResourceConstants.LANGUAGE_NB))
            {
                message = $"Missing Description in bokmal {ResourceConstants.LANGUAGE_NB}";
                return false;
            }

            if (!serviceResource.Description.ContainsKey(ResourceConstants.LANGUAGE_NN))
            {
                message = $"Missing Description in nynorsk {ResourceConstants.LANGUAGE_NN}";
                return false;
            }

            message = null;

            return true;
        }

        private static bool MatchingIdentifier(ServiceResource resource, ResourceSearch resourceSearch)
        {
            return resourceSearch.Id == null || resource.Identifier.Contains(resourceSearch.Id, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool MatchingDescription(ServiceResource resource, ResourceSearch resourceSearch)
        {
            if (resourceSearch.Description == null)
            {
                return true;
            }
            else
            {
                if (resource.Description == null)
                {
                    return false;
                }
                else
                {
                    return resource.Description.Any(d => d.Value.Contains(resourceSearch.Description, StringComparison.InvariantCultureIgnoreCase));
                }
            }
        }

        private static bool MatchingResourceType(ServiceResource resource, ResourceSearch resourceSearch)
        {
            return resourceSearch.ResourceType == null || resource.ResourceType == resourceSearch.ResourceType;
        }

        private static bool MatchingKeywords(ServiceResource resource, ResourceSearch resourceSearch)
        {
            if (resourceSearch.Keyword != null && resource.Keywords != null)
            {
                if (resourceSearch.Keyword != null && resource.Keywords.Count == 0)
                {
                    return false;
                }
                else
                {
                    return resource.Keywords.Exists(k => k.Word.Contains(resourceSearch.Keyword, StringComparison.InvariantCultureIgnoreCase));
                }
            }
            else
            {
                if ((resourceSearch.Keyword != null) && (resource.Keywords == null || resource.Keywords.Count == 0))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
