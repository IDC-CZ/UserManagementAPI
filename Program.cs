var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add exception handling middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var result = System.Text.Json.JsonSerializer.Serialize(new { error = error.Error.Message });
            await context.Response.WriteAsync(result);
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// In-memory user list
var users = new List<User>();
var nextId = 1;

// CREATE
app.MapPost("/users", (User user) =>
{
    user.Id = nextId++;
    users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
});

// READ ALL
app.MapGet("/users", () => users);

// READ BY ID
app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// UPDATE
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();
    user.Name = updatedUser.Name;
    return Results.Ok(user);
});

// DELETE
app.MapDelete("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();
    users.Remove(user);
    return Results.NoContent();
});

app.Run();

record User
{
    public int Id { get; set; }
    public string Name { get; set; }
}