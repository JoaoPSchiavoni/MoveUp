using System.Data;
using Microsoft.EntityFrameworkCore;
using MoveUp.Context;
using MoveUp.DTOs.Acompanhamento;
using MoveUp.Entities;

namespace MoveUp.Application.Services;

public class AcompanhamentoService(MoveUpDbContext db, TimeProvider clock)
{
    private static TimeZoneInfo Zone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || (id != "UTC" && !id.Contains('/')))
            throw new ApiException(400, "Informe um fuso IANA válido, por exemplo America/Sao_Paulo ou UTC.");
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        { throw new ApiException(400, "Fuso horário não encontrado."); }
    }
    private DateOnly Today(string zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone(zone)).DateTime);
    public static DateOnly LocalDate(DateTime utc, string zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone(zone)));
    private Task<AcompanhamentoConfig?> Config(CancellationToken ct) => db.AcompanhamentoConfigs.SingleOrDefaultAsync(ct);
    private static DateOnly Monday(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
    private static bool Planned(DateOnly day, IReadOnlyList<RotinaRevisao> routines) =>
        routines.LastOrDefault(r => r.Inicio <= day)?.DiasSemana.Contains(((int)day.DayOfWeek + 6) % 7 + 1) == true;
    private static IEnumerable<DateOnly> Dates(DateOnly start, DateOnly end)
    {
        for (var day = start; day <= end; day = day.AddDays(1)) yield return day;
    }
    public async Task<ConfiguracaoDto> ConfiguracaoAsync(CancellationToken ct)
    {
        var config = await Config(ct);
        var zone = config?.Fuso ?? "America/Sao_Paulo";
        var routines = await db.RotinaRevisoes.AsNoTracking().OrderBy(r => r.Inicio).ToListAsync(ct);
        var goals = await db.MetaRevisoes.AsNoTracking().OrderBy(r => r.Inicio).ToListAsync(ct);
        return new(config != null, zone, Today(zone), config?.Inicio,
            routines.Select(r => new RotinaDto(r.Inicio, r.DiasSemana)).ToArray(),
            goals.Select(g => new MetaDto(g.Inicio, g.Dias)).ToArray());
    }
    public async Task<ConfiguracaoDto> ConfigurarAsync(ConfigurarAcompanhamentoDto dto, CancellationToken ct)
    {
        Zone(dto.Fuso);
        if (dto.DiasSemana == null || dto.DiasSemana.Any(d => d is < 1 or > 7) || dto.DiasSemana.Distinct().Count() != dto.DiasSemana.Length)
            throw new ApiException(400, "Dias da semana devem ser únicos, entre 1 e 7.");
        if (dto.MetaSemanal is < 1 or > 7) throw new ApiException(400, "A meta semanal deve ser entre 1 e 7 dias.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var config = await Config(ct);
        var today = Today(dto.Fuso);
        var first = config == null;
        if (first)
        {
            config = new AcompanhamentoConfig { Fuso = dto.Fuso, Inicio = today };
            db.AcompanhamentoConfigs.Add(config);
            // Assign the first chosen zone once, including sessions already in progress.
            foreach (var session in await db.SessoesTreino.Where(s => s.DataPresenca == null).ToListAsync(ct))
            {
                session.DataPresenca = LocalDate(session.Inicio, dto.Fuso);
                session.FusoPresenca = dto.Fuso;
            }
        }
        config!.Fuso = dto.Fuso;
        var routines = await db.RotinaRevisoes.OrderBy(r => r.Inicio).ToListAsync(ct);
        var days = string.Join(',', dto.DiasSemana.Order());
        var effective = first ? today : today.AddDays(1);
        var current = routines.LastOrDefault(r => r.Inicio <= today) ?? routines.FirstOrDefault();
        // Keep one pending revision; repeating a request never adds another revision.
        // A zone change can move 'today' backwards. Preserve the initial snapshot
        // and never bring an already scheduled revision forward to an earlier date.
        var pendingRoutine = routines.Skip(1).LastOrDefault(r => r.Inicio > today);
        if (first || current?.Dias != days)
        {
            if (pendingRoutine == null) db.RotinaRevisoes.Add(new RotinaRevisao { Inicio = effective, Dias = days });
            else { pendingRoutine.Inicio = pendingRoutine.Inicio > effective ? pendingRoutine.Inicio : effective; pendingRoutine.Dias = days; }
        }
        else if (pendingRoutine != null) db.RotinaRevisoes.Remove(pendingRoutine);
        var goals = await db.MetaRevisoes.OrderBy(g => g.Inicio).ToListAsync(ct);
        var week = Monday(today);
        var currentGoal = goals.LastOrDefault(g => g.Inicio <= week) ?? goals.FirstOrDefault();
        var pendingGoal = goals.Skip(1).LastOrDefault(g => g.Inicio > week);
        if (first || currentGoal?.Dias != dto.MetaSemanal)
        {
            if (pendingGoal == null) db.MetaRevisoes.Add(new MetaRevisao { Inicio = first ? week : week.AddDays(7), Dias = dto.MetaSemanal });
            else { pendingGoal.Inicio = pendingGoal.Inicio > week.AddDays(7) ? pendingGoal.Inicio : week.AddDays(7); pendingGoal.Dias = dto.MetaSemanal; }
        }
        else if (pendingGoal != null) db.MetaRevisoes.Remove(pendingGoal);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await ConfiguracaoAsync(ct);
    }

    public async Task AssignDateAsync(SessaoTreino session, CancellationToken ct)
    {
        var config = await Config(ct);
        if (config == null || session.DataPresenca != null) return;
        session.DataPresenca = LocalDate(session.Inicio, config.Fuso);
        session.FusoPresenca = config.Fuso;
    }
    private IQueryable<SessaoTreino> Completed => db.SessoesTreino.AsNoTracking()
        .Where(s => s.Status == StatusSessao.Concluida && s.DataPresenca != null &&
            s.Exercicios.Any(e => e.Series.Any(x => x.Status == StatusSerie.Concluida)));
    private Task<List<DateOnly>> Presence(DateOnly start, DateOnly end, CancellationToken ct) =>
        Completed.Where(s => s.DataPresenca >= start && s.DataPresenca <= end)
            .Select(s => s.DataPresenca!.Value).Distinct().ToListAsync(ct);
    public async Task<CalendarioDto> CalendarioAsync(int year, int month, CancellationToken ct)
    {
        if (year is < 1900 or > 2100 || month is < 1 or > 12)
            throw new ApiException(400, "Informe ano entre 1900 e 2100 e mês entre 1 e 12.");
        var config = await Config(ct);
        var zone = config?.Fuso ?? "America/Sao_Paulo";
        var today = Today(zone);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var routines = await db.RotinaRevisoes.AsNoTracking().Where(r => r.Inicio <= end).OrderBy(r => r.Inicio).ToListAsync(ct);
        var sessions = await Completed.Where(s => s.DataPresenca >= start && s.DataPresenca <= end)
            .OrderBy(s => s.Inicio).Select(s => new { s.Id, s.NomeTreino, s.DataPresenca }).ToListAsync(ct);
        return new(today, zone, Dates(start, end).Select(day =>
        {
            var items = sessions.Where(s => s.DataPresenca == day).Select(s => new SessaoDiaDto(s.Id, s.NomeTreino)).ToArray();
            var planned = Planned(day, routines);
            var state = items.Length > 0 ? "treinado" : config == null || day < config.Inicio ? "semPlanejamento"
                : !planned ? "descanso" : day < today ? "falta" : day == today ? "planejadoHoje" : "planejadoFuturo";
            return new DiaDto(day, state, planned, items);
        }).ToArray());
    }
    public async Task<SemanaDto> SemanaAsync(DateOnly start, CancellationToken ct)
    {
        if (start.Year is < 1900 or > 2100 || start.DayOfWeek != DayOfWeek.Monday)
            throw new ApiException(400, "Informe a data de uma segunda-feira entre 1900 e 2100.");
        var config = await Config(ct);
        var today = Today(config?.Fuso ?? "America/Sao_Paulo");
        var end = start.AddDays(6);
        var routines = await db.RotinaRevisoes.AsNoTracking().Where(r => r.Inicio <= end).OrderBy(r => r.Inicio).ToListAsync(ct);
        var goal = await db.MetaRevisoes.AsNoTracking().Where(g => g.Inicio <= start).OrderByDescending(g => g.Inicio).FirstOrDefaultAsync(ct);
        var sessions = await Completed.Where(s => s.DataPresenca >= start && s.DataPresenca <= end)
            .Select(s => new { s.DataPresenca, s.Inicio, s.Fim,
                Volume = s.Exercicios.SelectMany(e => e.Series).Where(x => x.Status == StatusSerie.Concluida)
                    .Sum(x => x.Carga!.Value * x.Repeticoes!.Value) }).ToListAsync(ct);
        var presence = sessions.Select(s => s.DataPresenca!.Value).ToHashSet();
        var closed = Dates(start, end).Where(day => day < today && Planned(day, routines)).ToArray();
        var fulfilled = closed.Count(presence.Contains);
        return new(start, end, presence.Count, sessions.Count, goal?.Dias, closed.Length, fulfilled,
            closed.Length == 0 ? null : (double)fulfilled / closed.Length,
            sessions.Sum(s => Math.Max(0, (s.Fim!.Value - s.Inicio).TotalSeconds)), sessions.Sum(s => s.Volume));
    }
    public async Task<PainelDto> PainelAsync(CancellationToken ct)
    {
        var config = await Config(ct);
        var zone = config?.Fuso ?? "America/Sao_Paulo";
        var today = Today(zone);
        var routines = await db.RotinaRevisoes.AsNoTracking().OrderBy(r => r.Inicio).ToListAsync(ct);
        var presence = (await Presence(config?.Inicio ?? today, today, ct)).ToHashSet();
        var current = 0;
        var best = 0;
        if (config != null)
            foreach (var day in Dates(config.Inicio, today))
            {
                if (!Planned(day, routines)) continue;
                if (presence.Contains(day)) { current++; best = Math.Max(best, current); }
                else if (day < today) current = 0;
            }
        var month = new DateOnly(today.Year, today.Month, 1);
        var monthPresence = await Presence(month, month.AddMonths(1).AddDays(-1), ct);
        return new(today, zone, config != null, current, best, monthPresence.Count, await SemanaAsync(Monday(today), ct));
    }
}
