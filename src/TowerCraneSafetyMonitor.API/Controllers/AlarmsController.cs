using Microsoft.AspNetCore.Mvc;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;

namespace TowerCraneSafetyMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlarmsController : ControllerBase
{
    private readonly IAlarmService _service;

    public AlarmsController(IAlarmService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Alarm>> GetById(int id)
    {
        var alarm = await _service.GetByIdAsync(id);
        if (alarm == null) return NotFound();
        return Ok(alarm);
    }

    [HttpGet("tower-crane/{towerCraneId}")]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetByTowerCraneId(int towerCraneId)
    {
        return Ok(await _service.GetByTowerCraneIdAsync(towerCraneId));
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetByStatus(AlarmStatus status)
    {
        return Ok(await _service.GetByStatusAsync(status));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<Alarm>>> GetPending()
    {
        return Ok(await _service.GetPendingAlarmsAsync());
    }

    [HttpGet("blocking/task/{taskId}")]
    public async Task<ActionResult<List<Alarm>>> GetBlockingForTask(int taskId)
    {
        try
        {
            return Ok(await _service.GetBlockingAlarmsForTaskAsync(taskId));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("blocking/tower-crane/{towerCraneId}")]
    public async Task<ActionResult<List<Alarm>>> GetBlockingForTowerCrane(int towerCraneId)
    {
        try
        {
            return Ok(await _service.GetBlockingAlarmsForTowerCraneAsync(towerCraneId));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<Alarm>> Create(Alarm alarm)
    {
        try
        {
            var created = await _service.CreateAsync(alarm);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/start-processing")]
    public async Task<IActionResult> StartProcessing(int id, [FromBody] HandlerRequest request)
    {
        try
        {
            return Ok(await _service.StartProcessingAsync(id, request.HandledById));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveAlarmRequest request)
    {
        try
        {
            return Ok(await _service.ResolveAsync(id, request.Action, request.Remarks, request.RequiresRectification));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/ignore")]
    public async Task<IActionResult> Ignore(int id, [FromBody] IgnoreAlarmRequest request)
    {
        try
        {
            return Ok(await _service.IgnoreAsync(id, request.HandledById, request.Reason));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public record HandlerRequest(int HandledById);
public record ResolveAlarmRequest(string Action, string Remarks, bool RequiresRectification);
public record IgnoreAlarmRequest(int HandledById, string Reason);
