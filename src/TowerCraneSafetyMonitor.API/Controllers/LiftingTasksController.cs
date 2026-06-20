using Microsoft.AspNetCore.Mvc;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;

namespace TowerCraneSafetyMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LiftingTasksController : ControllerBase
{
    private readonly ILiftingTaskService _service;

    public LiftingTasksController(ILiftingTaskService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LiftingTask>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LiftingTask>> GetById(int id)
    {
        var task = await _service.GetByIdAsync(id);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpGet("tower-crane/{towerCraneId}")]
    public async Task<ActionResult<IEnumerable<LiftingTask>>> GetByTowerCraneId(int towerCraneId)
    {
        return Ok(await _service.GetByTowerCraneIdAsync(towerCraneId));
    }

    [HttpGet("driver/{driverId}")]
    public async Task<ActionResult<IEnumerable<LiftingTask>>> GetByDriverId(int driverId)
    {
        return Ok(await _service.GetByDriverIdAsync(driverId));
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<LiftingTask>>> GetByStatus(TaskStatus status)
    {
        return Ok(await _service.GetByStatusAsync(status));
    }

    [HttpGet("{id}/validate-start")]
    public async Task<ActionResult<List<string>>> ValidateStart(int id)
    {
        try
        {
            return Ok(await _service.ValidateTaskStartAsync(id));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("validate-creation")]
    public async Task<ActionResult<List<string>>> ValidateCreation([FromBody] LiftingTask task)
    {
        try
        {
            return Ok(await _service.ValidateTaskCreationAsync(task));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<LiftingTask>> Create(LiftingTask task)
    {
        try
        {
            var created = await _service.CreateAsync(task);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, LiftingTask task)
    {
        if (id != task.Id) return BadRequest();
        try
        {
            return Ok(await _service.UpdateAsync(task));
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
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

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        try
        {
            return Ok(await _service.SubmitTaskAsync(id));
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

    [HttpPost("{id}/driver-confirm")]
    public async Task<IActionResult> DriverConfirm(int id, [FromBody] DriverConfirmRequest request)
    {
        try
        {
            return Ok(await _service.DriverConfirmAsync(id, request.DriverId));
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

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            return Ok(await _service.StartTaskAsync(id));
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

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            return Ok(await _service.CompleteTaskAsync(id));
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

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequest request)
    {
        try
        {
            return Ok(await _service.CancelTaskAsync(id, request.Reason));
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

public record DriverConfirmRequest(int DriverId);
public record CancelRequest(string Reason);
