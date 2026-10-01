using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MoveUp.Application.Services;
using MoveUp.Context;
using MoveUp.Domain.Interfaces;
using MoveUp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
var databasePath = builder.Configuration["Database:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "moveup.db");
if (!Path.IsPathRooted(databasePath)) databasePath = Path.Combine(builder.Environment.ContentRootPath, databasePath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
var connection = new SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true };
builder.Services.AddDbContext<MoveUpDbContext>(options => options.UseSqlite(connection.ToString()));
builder.Services.AddScoped<ITreinoRepository, TreinoRepository>();
builder.Services.AddScoped<IExercicioRepository, ExercicioRepository>();
builder.Services.AddScoped<TreinoService>();
builder.Services.AddScoped<ExercicioService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()!.Error;
    var status = exception switch
    {
        ApiException error => error.Status,
        DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 19 } } => 409,
        _ => 500
    };
    var detail = exception is ApiException known ? known.Message : status == 409
        ? "A operação conflita com os dados existentes. Recarregue e tente novamente."
        : "Não foi possível concluir a operação.";
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status,
        Title = status == 500 ? "Erro interno" : "Operação não concluída", Detail = detail });
}));
app.UseCors();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MoveUpDbContext>();
    await db.Database.MigrateAsync();
}
app.Run();

public partial class Program { }
