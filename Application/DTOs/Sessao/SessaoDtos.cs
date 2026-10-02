using MoveUp.Entities;

namespace MoveUp.DTOs.Sessao;

public sealed record IniciarSessaoDto(Guid TreinoId);
public sealed record RegistrarSerieDto(string? Status, int? Repeticoes, double? Carga);
public sealed record ConcluirSessaoDto(string? Observacao);
public sealed record HistoricoSessaoDto(IReadOnlyList<SessaoResponseDto> Items, bool HasMore);
public sealed record ResumoSessaoDto(int DuracaoSegundos, int ExerciciosRealizados,
    int SeriesConcluidas, int SeriesPuladas, double VolumeRegistrado);
public sealed record SerieResponseDto(Guid Id, int Ordem, int RepeticoesPlanejadas,
    double CargaPlanejada, int? Repeticoes, double? Carga, string Status, DateTime? ConcluidaEm);
public sealed record SessaoExercicioResponseDto(Guid Id, string Nome, string GrupoMuscular,
    int Ordem, int TempoDescanso, IReadOnlyList<SerieResponseDto> Series, Guid? ExercicioOrigemId = null);
public sealed record SessaoResponseDto(Guid Id, Guid? TreinoId, string NomeTreino,
    DateTime Inicio, DateTime? Fim, string Status, string? Observacao,
    IReadOnlyList<SessaoExercicioResponseDto> Exercicios, ResumoSessaoDto Resumo,
    DateOnly? DataPresenca = null, string? FusoPresenca = null, Guid? TreinoOrigemId = null)
{
    public static SessaoResponseDto FromEntity(SessaoTreino session)
    {
        var series = session.Exercicios.SelectMany(e => e.Series).ToList();
        var completed = series.Where(s => s.Status == StatusSerie.Concluida).ToList();
        var duration = session.Fim.HasValue ? (int)Math.Clamp((session.Fim.Value - session.Inicio).TotalSeconds, 0, int.MaxValue) : 0;
        return new(session.Id, session.TreinoId, session.NomeTreino, session.Inicio,
            session.Fim, session.Status, session.Observacao,
            session.Exercicios.OrderBy(e => e.Ordem).Select(e => new SessaoExercicioResponseDto(
                e.Id, e.Nome, e.GrupoMuscular, e.Ordem, e.TempoDescanso,
                e.Series.OrderBy(s => s.Ordem).Select(s => new SerieResponseDto(
                    s.Id, s.Ordem, s.RepeticoesPlanejadas, s.CargaPlanejada, s.Repeticoes,
                    s.Carga, s.Status, s.ConcluidaEm)).ToArray(), e.ExercicioOrigemId)).ToArray(),
            new(duration, session.Exercicios.Count(e => e.Series.Any(s => s.Status == StatusSerie.Concluida)),
                completed.Count, series.Count(s => s.Status == StatusSerie.Pulada),
                completed.Sum(s => s.Carga!.Value * s.Repeticoes!.Value)),
            session.DataPresenca, session.FusoPresenca, session.TreinoOrigemId);
    }
}
