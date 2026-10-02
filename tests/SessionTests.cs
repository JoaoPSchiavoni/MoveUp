using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MoveUp.Context;
using MoveUp.DTOs.Exercicio;
using MoveUp.DTOs.Sessao;
using MoveUp.DTOs.Treino;
using MoveUp.Entities;
using Xunit;

namespace MoveUp.Tests;

public class SessionTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "moveup-sessions-" + Guid.NewGuid());
    private string DatabasePath => Path.Combine(directory, "sessions.db");
    private TestApp Factory() => new(DatabasePath);
    private DbContextOptions<MoveUpDbContext> Options => new DbContextOptionsBuilder<MoveUpDbContext>()
        .UseSqlite($"Data Source={DatabasePath};Foreign Keys=True").Options;
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    private static async Task<TreinoResponseDto> Workout(HttpClient client)
    {
        var catalog = (await client.GetFromJsonAsync<List<ExercicioResponseDto>>("/api/exercicios"))!;
        var response = await client.PostAsJsonAsync("/api/treinos", new TreinoCreateDto("Treino A", 1, null,
            [new(catalog[0].Id, 2, 10, 70, 90), new(catalog[1].Id, 1, 12, 20, 60)]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TreinoResponseDto>())!;
    }
    private static async Task<SessaoResponseDto> Start(HttpClient client, Guid workoutId, Guid? id = null)
    {
        var response = await client.PutAsJsonAsync($"/api/sessoes/{id ?? Guid.NewGuid()}", new IniciarSessaoDto(workoutId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessaoResponseDto>())!;
    }
    private static async Task<SessaoResponseDto> Record(HttpClient client, SessaoResponseDto session,
        string status = "concluida", int? reps = 8, double? weight = 72.5)
    {
        var response = await client.PutAsJsonAsync($"/api/sessoes/{session.Id}/series/{session.Exercicios[0].Series[0].Id}",
            new RegistrarSerieDto(status, reps, weight));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessaoResponseDto>())!;
    }
    private static async Task<SessaoResponseDto> Finish(HttpClient client, Guid id, string? note = " Bom treino ")
    {
        var response = await client.PutAsJsonAsync($"/api/sessoes/{id}/conclusao", new ConcluirSessaoDto(note));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessaoResponseDto>())!;
    }
    private static Task<HttpResponseMessage> Cancel(HttpClient client, Guid id) =>
        client.PutAsJsonAsync($"/api/sessoes/{id}/cancelamento", new { });

    [Fact]
    public async Task CompleteFlowMatchesFrontendContractAndIdempotentRetries()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/sessoes/ativa")).StatusCode);
        var workout = await Workout(client);
        var session = await Start(client, workout.Id);
        Assert.Equal("emAndamento", session.Status);
        Assert.Equal(new[] { 0, 1 }, session.Exercicios.Select(e => e.Ordem));
        Assert.Equal(3, session.Exercicios.Sum(e => e.Series.Count));
        Assert.All(session.Exercicios.SelectMany(e => e.Series), s => { Assert.Equal("pendente", s.Status); Assert.Null(s.Carga); });
        var repeated = await Start(client, workout.Id, session.Id);
        Assert.Equal(session.Inicio, repeated.Inicio);
        Assert.Equal(session.Exercicios[0].Series[0].Id, repeated.Exercicios[0].Series[0].Id);
        var conflict = await client.PutAsJsonAsync($"/api/sessoes/{Guid.NewGuid()}", new IniciarSessaoDto(workout.Id));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var recorded = await Record(client, session);
        Assert.Equal(580, recorded.Resumo.VolumeRegistrado);
        var repeatedRecord = await Record(client, session);
        Assert.Equal(recorded.Exercicios[0].Series[0].ConcluidaEm, repeatedRecord.Exercicios[0].Series[0].ConcluidaEm);
        var finished = await Finish(client, session.Id);
        Assert.Equal("concluida", finished.Status); Assert.NotNull(finished.Fim);
        Assert.Equal("Bom treino", finished.Observacao);
        Assert.Equal(2, finished.Resumo.SeriesPuladas); Assert.Equal(1, finished.Resumo.SeriesConcluidas);
        Assert.Equal(1, finished.Resumo.ExerciciosRealizados); Assert.Equal(580, finished.Resumo.VolumeRegistrado);
        Assert.Equal(finished.Fim, (await Finish(client, session.Id, "Não sobrescrever")).Fim);
        Assert.Equal("Bom treino", (await Finish(client, session.Id, "Não sobrescrever")).Observacao);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/sessoes/ativa")).StatusCode);
        var persisted = (await client.GetFromJsonAsync<SessaoResponseDto>($"/api/sessoes/{session.Id}"))!;
        Assert.Equal(DateTimeKind.Utc, persisted.Inicio.Kind); Assert.Equal(DateTimeKind.Utc, persisted.Fim!.Value.Kind);
        var history = (await client.GetFromJsonAsync<HistoricoSessaoDto>("/api/sessoes"))!;
        Assert.Single(history.Items); Assert.False(history.HasMore);
        Assert.Equal(HttpStatusCode.Conflict, (await Cancel(client, session.Id)).StatusCode);
        var editEnded = await client.PutAsJsonAsync($"/api/sessoes/{session.Id}/series/{session.Exercicios[0].Series[0].Id}", new RegistrarSerieDto("pendente", null, null));
        Assert.Equal(HttpStatusCode.Conflict, editEnded.StatusCode);
    }

    [Fact]
    public async Task ValidateNumbersStatesMembershipAndCancellation()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = await Workout(client); var session = await Start(client, workout.Id);
        var seriesUrl = $"/api/sessoes/{session.Id}/series/{session.Exercicios[0].Series[0].Id}";
        RegistrarSerieDto[] invalid = [new(null, 8, 70), new("invalido", 8, 70), new("concluida", 0, 70),
            new("concluida", -1, 70), new("concluida", null, 70), new("concluida", 8, null),
            new("concluida", 8, -1), new("pulada", 8, 70), new("pendente", 8, 70), new("concluida", 8, double.MaxValue)];
        foreach (var dto in invalid) Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(seriesUrl, dto)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(seriesUrl, new { status = "concluida", repeticoes = 1.5, carga = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(seriesUrl, new { status = "concluida", repeticoes = 8, carga = "NaN" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/sessoes/{session.Id}/conclusao", new ConcluirSessaoDto(null))).StatusCode);
        var recorded = await Record(client, session, weight: 0);
        Assert.Equal(1, recorded.Resumo.SeriesConcluidas); Assert.Equal(0, recorded.Resumo.VolumeRegistrado);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/sessoes/{session.Id}/conclusao", new ConcluirSessaoDto(new string('x', 2001)))).StatusCode);
        var reopened = await Record(client, session, "pendente", null, null);
        Assert.Null(reopened.Exercicios[0].Series[0].ConcluidaEm);
        Assert.Equal(70, reopened.Exercicios[0].Series[0].CargaPlanejada);
        var skipped = await Record(client, session, "pulada", null, null); Assert.Equal(1, skipped.Resumo.SeriesPuladas);
        var canceled = await Cancel(client, session.Id); canceled.EnsureSuccessStatusCode();
        var originalEnd = (await canceled.Content.ReadFromJsonAsync<SessaoResponseDto>())!.Fim;
        Assert.Equal(originalEnd, (await (await Cancel(client, session.Id)).Content.ReadFromJsonAsync<SessaoResponseDto>())!.Fim);
        var next = await Start(client, workout.Id);
        var foreign = await client.PutAsJsonAsync($"/api/sessoes/{next.Id}/series/{session.Exercicios[0].Series[0].Id}", new RegistrarSerieDto("concluida", 8, 70));
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<HistoricoSessaoDto>("/api/sessoes"))!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/sessoes/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task SnapshotsSurviveWorkoutAndCatalogChangesAndDeletion()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = await Workout(client); var session = await Start(client, workout.Id);
        var exercise = workout.Exercicios[0].Exercicio;
        (await client.PutAsJsonAsync($"/api/exercicios/{exercise.Id}", new ExercicioSaveDto("Renomeado", "Outro", null))).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/treinos/{workout.Id}", new TreinoCreateDto("Ficha editada", 5, null,
            [new(workout.Exercicios[1].Exercicio.Id, 1, 1, 1, 0)]))).EnsureSuccessStatusCode();
        var snapshot = (await client.GetFromJsonAsync<SessaoResponseDto>($"/api/sessoes/{session.Id}"))!;
        Assert.Equal("Treino A", snapshot.NomeTreino); Assert.Equal(exercise.Nome, snapshot.Exercicios[0].Nome);
        Assert.Equal(70, snapshot.Exercicios[0].Series[0].CargaPlanejada);
        (await client.DeleteAsync($"/api/treinos/{workout.Id}")).EnsureSuccessStatusCode();
        (await client.DeleteAsync($"/api/exercicios/{exercise.Id}")).EnsureSuccessStatusCode();
        var afterDelete = (await client.GetFromJsonAsync<SessaoResponseDto>($"/api/sessoes/{session.Id}"))!;
        Assert.Null(afterDelete.TreinoId); Assert.Equal(2, afterDelete.Exercicios.Count);
        Assert.Equal(exercise.Id, afterDelete.Exercicios[0].ExercicioOrigemId);
        Assert.Equal(workout.Id, afterDelete.TreinoOrigemId);
        Assert.Equal(session.Id, (await Start(client, workout.Id, session.Id)).Id);
        await Record(client, session); await Finish(client, session.Id);
        Assert.Equal(exercise.Nome, (await client.GetFromJsonAsync<SessaoResponseDto>($"/api/sessoes/{session.Id}"))!.Exercicios[0].Nome);
    }

    [Fact]
    public async Task ActiveSessionPersistsAcrossServerRestart()
    {
        Guid id;
        using (var factory = Factory()) using (var client = factory.CreateClient())
        {
            var session = await Start(client, (await Workout(client)).Id); id = session.Id;
            await Record(client, session);
        }
        using (var factory = Factory()) using (var client = factory.CreateClient())
        {
            var active = (await client.GetFromJsonAsync<SessaoResponseDto>("/api/sessoes/ativa"))!;
            Assert.Equal(id, active.Id); Assert.Equal(580, active.Resumo.VolumeRegistrado);
            await Finish(client, id);
        }
        using (var factory = Factory()) using (var client = factory.CreateClient())
        {
            Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/sessoes/ativa")).StatusCode);
            Assert.Equal(id, Assert.Single((await client.GetFromJsonAsync<HistoricoSessaoDto>("/api/sessoes"))!.Items).Id);
        }
    }

    [Fact]
    public async Task ConcurrentStartsAndFinishCannotDuplicateOrReopenSessions()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = await Workout(client); var id = Guid.NewGuid();
        var same = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.PutAsJsonAsync($"/api/sessoes/{id}", new IniciarSessaoDto(workout.Id))));
        Assert.All(same, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        var sessions = await Task.WhenAll(same.Select(r => r.Content.ReadFromJsonAsync<SessaoResponseDto>()));
        Assert.Single(sessions.Select(s => s!.Exercicios[0].Series[0].Id).Distinct());
        await Cancel(client, id);
        var different = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.PutAsJsonAsync($"/api/sessoes/{Guid.NewGuid()}", new IniciarSessaoDto(workout.Id))));
        Assert.Single(different, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(2, different.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var active = (await client.GetFromJsonAsync<SessaoResponseDto>("/api/sessoes/ativa"))!;
        await Record(client, active);
        var finishTask = client.PutAsJsonAsync($"/api/sessoes/{active.Id}/conclusao", new ConcluirSessaoDto(null));
        var writeTask = client.PutAsJsonAsync($"/api/sessoes/{active.Id}/series/{active.Exercicios[0].Series[1].Id}", new RegistrarSerieDto("concluida", 10, 1));
        var responses = await Task.WhenAll(finishTask, writeTask);
        Assert.Equal(HttpStatusCode.OK, responses[0].StatusCode);
        Assert.Contains(responses[1].StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
        var ended = (await client.GetFromJsonAsync<SessaoResponseDto>($"/api/sessoes/{active.Id}"))!;
        Assert.Equal("concluida", ended.Status);
        Assert.DoesNotContain(ended.Exercicios.SelectMany(e => e.Series), s => s.Status == "pendente");
        Assert.Equal(responses[1].StatusCode == HttpStatusCode.OK ? 590 : 580, ended.Resumo.VolumeRegistrado);
    }

    [Fact]
    public async Task HistoryIsPaginatedWithStableOrderingAndValidatedBounds()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = await Workout(client);
        for (var i = 0; i < 3; i++) { var s = await Start(client, workout.Id); await Record(client, s); await Finish(client, s.Id); }
        await using (var db = new MoveUpDbContext(Options))
        {
            var fixedTime = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            await db.SessoesTreino.ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Inicio, fixedTime));
        }
        var first = (await client.GetFromJsonAsync<HistoricoSessaoDto>("/api/sessoes?pagina=1&tamanhoPagina=2&status=concluida"))!;
        var second = (await client.GetFromJsonAsync<HistoricoSessaoDto>("/api/sessoes?pagina=2&tamanhoPagina=2&status=concluida"))!;
        Assert.True(first.HasMore); Assert.False(second.HasMore);
        Assert.Equal(3, first.Items.Concat(second.Items).Select(s => s.Id).Distinct().Count());
        Assert.Equal(first.Items.Select(s => s.Id), (await client.GetFromJsonAsync<HistoricoSessaoDto>("/api/sessoes?pagina=1&tamanhoPagina=2"))!.Items.Select(s => s.Id));
        foreach (var query in new[] { "pagina=0", "tamanhoPagina=0", "tamanhoPagina=101", "pagina=2147483647", "status=cancelada" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/sessoes?" + query)).StatusCode);
    }

    [Fact]
    public async Task FailedSnapshotCreationRollsBackEntireSessionAndCanBeRetried()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = await Workout(client); var id = Guid.NewGuid();
        await using (var db = new MoveUpDbContext(Options))
        {
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectSecondSeries BEFORE INSERT ON SeriesRealizadas WHEN NEW.Ordem = 1 BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
        }
        var failure = await client.PutAsJsonAsync($"/api/sessoes/{id}", new IniciarSessaoDto(workout.Id));
        Assert.Equal(HttpStatusCode.Conflict, failure.StatusCode);
        await using (var db = new MoveUpDbContext(Options))
        {
            Assert.Equal(0, await db.SessoesTreino.CountAsync());
            Assert.Equal(0, await db.SessaoExercicios.CountAsync());
            Assert.Equal(0, await db.SeriesRealizadas.CountAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectSecondSeries;");
        }
        Assert.Equal(id, (await Start(client, workout.Id, id)).Id);
    }

    [Fact]
    public async Task StartRejectsInvalidIdsMissingInactiveWorkoutsAndReusedIds()
    {
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = await Workout(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/sessoes/{Guid.Empty}", new IniciarSessaoDto(workout.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/sessoes/{Guid.NewGuid()}", new IniciarSessaoDto(Guid.Empty))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/sessoes/{Guid.NewGuid()}", new IniciarSessaoDto(Guid.NewGuid()))).StatusCode);
        await using (var db = new MoveUpDbContext(Options))
            await db.Treinos.Where(w => w.Id == workout.Id).ExecuteUpdateAsync(setters => setters.SetProperty(w => w.Ativo, false));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/sessoes/{Guid.NewGuid()}", new IniciarSessaoDto(workout.Id))).StatusCode);
        await using (var db = new MoveUpDbContext(Options))
            await db.Treinos.Where(w => w.Id == workout.Id).ExecuteUpdateAsync(setters => setters.SetProperty(w => w.Ativo, true));
        var session = await Start(client, workout.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/sessoes/{session.Id}", new IniciarSessaoDto(Guid.NewGuid()))).StatusCode);
    }

    [Fact]
    public async Task PhaseThreeHistoryMigratesWithoutInventingExerciseOrigin()
    {
        Directory.CreateDirectory(directory);
        var sessionId = Guid.NewGuid(); var exerciseId = Guid.NewGuid(); var setId = Guid.NewGuid();
        var start = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        await using (var db = new MoveUpDbContext(Options))
        {
            await db.GetService<IMigrator>().MigrateAsync("20261002135539_AddConsistency");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SessoesTreino (Id, TreinoOrigemId, NomeTreino, Inicio, Fim, Status, DataPresenca, FusoPresenca) VALUES ({sessionId}, {Guid.Empty}, {"Legado"}, {start}, {start.AddMinutes(30)}, {"concluida"}, {new DateOnly(2026, 10, 1)}, {"UTC"})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SessaoExercicios (Id, SessaoTreinoId, Nome, GrupoMuscular, Ordem, TempoDescanso) VALUES ({exerciseId}, {sessionId}, {"Supino"}, {"Peito"}, {0}, {60})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SeriesRealizadas (Id, SessaoExercicioId, Ordem, RepeticoesPlanejadas, CargaPlanejada, Repeticoes, Carga, Status, ConcluidaEm) VALUES ({setId}, {exerciseId}, {0}, {10}, {20d}, {8}, {25d}, {"concluida"}, {start.AddMinutes(1)})");
        }
        using var factory = Factory(); using var client = factory.CreateClient();
        var session = (await client.GetFromJsonAsync<SessaoResponseDto>($"/api/sessoes/{sessionId}"))!;
        Assert.Equal(200, session.Resumo.VolumeRegistrado);
        Assert.Null(session.Exercicios.Single().ExercicioOrigemId);
        Assert.Equal(new DateOnly(2026, 10, 1), session.DataPresenca);
        Assert.Equal("UTC", session.FusoPresenca);
    }

    [Fact]
    public async Task PhaseOneDatabaseMigratesWithoutLosingWorkoutsOrCatalog()
    {
        Directory.CreateDirectory(directory);
        var workoutId = Guid.NewGuid();
        await using (var db = new MoveUpDbContext(Options))
        {
            await db.GetService<IMigrator>().MigrateAsync("20261001185558_InitialCreate");
            var exercise = await db.Exercicios.FirstAsync();
            db.Treinos.Add(new Treino { Id = workoutId, Nome = "Ficha existente", DiaSemana = 2,
                TreinoExercicios = [new TreinoExercicio { ExercicioId = exercise.Id, Series = 2, Repeticoes = 10, CargaInicial = 20 }] });
            await db.SaveChangesAsync();
        }
        using var factory = Factory(); using var client = factory.CreateClient();
        var workout = (await client.GetFromJsonAsync<TreinoResponseDto>($"/api/treinos/{workoutId}"))!;
        Assert.Equal("Ficha existente", workout.Nome);
        Assert.Equal(12, (await client.GetFromJsonAsync<List<ExercicioResponseDto>>("/api/exercicios"))!.Count);
        Assert.Equal(2, (await Start(client, workoutId)).Exercicios[0].Series.Count);
    }
}
