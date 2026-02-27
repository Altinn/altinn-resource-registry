using System.Linq;
using System.Text;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.Authorization.ServiceDefaults;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceController"/> controller.
        /// </summary>
        public ResourceV2Controller(
            IResourceRegistry resourceRegistry,
            ILogger<ResourceController> logger,
            AltinnServiceDescriptor serviceDescriptor)
        {
            _resourceRegistry = resourceRegistry;
            _logger = logger;
            _serviceDescriptor = serviceDescriptor;
        }

        /// <summary>
        /// Returns a list of rights for a resource in V2 version with new model. A right is a combination of resource and action.
        /// Response is grouped by right.
        /// </summary>
        [HttpGet("{id}/policy/rights")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<ActionResult<ResourceDecomposedDto>> GetRights(string id, CancellationToken cancellationToken = default)
        {
            List<Right> rights = await _resourceRegistry.GetPolicyRightsV2(id, cancellationToken);

            // Map to result
            IEnumerable<RightDecomposedDto> decomposedRights = await MapFromInternalToDecomposedRights(rights, id, cancellationToken);

            // build result with reason based on roles, packages, resource rights and users delegable
            ResourceDecomposedDto resourceDecomposedDto = new ResourceDecomposedDto
            {
                Rights = decomposedRights
            };

            return resourceDecomposedDto;
        }

        private async Task<IEnumerable<RightDecomposedDto>> MapFromInternalToDecomposedRights(List<Right> rights, string resource, CancellationToken cancellationToken = default)
        {
            List<RightDecomposedDto> result = [];

            foreach (var right in rights)
            {
                result.Add(await MapFromInternalToDecomposeRight(right, resource, cancellationToken));
            }

            return result;
        }

        private async Task<RightDecomposedDto> MapFromInternalToDecomposeRight(Right rights, string resource, CancellationToken cancellationToken)
        {
            ResourceAndAction resourceAndAction = DelegationCheckHelper.SplitRightKey(rights.Key);

            RightDecomposedDto currentAction = new()
            {
                Right = new RightDto
                {
                    Key = rights.Key,
                    Name = GetActionNameFromRightKey(rights.Key, resource),
                    Resource = resourceAndAction.Resource,
                    Action = resourceAndAction.Action
                }
            };

            return currentAction;
        }

        private string GetActionNameFromRightKey(string key, string resourceId)
        {
            string[] parts = key.Split("urn:", options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            StringBuilder sb = new();

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

                sb.Append(UppercaseFirstLetter(currentPart));
                sb.Append(' ');
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private string UppercaseFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToUpper(input[0]) + input.Substring(1);
        }


        private string GetActionName(string actionName, string language) {
            {

            }
    }
}
