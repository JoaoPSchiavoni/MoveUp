using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MoveUp.Application.Services;
using MoveUp.Context;
using MoveUp.DTOs.Acompanhamento;
using MoveUp.Entities;
using Xunit;

namespace MoveUp.Tests;

public sealed class ConsistencyTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"moveup-consistency-{Guid.NewGuid()}.db");
    private MoveUpDbContext db = null!;
    private readonly TestClock clock = new();
    private AcompanhamentoService service = null!;
    public async Task InitializeAsync()
    {
        db = new(new DbContextOptionsBuilder<MoveUpDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True").Options);
        await db.Database.MigrateAsync();
        service = new(db, clock);
    }
    public async Task DisposeAsync()
    {
        await db.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(path);
    }
    private async Task<SessaoTreino> AddSession(DateTime start, string status = StatusSessao.Concluida)
    {
        var session = new SessaoTreino { Id = Guid.NewGuid(), NomeTreino = "Treino preservado", Inicio = start,
            Fim = status == StatusSessao.EmAndamento ? null : start.AddMinutes(30), Status = status };
        var exercise = new SessaoExercicio { Nome = "Supino", GrupoMuscular = "Peito", SessaoTreinoId = session.Id };
        exercise.Series.Add(new SerieRealizada { SessaoExercicioId = exercise.Id, RepeticoesPlanejadas = 10, CargaPlanejada = 20,
            Status = StatusSerie.Concluida, Repeticoes = 10, Carga = 20, ConcluidaEm = start.AddMinutes(1) });
        session.Exercicios.Add(exercise);
        await service.AssignDateAsync(session, default);
        db.SessoesTreino.Add(session);
        await db.SaveChangesAsync();
        return session;
    }
    [Fact]
    public async Task OnFireCrossesMonthsWithoutCountingDuplicateSessionsOrRestDays()
    {
        clock.Utc = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        await service.ConfigurarAsync(new("UTC", [1, 3, 5], 3), default);
        foreach (var day in new[] {21, 23, 25, 28, 30})
        {
            clock.Utc = new(2026, 9, day, 12, 0, 0, TimeSpan.Zero);
            await AddSession(clock.Utc.UtcDateTime);
        }
        await AddSession(clock.Utc.UtcDateTime.AddMinutes(40));
        Assert.Equal(5, (await service.PainelAsync(default)).SequenciaAtual);
        Assert.Equal(5, (await service.CalendarioAsync(2026, 9, default)).Dias.Count(d => d.OnFire));
        clock.Utc = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(5, (await service.PainelAsync(default)).SequenciaAtual);
        clock.Utc = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(0, (await service.PainelAsync(default)).SequenciaAtual);
        Assert.Equal(5, (await service.CalendarioAsync(2026, 9, default)).Dias.Count(d => d.OnFire));
    }
    [Fact]
    public async Task ActivationBackfillsHistoryOnceAndCountsDaysInsteadOfSessions()
    {
        var before = new DateTime(2026, 10, 3, 1, 0, 0, DateTimeKind.Utc);
        var a = await AddSession(before);
        await AddSession(before.AddMinutes(45));
        await service.ConfigurarAsync(new("America/Sao_Paulo", [1, 3, 5], 3), default);
        var calendar = await service.CalendarioAsync(2026, 10, default);
        var trained = calendar.Dias.Single(d => d.Data == new DateOnly(2026, 10, 2));
        Assert.Equal("treinado", trained.Estado);
        Assert.Equal(2, trained.Sessoes.Count);
        Assert.Equal("semPlanejamento", calendar.Dias.Single(d => d.Data == new DateOnly(2026, 10, 1)).Estado);
        Assert.Equal(1, (await service.PainelAsync(default)).DiasTreinadosMes);
        await service.ConfigurarAsync(new("UTC", [1, 3, 5], 3), default);
        Assert.Equal(new DateOnly(2026, 10, 2), a.DataPresenca);
        Assert.Equal("America/Sao_Paulo", a.FusoPresenca);
        var future = await AddSession(before.AddDays(4));
        Assert.Equal("UTC", future.FusoPresenca);
    }
    [Fact]
    public async Task RestExtraDaysAndOpenTodayDoNotBreakOrInflateStreak()
    {
        await service.ConfigurarAsync(new("UTC", [1, 3, 5], 3), default);
        await AddSession(clock.Utc.UtcDateTime);
        await AddSession(clock.Utc.UtcDateTime.AddHours(1));
        clock.Utc = clock.Utc.AddDays(2); // Wednesday is still open.
        Assert.Equal(1, (await service.PainelAsync(default)).SequenciaAtual);
        clock.Utc = clock.Utc.AddDays(1);
        await AddSession(clock.Utc.UtcDateTime); // Thursday extra.
        Assert.Equal(0, (await service.PainelAsync(default)).SequenciaAtual);
        clock.Utc = clock.Utc.AddDays(1);
        await AddSession(clock.Utc.UtcDateTime);
        var panel = await service.PainelAsync(default);
        Assert.Equal(1, panel.SequenciaAtual);
        Assert.Equal(1, panel.MelhorSequencia);
        Assert.Equal(3, panel.Semana.DiasTreinados);
        Assert.Equal(4, panel.Semana.Sessoes);
        Assert.Equal(.5, panel.Semana.Adesao);
        Assert.Equal(800, panel.Semana.Volume);
        Assert.Equal(7200, panel.Semana.DuracaoSegundos);
        var calendar = await service.CalendarioAsync(2026, 10, default);
        Assert.Equal("descanso", calendar.Dias.Single(d => d.Data.Day == 6).Estado);
        Assert.Equal("falta", calendar.Dias.Single(d => d.Data.Day == 7).Estado);
        Assert.Equal("planejadoFuturo", calendar.Dias.Single(d => d.Data.Day == 12).Estado);
    }
    [Fact]
    public async Task RevisionsAreRepeatableAndPreservePastRoutineAndWeeklyGoal()
    {
        await service.ConfigurarAsync(new("UTC", [1, 3, 5], 3), default);
        await service.ConfigurarAsync(new("UTC", [2, 4], 2), default);
        await service.ConfigurarAsync(new("UTC", [2, 4], 2), default);
        var updated = await service.ConfigurarAsync(new("UTC", [2, 6], 5), default);
        Assert.Equal(2, updated.Rotinas.Count);
        Assert.Equal(2, updated.Metas.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), updated.Rotinas.Last().Inicio);
        Assert.Equal(new DateOnly(2026, 10, 12), updated.Metas.Last().Inicio);
        Assert.Equal(3, (await service.PainelAsync(default)).Semana.Meta);
        clock.Utc = clock.Utc.AddDays(7);
        Assert.Equal(5, (await service.PainelAsync(default)).Semana.Meta);
        Assert.Equal(3, (await service.SemanaAsync(new(2026, 10, 5), default)).Meta);
        Assert.Equal(2, (await service.ConfiguracaoAsync(default)).Rotinas.Count);
        var calendar = await service.CalendarioAsync(2026, 10, default);
        Assert.True(calendar.Dias.Single(d => d.Data.Day == 5).Planejado);
        Assert.False(calendar.Dias.Single(d => d.Data.Day == 7).Planejado);
    }
    [Fact]
    public async Task MovingTimezoneBackAcrossMondayPreservesScheduledRevisionDates()
    {
        clock.Utc = new(2026, 10, 5, 1, 0, 0, TimeSpan.Zero);
        await service.ConfigurarAsync(new("UTC", [1], 1), default);
        await service.ConfigurarAsync(new("UTC", [2], 2), default);
        var config = await service.ConfigurarAsync(new("America/Sao_Paulo", [2], 2), default);
        Assert.Equal(new DateOnly(2026, 10, 4), config.Hoje);
        Assert.Equal(2, config.Rotinas.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), config.Rotinas.Last().Inicio);
        Assert.Equal(new DateOnly(2026, 10, 12), config.Metas.Last().Inicio);
        Assert.Equal(new[] { 1 }, config.Rotinas.First().DiasSemana);
    }
    [Fact]
    public async Task RestartKeepsPreferencesRevisionsAndPresenceAssignedAtSessionStart()
    {
        await service.ConfigurarAsync(new("UTC", [1], 1), default);
        var session = await AddSession(new DateTime(2026, 10, 5, 1, 0, 0, DateTimeKind.Utc), StatusSessao.EmAndamento);
        await service.ConfigurarAsync(new("America/Sao_Paulo", [1, 3], 2), default);
        session.Status = StatusSessao.Concluida;
        session.Fim = session.Inicio.AddMinutes(30);
        await db.SaveChangesAsync();
        await db.DisposeAsync();
        db = new(new DbContextOptionsBuilder<MoveUpDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True").Options);
        service = new(db, clock);
        var config = await service.ConfiguracaoAsync(default);
        Assert.Equal("America/Sao_Paulo", config.Fuso);
        Assert.Equal(2, config.Rotinas.Count);
        Assert.Equal(2, config.Metas.Count);
        Assert.Equal(new DateOnly(2026, 10, 5), (await db.SessoesTreino.SingleAsync()).DataPresenca);
        Assert.Equal("UTC", (await db.SessoesTreino.SingleAsync()).FusoPresenca);
        Assert.Equal(1, (await service.PainelAsync(default)).Semana.DiasTreinados);
    }
    [Fact]
    public async Task InvalidConfigurationAndNoPlannedDaysHaveNoFabricatedAdherence()
    {
        foreach (var dto in new[] { new ConfigurarAcompanhamentoDto("Invalid/Zone", [1], 3),
            new("UTC", [1, 1], 3), new("UTC", [8], 3), new("UTC", [1], 0) })
            Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.ConfigurarAsync(dto, default))).Status);
        await service.ConfigurarAsync(new("UTC", [], 1), default);
        await AddSession(clock.Utc.UtcDateTime, StatusSessao.Cancelada);
        await AddSession(clock.Utc.UtcDateTime, StatusSessao.EmAndamento);
        var panel = await service.PainelAsync(default);
        Assert.Null(panel.Semana.Adesao);
        Assert.Equal(0, panel.Semana.DiasTreinados);
        Assert.Equal(0, panel.SequenciaAtual);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.CalendarioAsync(2101, 13, default))).Status);
        Assert.Equal(400, (await Assert.ThrowsAsync<ApiException>(() => service.SemanaAsync(new(2026, 10, 6), default))).Status);
    }
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Utc = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Utc;
    }
}
