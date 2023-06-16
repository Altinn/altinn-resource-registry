﻿using Altinn.ResourceRegistry.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// The resource owner controller
    /// </summary>
    [Route("resourceregistry/api/v1/resource/orgs")]
    [ApiController]
    public class ResourceOwnerController : ControllerBase
    {
        private readonly IOrgListClient _orgListClient;

        /// <summary>
        ///  The controller for resource owners
        /// </summary>
        public ResourceOwnerController(IOrgListClient orgListClient)
        {
            _orgListClient = orgListClient;
        }

        /// <summary>
        /// Endpoint to get the org list from CDN
        /// </summary>
        /// <returns></returns>
        [HttpGet]   
        public async Task<ActionResult> GetAltinnResourceOwners()
        {
           return Ok(await _orgListClient.GetOrgList());
        }
    }
}
