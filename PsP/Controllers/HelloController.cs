using Microsoft.AspNetCore.Mvc;
namespace PsP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello from controller");
    }
}