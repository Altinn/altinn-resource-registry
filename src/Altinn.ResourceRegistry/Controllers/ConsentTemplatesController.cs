using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// Controller responsible for getting consent templates
    /// </summary>
    [Route("resourceregistry/api/v1/consent-templates")]
    [ApiController]
    public class ConsentTemplatesController : ControllerBase
    {
        private readonly IConsentTemplatesService _consentTemplatesService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsentTemplatesController"/> controller.
        /// </summary>
        public ConsentTemplatesController(IConsentTemplatesService consentTemplatesService)
        {
            _consentTemplatesService = consentTemplatesService;
        }

        /// <summary>
        /// Get list of all consent templates. Will return newest version of each template.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>List of <see cref="ConsentTemplate"/></returns>
        [HttpGet("")]
        [Produces("application/json")]
        public async Task<List<ConsentTemplate>> GetConsentTemplateList(CancellationToken cancellationToken)
        {
            return await _consentTemplatesService.GetConsentTemplates(cancellationToken);
        }

        /// <summary>
        /// Get a single consent template by id. If version is not specified, the newest version will be returned.
        /// </summary>
        /// <param name="id">Template id to get</param>
        /// <param name="version">Specific template version</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns>A single <see cref="ConsentTemplate"/></returns>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ConsentTemplate>> GetConsentTemplate([FromRoute] string id, [FromQuery] int? version = null, CancellationToken cancellationToken = default)
        {
            var template = await _consentTemplatesService.GetConsentTemplate(id, version, cancellationToken);
            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }
    }
}
