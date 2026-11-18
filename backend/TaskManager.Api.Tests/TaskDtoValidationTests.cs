
using System.ComponentModel.DataAnnotations;
using TaskManager.Api.DTOs;
using Xunit;

namespace TaskManager.Api.Tests;

public class TaskDtoValidationTests
{
    private static List<ValidationResult> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);

        Validator.TryValidateObject(
            dto,
            context,
            results,
            validateAllProperties: true
        );

        return results;
    }

    [Fact]
    public void CreateTask_WithValidTitle_ShouldPass()
    {
        var dto = new CreateTaskDto
        {
            Title = "Implement authentication",
            Description = "Add JWT authentication"
        };

        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    public void CreateTask_WithInvalidTitle_ShouldFail(string title)
    {
        var dto = new CreateTaskDto { Title = title };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void CreateTask_WithTitleOver100Characters_ShouldFail()
    {
        var dto = new CreateTaskDto
        {
            Title = new string('A', 101)
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void CreateTask_WithDescriptionOver500Characters_ShouldFail()
    {
        var dto = new CreateTaskDto
        {
            Title = "Valid title",
            Description = new string('A', 501)
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void CreateTask_WithoutDescription_ShouldPass()
    {
        var dto = new CreateTaskDto
        {
            Title = "Valid title"
        };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void UpdateTask_WithValidData_ShouldPass()
    {
        var dto = new UpdateTaskDto
        {
            Title = "Updated task",
            Description = "Updated description",
            IsCompleted = true
        };

        Assert.Empty(Validate(dto));
    }

    [Fact]
    public void UpdateTask_WithShortTitle_ShouldFail()
    {
        var dto = new UpdateTaskDto
        {
            Title = "AB",
            IsCompleted = false
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Theory]
    [InlineData("low")]
    [InlineData("medium")]
    [InlineData("high")]
    [InlineData("urgent")]
    public void CreateTask_WithValidPriority_ShouldPass(string priority)
    {
        Assert.Empty(Validate(new CreateTaskDto
        {
            Title = "Valid task", Priority = priority
        }));
    }

    [Theory]
    [InlineData("critical")]
    [InlineData("HIGH")]
    [InlineData("unknown")]
    public void CreateTask_WithInvalidPriority_ShouldFail(string priority)
    {
        Assert.NotEmpty(Validate(new CreateTaskDto
        {
            Title = "Valid task", Priority = priority
        }));
    }

    [Theory]
    [InlineData("frontend")]
    [InlineData("backend")]
    [InlineData("database")]
    [InlineData("documentation")]
    [InlineData("other")]
    public void CreateTask_WithValidCategory_ShouldPass(string category)
    {
        Assert.Empty(Validate(new CreateTaskDto
        {
            Title = "Valid task", Category = category
        }));
    }

    [Theory]
    [InlineData("devops")]
    [InlineData("BACKEND")]
    public void CreateTask_WithInvalidCategory_ShouldFail(string category)
    {
        Assert.NotEmpty(Validate(new CreateTaskDto
        {
            Title = "Valid task", Category = category
        }));
    }

    [Fact]
    public void CreateTask_WithOptionalDueDate_ShouldPass()
    {
        Assert.Empty(Validate(new CreateTaskDto
        {
            Title = "Valid task", DueDate = new DateOnly(2026, 12, 10)
        }));
    }

    [Fact]
    public void UpdateTask_WithValidDetails_ShouldPass()
    {
        Assert.Empty(Validate(new UpdateTaskDto
        {
            Title = "Valid task",
            Status = "in_progress",
            Priority = "urgent",
            Category = "database",
            DueDate = new DateOnly(2026, 12, 10)
        }));
    }

    [Theory]
    [InlineData("done", "high", "backend")]
    [InlineData("pending", "super", "backend")]
    [InlineData("pending", "high", "sales")]
    public void UpdateTask_WithInvalidDetails_ShouldFail(
        string status, string priority, string category)
    {
        Assert.NotEmpty(Validate(new UpdateTaskDto
        {
            Title = "Valid task", Status = status,
            Priority = priority, Category = category
        }));
    }
}
