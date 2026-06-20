using Microsoft.AspNetCore.Mvc;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;

namespace TowerCraneSafetyMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RectificationsController : ControllerBase
{
    private readonly IRectificationService _service;

    public RectificationsController(IRectificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rectification>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Rectification>> GetById(int id)
    {
        var rectification = await _service.GetByIdAsync(id);
        if (rectification == null) return NotFound();
        return Ok(rectification);
    }

    [HttpGet("tower-crane/{towerCraneId}")]
    public async Task<ActionResult<IEnumerable<Rectification>>> GetByTowerCraneId(int towerCraneId)
    {
        return Ok(await _service.GetByTowerCraneIdAsync(towerCraneId));
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Rectification>>> GetByStatus(RectificationStatus status)
    {
        return Ok(await _service.GetByStatusAsync(status));
    }

    [HttpGet("open")]
    public async Task<ActionResult<IEnumerable<Rectification>>> GetOpen()
    {
        return Ok(await _service.GetOpenRectificationsAsync());
    }

    [HttpPost]
    public async Task<ActionResult<Rectification>> Create(Rectification rectification)
    {
        try
        {
            var created = await _service.CreateAsync(rectification);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Rectification rectification)
    {
        if (id != rectification.Id) return BadRequest();
        try
        {
            return Ok(await _service.UpdateAsync(rectification));
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

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            return Ok(await _service.StartRectificationAsync(id));
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

    [HttpPost("{id}/submit-review")]
    public async Task<IActionResult> SubmitForReview(int id, [FromBody] ActionsRequest request)
    {
        try
        {
            return Ok(await _service.SubmitForReviewAsync(id, request.Actions));
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

    [HttpPost("{id}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewRequest request)
    {
        try
        {
            return Ok(await _service.ReviewAsync(id, request.ReviewerId, request.Approved, request.ReviewResult));
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

    [HttpPost("{id}/close")]
    public async Task<IActionResult> Close(int id)
    {
        try
        {
            return Ok(await _service.CloseAsync(id));
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

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectRequest request)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, request.ReviewerId, request.ReviewResult));
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

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest request)
    {
        try
        {
            return Ok(await _service.AssignAsync(id, request.AssignedToId));
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

    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromBody] ExecuteRequest request)
    {
        try
        {
            return Ok(await _service.ExecuteAsync(id, request.ActionsTaken, request.Results));
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

public record ActionsRequest(string Actions);
public record ReviewRequest(int ReviewerId, bool Approved, string ReviewResult, string Comments = null);
public record RejectRequest(int ReviewerId, string ReviewResult);
public record AssignRequest(int AssignedToId);
public record ExecuteRequest(string ActionsTaken, string Results);
