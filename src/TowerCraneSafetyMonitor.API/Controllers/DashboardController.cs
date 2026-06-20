using Microsoft.AspNetCore.Mvc;
using TowerCraneSafetyMonitor.Application.Interfaces;

namespace TowerCraneSafetyMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        try
        {
            return Ok(await _service.GetDashboardStatsAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("alarm-trend")]
    public async Task<ActionResult<IEnumerable<AlarmTrendData>>> GetAlarmTrend([FromQuery] int days = 7)
    {
        try
        {
            return Ok(await _service.GetAlarmTrendAsync(days));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("task-status-distribution")]
    public async Task<ActionResult<IEnumerable<TaskStatusDistribution>>> GetTaskStatusDistribution()
    {
        try
        {
            return Ok(await _service.GetTaskStatusDistributionAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("crane-workload")]
    public async Task<ActionResult<IEnumerable<CraneWorkloadData>>> GetCraneWorkload()
    {
        try
        {
            return Ok(await _service.GetCraneWorkloadAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("alarm-type-distribution")]
    public async Task<ActionResult<IEnumerable<AlarmTypeDistribution>>> GetAlarmTypeDistribution()
    {
        try
        {
            return Ok(await _service.GetAlarmTypeDistributionAsync());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
