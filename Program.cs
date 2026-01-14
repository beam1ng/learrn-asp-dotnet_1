using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.VisualBasic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)","todos/$1"));
var todos = new List<Todo>();

app.MapGet("/todos/", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id)=>
{
    var targetTodo = todos.SingleOrDefault(t=>id==t.Id);
    return targetTodo is null
        ?TypedResults.NotFound()
        :TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", Results<Created<Todo>,Conflict<string>> (Todo task) =>
{
    if(todos.Any(t=>t.Id == task.Id))
        return TypedResults.Conflict($"Todo with id {task.Id} already exists.");

    todos.Add(task);
    return TypedResults.Created($"/todos/{task.Id}", task);
});

app.MapDelete("/todos/{id}", (int id) =>
{
    var matchedTodo = todos.RemoveAll(t=>id == t.Id);
    return TypedResults.NoContent();
});

var xd = new DateTime(new DateOnly(2026,01,1),new TimeOnly(10,01));


app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool IsCompleted){}
