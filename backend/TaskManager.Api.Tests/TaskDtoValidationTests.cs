
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
}
