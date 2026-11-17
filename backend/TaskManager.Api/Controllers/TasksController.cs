
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TasksController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: api/tasks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var tasks = await _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks);
    }

    // GET: api/tasks/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskItem>> GetTask(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var task = await _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskItem>> CreateTask(
        CreateTaskDto dto)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetTask),
            new { id = task.Id },
            task);
    }

    // PUT: api/tasks/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTask(
        int id,
        UpdateTaskDto dto)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.IsCompleted = dto.IsCompleted;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/tasks/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Unauthorized();

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t =>
                t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
