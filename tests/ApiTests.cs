using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MoveUp.Context;
using MoveUp.DTOs.Exercicio;
using MoveUp.DTOs.Treino;
using MoveUp.Entities;
using MoveUp.Infrastructure.Repositories;
using Xunit;

namespace MoveUp.Tests;

public class ApiTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "moveup-tests-" + Guid.NewGuid());
    private string DatabasePath => Path.Combine(directory, "test.db");
    private TestApp Factory() => new(DatabasePath);
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    private static TreinoCreateDto Plan(params Guid[] ids) => new(" Peito ", 1, " Observação ",
        ids.Select(id => (TreinoItemDto?)new TreinoItemDto(id, 4, 10, 72.5, 90)).ToList());
    private static async Task<List<ExercicioResponseDto>> Catalog(HttpClient client) =>
        (await client.GetFromJsonAsync<List<ExercicioResponseDto>>("/api/exercicios"))!;

    [Fact]
    public async Task CreateReadEditDeletePreservesCatalog()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        var catalog = await Catalog(client);
        Assert.Equal(12, catalog.Count);
        var id = Guid.NewGuid();
        var response = await client.PutAsJsonAsync($"/api/treinos/{id}", Plan(catalog[0].Id, catalog[1].Id));
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<TreinoResponseDto>())!;
        Assert.Equal("Peito", created.Nome);
        Assert.Equal("Observação", created.Descricao);
        Assert.Equal(2, created.Exercicios.Count);
        Assert.Equal(new[] { 0, 1 }, created.Exercicios.Select(x => x.Ordem));
        var usedDelete = await client.DeleteAsync($"/api/exercicios/{catalog[0].Id}");
        Assert.Equal(HttpStatusCode.Conflict, usedDelete.StatusCode);

        var edit = Plan(catalog[1].Id, catalog[0].Id) with { Nome = "Pernas", DiaSemana = 5 };
        (await client.PutAsJsonAsync($"/api/treinos/{id}", edit)).EnsureSuccessStatusCode();
        var updated = (await client.GetFromJsonAsync<TreinoResponseDto>($"/api/treinos/{id}"))!;
        Assert.Equal(catalog[1].Id, updated.Exercicios[0].Exercicio.Id);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal(5, updated.DiaSemana);

        (await client.PutAsJsonAsync($"/api/treinos/{id}", Plan(catalog[0].Id))).EnsureSuccessStatusCode();
        var single = (await client.GetFromJsonAsync<TreinoResponseDto>($"/api/treinos/{id}"))!;
        Assert.Single(single.Exercicios);
        Assert.Single((await client.GetFromJsonAsync<List<TreinoResponseDto>>("/api/treinos"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/treinos/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/treinos/{id}")).StatusCode);
        Assert.Equal(12, (await Catalog(client)).Count);
        var options = new DbContextOptionsBuilder<MoveUpDbContext>().UseSqlite($"Data Source={DatabasePath}").Options;
        await using var db = new MoveUpDbContext(options);
        Assert.Equal(0, await db.TreinoExercicios.CountAsync());
    }

    [Fact]
    public async Task DatabaseAndSeedSurviveServerRestart()
    {
        var id = Guid.NewGuid();
        Guid changedExercise;
        using (var factory = Factory())
        using (var client = factory.CreateClient())
        {
            var catalog = await Catalog(client);
            changedExercise = catalog[0].Id;
            (await client.PutAsJsonAsync($"/api/exercicios/{changedExercise}", new ExercicioSaveDto("Personalizado", "Peito", null))).EnsureSuccessStatusCode();
            (await client.PutAsJsonAsync($"/api/treinos/{id}", Plan(changedExercise))).EnsureSuccessStatusCode();
        }
        using (var factory = Factory())
        using (var client = factory.CreateClient())
        {
            var catalog = await Catalog(client);
            Assert.Equal(12, catalog.Count);
            Assert.Equal("Personalizado", catalog.Single(x => x.Id == changedExercise).Nome);
            var plan = await client.GetFromJsonAsync<TreinoResponseDto>($"/api/treinos/{id}");
            Assert.Equal(72.5, plan!.Exercicios[0].CargaInicial);
        }
    }

    [Fact]
    public async Task InvalidPayloadsNeverOverwriteExistingPlan()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        var exercise = (await Catalog(client))[0].Id;
        var id = Guid.NewGuid();
        var valid = Plan(exercise);
        (await client.PutAsJsonAsync($"/api/treinos/{id}", valid)).EnsureSuccessStatusCode();
        var item = valid.Exercicios![0]!;
        TreinoCreateDto[] invalid = [
            valid with { Nome = " " }, valid with { Nome = null },
            valid with { DiaSemana = 0 }, valid with { DiaSemana = 8 },
            valid with { Exercicios = [] }, valid with { Exercicios = null },
            valid with { Exercicios = [null] }, Plan(exercise, exercise), Plan(Guid.NewGuid()),
            valid with { Exercicios = [item with { Series = 0 }] },
            valid with { Exercicios = [item with { Repeticoes = -1 }] },
            valid with { Exercicios = [item with { CargaInicial = -1 }] },
            valid with { Exercicios = [item with { TempoDescanso = -1 }] }
        ];
        foreach (var dto in invalid)
        {
            var response = await client.PutAsJsonAsync($"/api/treinos/{id}", dto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        var unchanged = (await client.GetFromJsonAsync<TreinoResponseDto>($"/api/treinos/{id}"))!;
        Assert.Equal("Peito", unchanged.Nome);
        Assert.Single(unchanged.Exercicios);
        Assert.Null(unchanged.UpdatedAt);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/treinos/{Guid.Empty}", valid)).StatusCode);
        var fractional = new { nome = "Teste", diaSemana = 1, exercicios = new[] { new { exercicioId = exercise, series = 1.5, repeticoes = 10, cargaInicial = 0, tempoDescanso = 0 } } };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/treinos/{id}", fractional)).StatusCode);
    }

    [Fact]
    public async Task ExerciseCrudAndNotFound()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/exercicios", new ExercicioSaveDto(" Máquina X ", " Pernas ", null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var exercise = (await create.Content.ReadFromJsonAsync<ExercicioResponseDto>())!;
        Assert.Equal("Máquina X", exercise.Nome);
        var update = await client.PutAsJsonAsync($"/api/exercicios/{exercise.Id}", new ExercicioSaveDto("Máquina Y", "Pernas", "Ajuste 2"));
        update.EnsureSuccessStatusCode();
        var edited = (await client.GetFromJsonAsync<ExercicioResponseDto>($"/api/exercicios/{exercise.Id}"))!;
        Assert.Equal(exercise.CreatedAt, edited.CreatedAt);
        Assert.Equal("Máquina Y", edited.Nome);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/exercicios", new ExercicioSaveDto(" ", " ", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/exercicios/{exercise.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/exercicios/{exercise.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/exercicios/{exercise.Id}", new ExercicioSaveDto("X", "Y", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/treinos/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task TransactionRollsBackEvenAfterOldItemsWereDeleted()
    {
        Directory.CreateDirectory(directory);
        var options = new DbContextOptionsBuilder<MoveUpDbContext>().UseSqlite($"Data Source={DatabasePath};Foreign Keys=True").Options;
        var id = Guid.NewGuid();
        Guid exercise;
        await using (var db = new MoveUpDbContext(options))
        {
            await db.Database.MigrateAsync();
            exercise = (await db.Exercicios.FirstAsync()).Id;
            await new TreinoRepository(db).SalvarAsync(new Treino { Id = id, Nome = "Original", DiaSemana = 1,
                TreinoExercicios = [new TreinoExercicio { TreinoId = id, ExercicioId = exercise, Series = 3, Repeticoes = 10 }] });
        }
        await using (var db = new MoveUpDbContext(options))
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => new TreinoRepository(db).SalvarAsync(new Treino
            {
                Id = id, Nome = "Não deve persistir", DiaSemana = 2,
                TreinoExercicios = [new TreinoExercicio { TreinoId = id, ExercicioId = Guid.NewGuid(), Series = 3, Repeticoes = 10 }]
            }));
        }
        await using (var db = new MoveUpDbContext(options))
        {
            var original = await new TreinoRepository(db).ObterPorIdAsync(id);
            Assert.Equal("Original", original!.Nome);
            Assert.Equal(exercise, Assert.Single(original.TreinoExercicios).ExercicioId);
        }
    }

    [Fact]
    public async Task PostLocationOpenApiAndCorsWork()
    {
        using var factory = Factory();
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/treinos", Plan((await Catalog(client))[0].Id));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);
        (await client.GetAsync(create.Headers.Location)).EnsureSuccessStatusCode();
        var docs = await client.GetAsync("/openapi/v1.json");
        docs.EnsureSuccessStatusCode();
        Assert.Contains("/api/treinos", await docs.Content.ReadAsStringAsync());
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/treinos");
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "PUT");
        var cors = await client.SendAsync(preflight);
        Assert.Equal("http://localhost:5173", Assert.Single(cors.Headers.GetValues("Access-Control-Allow-Origin")));
    }
}

internal sealed class TestApp(string databasePath) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Path", databasePath);
    }
}
