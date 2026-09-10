using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services;
using AgentMesh.Authentication;
using AgentMesh.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentMesh.Controllers
{
    [ApiController]
    [Route("api/requests")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
    public sealed class RequestsController(AppInstance appInstance) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(WorkflowResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WorkflowResult>> Post([FromBody] ProcessRequestApiInput request, CancellationToken cancellationToken)
        {
            var result = await appInstance.ProcessRequest(request.Message, cancellationToken);
            return Ok(result);
        }
    }
}
