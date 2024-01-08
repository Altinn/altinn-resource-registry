#nullable enable

using Altinn.ResourceRegistry.JsonPatch;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.ResourceRegistry.Controllers;

/// <summary>
/// Controller for party registries apis.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("resourceregistry/api/v1/party-registries")]
public class PartyRegistriesController 
    : Controller
{
    /// <summary>
    /// Get all party registries for a given owner.
    /// </summary>
    /// <param name="owner">The owner</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated set of <see cref="PartyRegistryInfoDto"/></returns>
    [HttpGet("{owner:required}")]
    [SwaggerOperation(Tags = ["Party Registry"])]
    public Task<ActionResult<Paginated<PartyRegistryInfoDto>>> GetPartyRegistries(string owner, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets a party registry by owner and identifier.
    /// </summary>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="PartyRegistryInfoDto"/></returns>
    [HttpGet("{owner:required}/{identifier:required}")]
    [SwaggerOperation(Tags = ["Party Registry"])]
    public Task<ActionResult<PartyRegistryInfoDto>> GetPartyRegistry(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Create or update a party registry.
    /// </summary>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="model">Information about the party registry</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="PartyRegistryInfoDto"/></returns>
    [HttpPut("{owner:required}/{identifier:required}")]
    [SwaggerOperation(Tags = ["Party Registry"])]
    public Task<ActionResult<PartyRegistryInfoDto>> UpsertPartyRegistry(string owner, string identifier, [FromBody] CreatePartyRegistryModel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Update a party registry.
    /// </summary>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="patch">The patch document containing what to update</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A <see cref="PartyRegistryInfoDto"/></returns>
    [HttpPatch("{owner:required}/{identifier:required}")]
    [SwaggerOperation(Tags = ["Party Registry"])]
    [Consumes("application/json-patch+json")]
    public Task<ActionResult<PartyRegistryInfoDto>> UpdatePartyRegistry(string owner, string identifier, [FromBody] JsonPatchDocument patch, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get party registry members.
    /// </summary>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="PartyRegistryMembershipDto"/></returns>
    [HttpGet("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Party Registry Members"])]
    public Task<ActionResult<Paginated<PartyRegistryMembershipDto>>> GetPartyRegistryMembers(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Replace party registry members.
    /// </summary>
    /// <remarks>
    /// This effectively overwrites all members with the ones included in the request. It should not be used for lists with more than 100 members.
    /// </remarks>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="members">The new members-list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="PartyRegistryMembershipDto"/></returns>
    [HttpPut("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Party Registry Members"])]
    public Task<ActionResult<Paginated<PartyRegistryMembershipDto>>> ReplacePartyRegistryMembers(string owner, string identifier, [FromBody] UpsertPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        if (members.Count > 100)
        {
            ActionResult<Paginated<PartyRegistryMembershipDto>> result = BadRequest("Cannot replace more than 100 members at a time. Use POST and DELETE methods instead.");
            return Task.FromResult(result);
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Add new members to a party registry.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a member already exists, it will not be added again.
    /// </remarks>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="members">The new members-list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="PartyRegistryMembershipDto"/></returns>
    [HttpPost("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Party Registry Members"])]
    public Task<ActionResult<Paginated<PartyRegistryMembershipDto>>> AddPartyRegistryMembers(string owner, string identifier, [FromBody] UpsertPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Remove members from a party registry if they exist.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a member does not exist, it will not be removed.
    /// </remarks>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="members">The new members-list</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A paginated list of <see cref="PartyRegistryMembershipDto"/></returns>
    [HttpDelete("{owner:required}/{identifier:required}/members")]
    [SwaggerOperation(Tags = ["Party Registry Members"])]
    public Task<ActionResult<Paginated<PartyRegistryMembershipDto>>> RemovePartyRegistryMembers(string owner, string identifier, [FromBody] UpsertPartyMembersListDto members, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get all resource connections for a party registry.
    /// </summary>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A parinated list of <see cref="PartyRegistryResourceConnectionDto"/></returns>
    [HttpGet("{owner:required}/{identifier:required}/resource-connections")]
    [SwaggerOperation(Tags = ["Party Registry Resource Connections"])]
    public Task<ActionResult<Paginated<PartyRegistryResourceConnectionDto>>> GetPartyRegistryResourceConnections(string owner, string identifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates or update a resource connection to a party registry.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a resource connection already exists, it will be updated.
    /// </remarks>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="model">The resource connection info</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The newly created/updated <see cref="PartyRegistryResourceConnectionDto"/></returns>
    [HttpPut("{owner:required}/{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Party Registry Resource Connections"])]
    public Task<ActionResult<PartyRegistryResourceConnectionDto>> UpsertPartyResourceConnection(string owner, string identifier, string resourceIdentifier, [FromBody] UpsertPartyResourceConnectionDto model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Removes a resource connection from a party registry if it exists.
    /// </summary>
    /// <remarks>
    /// This method is idempotent, meaning that if a resource connection does not exist, it will not be removed.
    /// </remarks>
    /// <param name="owner">The owner</param>
    /// <param name="identifier">The owner-unique identifier</param>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The newly removed <see cref="PartyRegistryResourceConnectionDto"/>, if it existed, otherwize returns no content</returns>
    [HttpDelete("{owner:required}/{identifier:required}/resource-connections/{resourceIdentifier:required}")]
    [SwaggerOperation(Tags = ["Party Registry Resource Connections"])]
    [SwaggerResponse(StatusCodes.Status200OK, description: "The resource connection was removed", type: typeof(PartyRegistryResourceConnectionDto))]
    [SwaggerResponse(StatusCodes.Status204NoContent, description: "The resource connection did not exist")]
    public Task<ActionResult<PartyRegistryResourceConnectionDto?>> DeletePartyResourceConnection(string owner, string identifier, string resourceIdentifier, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}