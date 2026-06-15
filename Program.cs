using System.Security.Claims;
using System.Text;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.DTOs;
using ExpenseTracker.Api.Extensions;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true, 
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGet("/api/categories", async (ClaimsPrincipal user, AppDbContext context) =>
{
    var userId = user.GetUserId();

    var categories = await context.Categories
        .Where(c => c.UserId == userId)
        .Select(c => new CategoryResponse(c.Id, c.Name))
        .ToListAsync();

    return Results.Ok(categories);
})
.WithName("Categories")
.RequireAuthorization();

app.MapPost("/api/categories", async (ClaimsPrincipal user, CreateCategoryRequest request, AppDbContext context) =>
{
    var userId = user.GetUserId();

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
.WithName("CreateCategory")
.RequireAuthorization();

app.MapGet("/api/categories/{id:int}", async (ClaimsPrincipal user, int id, AppDbContext context) =>
{
    var userId = user.GetUserId();

    var category = await context.Categories
        .Where(c => c.Id == id && c.UserId == userId)
        .Select(c => new CategoryResponse(c.Id, c.Name))
        .FirstOrDefaultAsync();

    return category is null ? Results.NotFound() : Results.Ok(category);
})
.WithName("GetCategory")
.RequireAuthorization();

app.MapPut("/api/categories/{id:int}", async (ClaimsPrincipal user, int id, UpdateCategoryRequest request, AppDbContext context) =>
{
    var userId = user.GetUserId();

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
.WithName("UpdateCategory")
.RequireAuthorization();

app.MapDelete("/api/categories/{id:int}", async (ClaimsPrincipal user, int id, AppDbContext context) =>
{
    var userId = user.GetUserId();

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
.WithName("DeleteCategory")
.RequireAuthorization();

app.MapPost("/api/transactions", async (ClaimsPrincipal user, CreateTransactionRequest request, AppDbContext context) =>
{
    var userId = user.GetUserId();

    if (request.Amount <= 0)
        return Results.BadRequest("Le montant doit être supérieur à zéro.");
    if (string.IsNullOrWhiteSpace(request.Label))
        return Results.BadRequest("Le libellé est requis.");

    var categoryExists = await context.Categories
        .AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId);
    if (!categoryExists)
        return Results.BadRequest("La catégorie n'existe pas.");

    var transaction = new Transaction
    {
        Amount = request.Amount,
        Date = request.Date,
        Label = request.Label,
        CategoryId = request.CategoryId,
        UserId = userId
    };

    context.Transactions.Add(transaction);
    await context.SaveChangesAsync();

    var response = new TransactionResponse(transaction.Id, transaction.Amount, transaction.Date, transaction.Label, transaction.CategoryId);
    return Results.Created($"/api/transactions/{transaction.Id}", response);
})
.WithName("CreateTransaction")
.RequireAuthorization();

app.MapGet("/api/transactions", async (ClaimsPrincipal user, DateOnly? from, DateOnly? to, int? categoryId, AppDbContext context) =>
{
    var userId = user.GetUserId();

    var query = context.Transactions.Where(t => t.UserId == userId);

    if (from is not null)
        query = query.Where(t => t.Date >= from);
    if (to is not null)
        query = query.Where(t => t.Date <= to);
    if (categoryId is not null)
        query = query.Where(t => t.CategoryId == categoryId);

    var transactions = await query
        .Select(t => new TransactionResponse(t.Id, t.Amount, t.Date, t.Label, t.CategoryId))
        .ToListAsync();

    return Results.Ok(transactions);
})
.WithName("Transactions")
.RequireAuthorization();

app.MapGet("/api/transactions/{id:int}", async (ClaimsPrincipal user, int id, AppDbContext context) =>
{
    var userId = user.GetUserId();

    var transaction = await context.Transactions
        .Where(t => t.Id == id && t.UserId == userId)
        .Select(t => new TransactionResponse(t.Id, t.Amount, t.Date, t.Label, t.CategoryId))
        .FirstOrDefaultAsync();

    return transaction is null ? Results.NotFound() : Results.Ok(transaction);
})
.WithName("GetTransaction")
.RequireAuthorization();

app.MapPut("/api/transactions/{id:int}", async (ClaimsPrincipal user, int id, UpdateTransactionRequest request, AppDbContext context) =>
{
    var userId = user.GetUserId();

    if (request.Amount <= 0)
        return Results.BadRequest("Le montant doit être supérieur à zéro.");
    if (string.IsNullOrWhiteSpace(request.Label))
        return Results.BadRequest("Le libellé est requis.");

    var transaction = await context.Transactions
        .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (transaction is null)
        return Results.NotFound();

    var categoryExists = await context.Categories
        .AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId);
    if (!categoryExists)
        return Results.BadRequest("La catégorie n'existe pas.");

    transaction.Amount = request.Amount;
    transaction.Date = request.Date;
    transaction.Label = request.Label;
    transaction.CategoryId = request.CategoryId;
    await context.SaveChangesAsync();

    return Results.Ok(new TransactionResponse(transaction.Id, transaction.Amount, transaction.Date, transaction.Label, transaction.CategoryId));
})
.WithName("UpdateTransaction")
.RequireAuthorization();

app.MapDelete("/api/transactions/{id:int}", async (ClaimsPrincipal user, int id, AppDbContext context) =>
{
    var userId = user.GetUserId();

    var transaction = await context.Transactions
        .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (transaction is null)
        return Results.NotFound();

    context.Transactions.Remove(transaction);
    await context.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteTransaction")
.RequireAuthorization();

app.MapPost("/api/auth/register", async (RegisterRequest request, AppDbContext context, TokenService tokenService) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest("Email et mot de passe requis.");

    var emailTaken = await context.Users.AnyAsync(u => u.Email == request.Email);
    if (emailTaken)
        return Results.Conflict("Cet email est déjà utilisé.");

    var user = new User
    {
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    return Results.Ok(new AuthResponse(tokenService.CreateToken(user)));
})
.WithName("Register")
.RequireAuthorization();

app.MapPost("/api/auth/login", async (LoginRequest request, AppDbContext context, TokenService tokenService) =>
{
    var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    return Results.Ok(new AuthResponse(tokenService.CreateToken(user)));
})
.WithName("Login")
.RequireAuthorization();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
