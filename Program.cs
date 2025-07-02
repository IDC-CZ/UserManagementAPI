using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Custom exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
        await context.Response.WriteAsync(result);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// In-memory user dictionary for O(1) lookups
var users = new Dictionary<int, User>();
var nextId = 1;

// CREATE
app.MapPost("/users", (User user) =>
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(user, null, null);
    if (!Validator.TryValidateObject(user, context, validationResults, true))
    {
        var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
        return Results.BadRequest(new { errors });
    }

    user.Id = nextId++;
    users[user.Id] = user;
    return Results.Created($"/users/{user.Id}", user);
});

// READ ALL
app.MapGet("/users", () => users.Values);

// READ BY ID
app.MapGet("/users/{id:int}", (int id) =>
{
    return users.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound();
});

// UPDATE
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    if (!users.TryGetValue(id, out var user))
        return Results.NotFound();

    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(updatedUser, null, null);
    if (!Validator.TryValidateObject(updatedUser, context, validationResults, true))
    {
        var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
        return Results.BadRequest(new { errors });
    }

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    users[id] = user;
    return Results.Ok(user);
});

// DELETE
app.MapDelete("/users/{id:int}", (int id) =>
{
    if (!users.Remove(id))
        return Results.NotFound();
    return Results.NoContent();
});

app.Run();

record User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; }
}