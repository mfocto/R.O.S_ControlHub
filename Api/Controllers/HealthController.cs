using Microsoft.AspNetCore.Mvc;

namespace ROS_ControlHub.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 헬스체크
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            timeUtc = DateTimeOffset.UtcNow
        });
    }
}