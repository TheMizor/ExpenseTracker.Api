using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/api/categories", async (AppDbContext context) =>
{
    const int userId = 1;

    var categories = await context.Categories
        .Where(c => c.UserId == userId)
        .Select(c => new CategoryResponse(c.Id, c.Name))
        .ToListAsync();

    return Results.Ok(categories);
})
.WithName("Categories");

app.MapPost("/api/categories", async (CreateCategoryRequest request, AppDbContext context) =>
{
    const int userId = 1;

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Le nom de la catégorie est requis.");

    var category = new Category
    {
        Name = request.Name,
        UserId = userId
    };

    context.Categories.Add(category);
    await context.SaveChangesAsync();

    var response = new CategoryResponse(category.Id, category.Name);
    return Results.Created($"/api/categories/{category.Id}", response);
})
.WithName("CreateCategory");

app.MapGet("/api/categories/{id:int}", async (int id, AppDbContext context) =>
{
    const int userId = 1;

    var category = await context.Categories
        .Where(c => c.Id == id && c.UserId == userId)
        .Select(c => new CategoryResponse(c.Id, c.Name))
        .FirstOrDefaultAsync();

    return category is null ? Results.NotFound() : Results.Ok(category);
})
.WithName("GetCategory");

app.MapPut("/api/categories/{id:int}", async (int id, UpdateCategoryRequest request, AppDbContext context) =>
{
    const int userId = 1;

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Le nom de la catégorie est requis.");

    var category = await context.Categories
        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    if (category is null)
        return Results.NotFound();

    category.Name = request.Name;
    await context.SaveChangesAsync();

    return Results.Ok(new CategoryResponse(category.Id, category.Name));
})
.WithName("UpdateCategory");

app.MapDelete("/api/categories/{id:int}", async (int id, AppDbContext context) =>
{
    const int userId = 1;

    var category = await context.Categories
        .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    if (category is null)
        return Results.NotFound();

    var hasTransactions = await context.Transactions.AnyAsync(t => t.CategoryId == id);
    if (hasTransactions)
        return Results.Conflict("Impossible de supprimer une catégorie qui contient des transactions.");

    context.Categories.Remove(category);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteCategory");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
