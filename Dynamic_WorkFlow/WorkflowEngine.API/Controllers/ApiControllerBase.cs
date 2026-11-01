using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
}
