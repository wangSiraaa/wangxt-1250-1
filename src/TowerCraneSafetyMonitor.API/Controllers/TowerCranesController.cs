using Microsoft.AspNetCore.Mvc;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;

namespace TowerCraneSafetyMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TowerCranesController : ControllerBase
{
    private readonly ITowerCraneService _service;

    public TowerCranesController(ITowerCraneService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TowerCrane>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TowerCrane>> GetById(int id)
    {
        var crane = await _service.GetByIdAsync(id);
        if (crane == null) return NotFound();
        return Ok(crane);
    }

    [HttpGet("{id}/can-execute/{riskLevel}")]
    public async Task<ActionResult<bool>> CanExecuteRiskLevelTask(int id, TaskRiskLevel riskLevel)
    {
        try
        {
            return Ok(await _service.CanExecuteRiskLevelTaskAsync(id, riskLevel));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/has-pending-critical-alarms")]
    public async Task<ActionResult<bool>> HasPendingCriticalAlarms(int id)
    {
        try
        {
            return Ok(await _service.HasPendingCriticalAlarmsAsync(id));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/has-open-rectification")]
    public async Task<ActionResult<bool>> HasOpenRectification(int id)
    {
        try
        {
            return Ok(await _service.HasOpenRectificationAsync(id));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<TowerCrane>> Create(TowerCrane towerCrane)
    {
        try
        {
            var created = await _service.CreateAsync(towerCrane);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, TowerCrane towerCrane)
    {
        if (id != towerCrane.Id) return BadRequest();
        try
        {
            return Ok(await _service.UpdateAsync(towerCrane));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TowerCraneStatus status)
    {
        try
        {
            await _service.UpdateStatusAsync(id, status);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
