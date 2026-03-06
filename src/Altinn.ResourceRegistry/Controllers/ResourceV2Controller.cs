using System.Text;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.Authorization.ServiceDefaults;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using static Altinn.ResourceRegistry.Core.Constants.AltinnXacmlConstants;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// Controller responsible for all operations for managing resources in the resource registry
    /// </summary>
    [Route("resourceregistry/api/v2/resource")]
    [ApiController]
    public class ResourceV2Controller : ControllerBase
    {
        private readonly IResourceRegistry _resourceRegistry;
        private readonly ILogger<ResourceController> _logger;
        private readonly AltinnServiceDescriptor _serviceDescriptor;
        private readonly ActionTranslationsOptions _actionTranslationsOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceController"/> controller.
        /// </summary>
        public ResourceV2Controller(
            IResourceRegistry resourceRegistry,
            ILogger<ResourceController> logger,
            AltinnServiceDescriptor serviceDescriptor,
            IOptions<ActionTranslationsOptions> actionConfig)
        {
            _resourceRegistry = resourceRegistry;
            _logger = logger;
            _serviceDescriptor = serviceDescriptor;
            _actionTranslationsOptions = actionConfig.Value;
        }

        /// <summary>
        /// Returns a list of rights for a resource in V2 version with new model. A right is a combination of resource and action.
        /// Response is grouped by right.
        /// </summary>
        [HttpGet("{id}/policy/rights")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<ResourceDecomposedDto>> GetRights(string id, bool includeServiceOwnerRights = false, bool includeAppRights = false, CancellationToken cancellationToken = default)
        {
            List<Right> rights = await _resourceRegistry.GetPolicyRightsV2(id, includeServiceOwnerRights, includeAppRights, cancellationToken);

            string language = HttpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            if (language == null) 
            {
                language = "nb";
            }

            // Map to result
            IEnumerable<RightDto> decomposedRights = await MapFromInternalToDecomposedRights(rights, id, language, cancellationToken);

            return Ok(decomposedRights);
        }

        /// <summary>
        /// Returns a list of rights for a resource in V2 version with new model. A right is a combination of resource and action.
        /// </summary>
        [HttpGet("{id}/policy/subjectrights")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<IEnumerable<SubjectRightsDto>>> GetSubjectRights(string id, bool includeServiceOwnerRights = false, bool includeAppRights = false, CancellationToken cancellationToken = default)
        {
            List<Right> rights = await _resourceRegistry.GetPolicyRightsV2(id, includeServiceOwnerRights, includeAppRights, cancellationToken);

            string language = HttpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            if (language == null)
            {
                language = "nb";
            }

            // Map to result
            IEnumerable<SubjectRightsDto> decomposedRights = await MapFromInternalToSubjectRights(rights, id, language, cancellationToken);

            return Ok(decomposedRights);
        }

        private async Task<IEnumerable<SubjectRightsDto>> MapFromInternalToSubjectRights(List<Right> rights, string resource, string language, CancellationToken cancellationToken = default)
        {
            List<SubjectRightsDto> result = [];
            
            Dictionary<string, SubjectRightsDto> subjectRightsDictionary = new();

            foreach (Right right in rights)
            {
                foreach (string subject in right.AccessorUrns)
                {
                    if (!subjectRightsDictionary.TryGetValue(subject, out SubjectRightsDto? subjectRight))
                    {
                        subjectRight = new SubjectRightsDto()
                        {
                            Subject = [GetSubjectAttributes(subject)],
                            Rights = await MapFromInternalToDecomposedRights([right], resource, language, cancellationToken)
                        };

                        subjectRightsDictionary.Add(subject, subjectRight);
                        result.Add(subjectRight);
                    }
                    else
                    {
                        subjectRight.Rights.AddRange(await MapFromInternalToDecomposedRights([right], resource, language, cancellationToken));
                    }
                }
            }

            return result;
        }

        private async Task<List<RightDto>> MapFromInternalToDecomposedRights(List<Right> rights, string resource, string language, CancellationToken cancellationToken = default)
        {
            List<RightDto> result = [];

            foreach (var right in rights)
            {
                result.Add(await MapFromInternalToDecomposeRight(right, resource, language, cancellationToken));
            }

            return result;
        }

        private async Task<RightDto> MapFromInternalToDecomposeRight(Right rights, string resource, string language, CancellationToken cancellationToken)
        {
            ResourceAndAction resourceAndAction = DelegationCheckHelper.SplitRightKey(rights.Key);

            RightDto right = new()
            {
                Key = "01" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(rights.Key.ToLowerInvariant()))).ToLowerInvariant(),
                Name = GetActionNameFromRightKey(rights.Key, resource, language),
                Resource = rights.Resource.Select(m => new AttributeMatchDTO() { Type = m.Type, Value = m.Value }).ToList(),
                Action = new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.ActionId, Value = rights.Action.Value }
            };  
    
            return right;
        }

        private string GetActionNameFromRightKey(string key, string resourceId, string language)
        {
            string[] parts = key.Split("urn:", options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            StringBuilder sb = new();

            bool actionAdded = false;
            foreach (string part in parts.OrderDescending())
            {
                string currentPart = part;
                if (currentPart.Substring(currentPart.Length - 1, 1) == ":")
                {
                    currentPart = currentPart.Substring(0, currentPart.Length - 1);
                }

                int removeBefore = currentPart.LastIndexOf(':');
                if (removeBefore > -1)
                {
                    currentPart = currentPart.Substring(currentPart.LastIndexOf(':') + 1);
                }

                if (currentPart.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                if (part.StartsWith("oasis:names:tc:xacml:1.0:action:action-id"))
                {
                   currentPart = GetActionName(currentPart, language);
                   actionAdded = true;
                }
                else if (actionAdded)
                {
                    currentPart = "(" + currentPart + ")";
                }

                sb.Append(UppercaseFirstLetter(currentPart));
                sb.Append(' ');
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private static AttributeMatchDTO GetSubjectAttributes(string urn)
        {
            if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute))
            {
                return new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.RoleAttribute, Value = urn[(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute.Length + 1)..] };
            }
            else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCcrRoleAttribute))
            {
                return new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.ExternalCcrRoleAttribute, Value = urn[(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCcrRoleAttribute.Length + 1)..] };
            }
            else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCraRoleAttribute))
            {
                return new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.ExternalCraRoleAttribute, Value = urn[(AltinnXacmlConstants.MatchAttributeIdentifiers.ExternalCraRoleAttribute.Length + 1)..] };
            }
            else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute))
            {
                return new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.AccessPackageAttribute, Value = urn[(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute.Length + 1)..] };
            }
            else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute))
            {
                return new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.OrgAttribute, Value = urn[(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute.Length + 1)..] };
            }
            else if (urn.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.Delegation))
            {
                return new AttributeMatchDTO() { Type = MatchAttributeIdentifiers.Delegation, Value = urn[(AltinnXacmlConstants.MatchAttributeIdentifiers.Delegation.Length + 1)..] };
            }

            return null;
        }

        private string UppercaseFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        private string GetActionName(string actionName, string language)
        {
            if (language == null)
            {
                language = "nb";
            }

            if (actionName == null)
            {
                return actionName;
            }

            _actionTranslationsOptions.TryGetValue(language.ToLowerInvariant(), out Dictionary<string, string> actionDictionary);
            if (actionDictionary != null)
            {
                if (actionDictionary.TryGetValue(actionName.ToLowerInvariant(), out string translatedAction))
                {
                    return translatedAction;
                }
            }

            return actionName;
        }
    }
}
