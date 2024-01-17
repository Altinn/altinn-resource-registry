using Altinn.ResourceRegistry.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// API controller for resource access subjects
    /// </summary>
    [Route("resourceregistry/api/v1/resourceaccesssubjects")]
    [ApiController]
    public class ResourceAccessSubjectsController : ControllerBase
    {
        /// <summary>
        /// List of resource access subjects. Either for a given subject or for a given resource.
        /// </summary>
        [HttpGet("subjectsforresource")]
        [Produces("application/json")]
        public async Task<List<SubjectAttribute>> SubjectList([FromQuery] string resource, [FromQuery] string subject, CancellationToken cancellationToken)
        {
            List<SubjectAttribute> resources = new List<SubjectAttribute>();
            resources.Add(new SubjectAttribute() { Type = "urn:altinn:rolecode:utinn" });
            resources.Add(new SubjectAttribute() { Resource = "urn:altinn:resource:ttd_test", ResourceOwner = "urn:altinn:org:skd", Subject = "urn:altinn:rolecode:utinn" });
            resources.Add(new SubjectAttribute() { Resource = "urn:altinn:resource:nav_sykemelding", ResourceOwner = "urn:altinn:org:skd", Subject = "urn:altinn:rolecode:utinn" });
            return resources;
        }
    }
}
