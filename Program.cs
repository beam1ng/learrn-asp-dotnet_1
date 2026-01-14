using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ITaskService>(new TemporaryTaskService());
var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

app.Use(async (context, next) =>
{

    System.Console.WriteLine($"Middleware_0 started.");
    await next(context);
    System.Console.WriteLine($"Middleware_0 ended.");
});
app.Use(async (context, next) =>
{

    System.Console.WriteLine($"Middleware_1 started.");
    await next(context);
    System.Console.WriteLine($"Middleware_1 ended.");
});

app.MapGet("/todos/", (ITaskService service) => service.GetTasks());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, ITaskService service) =>
{
    var targetTodo = service.GetTaskById(id);
    return targetTodo is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", Results<Created<Todo>, Conflict<string>> (Todo task, ITaskService service) =>
{
    service.CreateTask(task);
    return TypedResults.Created($"/todos/{task.Id}", task);
})
.AddEndpointFilter(async (context, next) =>
{
    Todo task = context.GetArgument<Todo>(0);
    Dictionary<string, string[]> errors = new();
    ITaskService? service = app.Services.GetService<ITaskService>();

    if (service is null)
        return TypedResults.InternalServerError("ITaskService was null.");

    if (task.DueDate < DateTime.UtcNow)
        errors.Add(nameof(task.DueDate), ["DueDate doesn't accept time from the past"]);

    if (task.IsCompleted)
        errors.Add(nameof(task.IsCompleted), ["Cannot add completed tasks."]);

    if (service.GetTaskById(task.Id) is not null)
        errors.Add(nameof(task.Id), ["Task of that Id already exists."]);

    if (errors.Count > 0)
    {
        return new ValidationProblemDetails(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id, ITaskService service) =>
{
    service.DeleteTaskById(id);
    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted) { }
public interface ITaskService
{
    public Todo? GetTaskById(int Id);
    public List<Todo> GetTasks();
    public void DeleteTaskById(int Id);
    public void CreateTask(Todo task);
}

public class TemporaryTaskService : ITaskService
{
    private List<Todo> _todos = new();

    public List<Todo> GetTasks() => _todos;

    public Todo? GetTaskById(int Id)
    {
        return _todos.FirstOrDefault<Todo>(t => t.Id == Id);
    }

    public void CreateTask(Todo task)
    {
        _todos.Add(task);
    }

    public void DeleteTaskById(int Id)
    {
        var task = _todos.SingleOrDefault<Todo>(t => t.Id == Id);
        if (task is null)
            return;

        _todos.Remove(task);
    }
}