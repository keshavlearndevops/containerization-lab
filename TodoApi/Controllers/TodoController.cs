using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodoController : ControllerBase
{
    private static readonly List<Todo> _todos = new()
    {
        new Todo { Id = 1, Title = "Learn Docker",       Description = "Containerize the app",       IsCompleted = false },
        new Todo { Id = 2, Title = "Learn Kubernetes",   Description = "Deploy app to K8s cluster",  IsCompleted = false },
        new Todo { Id = 3, Title = "Build .NET 8 API",   Description = "Create REST API with Swagger", IsCompleted = true  },
    };

    private static int _nextId = 4;

    /// <summary>Get all todo items.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Todo>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Todo>> GetAll() => Ok(_todos);

    /// <summary>Get a todo item by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Todo> GetById(int id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        return todo is null ? NotFound() : Ok(todo);
    }

    /// <summary>Create a new todo item.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Todo), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Todo> Create([FromBody] Todo todo)
    {
        todo.Id = _nextId++;
        todo.CreatedAt = DateTime.UtcNow;
        _todos.Add(todo);
        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo);
    }

    /// <summary>Update an existing todo item.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Update(int id, [FromBody] Todo updated)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is null) return NotFound();

        todo.Title       = updated.Title;
        todo.Description = updated.Description;
        todo.IsCompleted = updated.IsCompleted;
        return NoContent();
    }

    /// <summary>Delete a todo item.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is null) return NotFound();

        _todos.Remove(todo);
        return NoContent();
    }
}
