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

    // Proyección traducible a SQL: devuelve solo datos seguros del usuario.
    private static IQueryable<TaskResponseDto> ProjectTasks(
        IQueryable<TaskItem> tasks)
    {
        return tasks.Select(t => new TaskResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            IsCompleted = t.IsCompleted,
            Status = t.Status,
            Priority = t.Priority,
            Category = t.Category,
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            UserId = t.UserId,
            CreatedByUserId = t.CreatedByUserId,
            CreatedByUserName = t.CreatedByUser != null
                ? t.CreatedByUser.UserName
                : null,
            AssignedToUserId = t.AssignedToUserId,
            AssignedToUserName = t.AssignedToUser != null
                ? t.AssignedToUser.UserName
                : null
        });
    }

    // GET: api/tasks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetTasks()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Unauthorized();

        var tasks = await ProjectTasks(_context.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks);
    }

    // GET: api/tasks/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Unauthorized();

        var task = await ProjectTasks(_context.Tasks.AsNoTracking()
                .Where(t => t.Id == id && t.UserId == userId))
            .FirstOrDefaultAsync();

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> CreateTask(
        CreateTaskDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Unauthorized();

        var now = DateTime.UtcNow;

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = TaskWorkflowStatus.Pending,
            IsCompleted = false,
            Priority = dto.Priority ?? TaskPriority.Medium,
            Category = dto.Category ?? TaskCategory.Other,
            DueDate = dto.DueDate,
            CreatedAt = now,
            UpdatedAt = now,
            UserId = userId,
            CreatedByUserId = userId,
            AssignedToUserId = userId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var response = await ProjectTasks(_context.Tasks.AsNoTracking()
                .Where(t => t.Id == task.Id && t.UserId == userId))
            .FirstAsync();

        return CreatedAtAction(
            nameof(GetTask),
            new { id = task.Id },
            response);
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
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        task.Title = dto.Title;
        task.Description = dto.Description;

        if (dto.Status != null)
        {
            task.Status = dto.Status;
            task.IsCompleted =
                dto.Status == TaskWorkflowStatus.Completed;
        }
        else if (dto.IsCompleted.HasValue)
        {
            task.IsCompleted = dto.IsCompleted.Value;
            task.Status = dto.IsCompleted.Value
                ? TaskWorkflowStatus.Completed
                : TaskWorkflowStatus.Pending;
        }

        // Si el frontend antiguo no envía estos campos, se conservan.
        if (dto.Priority != null)
            task.Priority = dto.Priority;

        if (dto.Category != null)
            task.Category = dto.Category;

        if (dto.ClearDueDate)
            task.DueDate = null;
        else if (dto.DueDate.HasValue)
            task.DueDate = dto.DueDate.Value;

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PATCH: api/tasks/5/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateTaskStatus(
        int id,
        UpdateTaskStatusDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Unauthorized();

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        task.Status = dto.Status;
        task.IsCompleted =
            dto.Status == TaskWorkflowStatus.Completed;
        task.UpdatedAt = DateTime.UtcNow;

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
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
