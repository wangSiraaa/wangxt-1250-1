using Microsoft.AspNetCore.Mvc;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;

namespace TowerCraneSafetyMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _service;

    public PersonsController(IPersonService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Person>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Person>> GetById(int id)
    {
        var person = await _service.GetByIdAsync(id);
        if (person == null) return NotFound();
        return Ok(person);
    }

    [HttpGet("drivers")]
    public async Task<ActionResult<IEnumerable<Person>>> GetDrivers()
    {
        return Ok(await _service.GetDriversAsync());
    }

    [HttpGet("safety-officers")]
    public async Task<ActionResult<IEnumerable<Person>>> GetSafetyOfficers()
    {
        return Ok(await _service.GetSafetyOfficersAsync());
    }

    [HttpGet("supervisors")]
    public async Task<ActionResult<IEnumerable<Person>>> GetSupervisors()
    {
        return Ok(await _service.GetSupervisorsAsync());
    }

    [HttpGet("{id}/qualification-check")]
    public async Task<ActionResult<bool>> CheckQualification(int id)
    {
        try
        {
            return Ok(await _service.CheckDriverQualificationAsync(id));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/qualification-issues")]
    public async Task<ActionResult<List<string>>> GetQualificationIssues(int id)
    {
        try
        {
            return Ok(await _service.GetDriverQualificationIssuesAsync(id));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<Person>> Create(Person person)
    {
        try
        {
            var created = await _service.CreateAsync(person);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Person person)
    {
        if (id != person.Id) return BadRequest();
        try
        {
            return Ok(await _service.UpdateAsync(person));
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
