using MoveUp.Domain.Interfaces;
using MoveUp.DTOs.Sessao;
using MoveUp.Entities;

namespace MoveUp.Application.Services;

public class SessaoService(ISessaoRepository sessions, ITreinoRepository workouts, AcompanhamentoService consistency)
{
    private async Task<SessaoTreino> Required(Guid id, CancellationToken ct) =>
        await sessions.ObterAsync(id, ct) ?? throw new ApiException(404, "Sessão não encontrada.");
    private static void RequireActive(SessaoTreino session)
    {
        if (session.Status != StatusSessao.EmAndamento)
            throw new ApiException(409, "Esta sessão já foi encerrada e não pode ser alterada.");
    }
    public async Task<SessaoResponseDto> ObterAsync(Guid id, CancellationToken ct) =>
        SessaoResponseDto.FromEntity(await Required(id, ct));
    public async Task<SessaoResponseDto?> AtivaAsync(CancellationToken ct)
    {
        var session = await sessions.ObterAtivaAsync(ct);
        return session is null ? null : SessaoResponseDto.FromEntity(session);
    }
    public async Task<HistoricoSessaoDto> HistoricoAsync(int page, int pageSize, string status, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)page * pageSize > int.MaxValue - 1)
            throw new ApiException(400, "Informe uma página válida e tamanho de página entre 1 e 100.");
        if (status != StatusSessao.Concluida) throw new ApiException(400, "O histórico lista somente sessões concluídas.");
        var results = await sessions.HistoricoAsync((page - 1) * pageSize, pageSize + 1, ct);
        return new(results.Take(pageSize).Select(SessaoResponseDto.FromEntity).ToArray(), results.Count > pageSize);
    }
    public Task<SessaoResponseDto> IniciarAsync(Guid id, IniciarSessaoDto dto, CancellationToken ct) =>
        sessions.EmTransacaoAsync(async () =>
        {
            if (id == Guid.Empty || dto.TreinoId == Guid.Empty) throw new ApiException(400, "IDs de sessão e treino são obrigatórios.");
            var existing = await sessions.ObterAsync(id, ct);
            if (existing is not null)
            {
                if (existing.TreinoOrigemId != dto.TreinoId) throw new ApiException(409, "Este ID de sessão pertence a outra ficha.");
                return SessaoResponseDto.FromEntity(existing);
            }
            if (await sessions.ObterAtivaAsync(ct) is not null)
                throw new ApiException(409, "Já existe um treino em andamento. Continue ou cancele essa sessão.");
            var workout = await workouts.ObterPorIdAsync(dto.TreinoId, ct)
                ?? throw new ApiException(404, "Ficha de treino não encontrada.");
            if (!workout.Ativo || workout.TreinoExercicios.Count == 0)
                throw new ApiException(400, "A ficha deve estar ativa e possuir exercícios.");
            // Bound snapshot expansion for an invalid or extremely large legacy workout.
            if (workout.TreinoExercicios.Sum(e => (long)e.Series) > 1000)
                throw new ApiException(400, "Uma sessão pode conter até 1000 séries.");
            var session = new SessaoTreino { Id = id, TreinoId = workout.Id, TreinoOrigemId = workout.Id,
                NomeTreino = workout.Nome, Inicio = DateTime.UtcNow };
            await consistency.AssignDateAsync(session, ct);
            foreach (var item in workout.TreinoExercicios.OrderBy(e => e.Ordem))
            {
                var exercise = new SessaoExercicio { SessaoTreinoId = id, ExercicioOrigemId = item.ExercicioId, Nome = item.Exercicio!.Nome,
                    GrupoMuscular = item.Exercicio.GrupoMuscular, Ordem = item.Ordem, TempoDescanso = item.TempoDescanso };
                for (var order = 0; order < item.Series; order++)
                    exercise.Series.Add(new SerieRealizada { SessaoExercicioId = exercise.Id, Ordem = order,
                        RepeticoesPlanejadas = item.Repeticoes, CargaPlanejada = item.CargaInicial });
                session.Exercicios.Add(exercise);
            }
            sessions.Adicionar(session);
            return SessaoResponseDto.FromEntity(session);
        }, ct);

    public Task<SessaoResponseDto> RegistrarAsync(Guid id, Guid serieId, RegistrarSerieDto dto, CancellationToken ct) =>
        sessions.EmTransacaoAsync(async () =>
        {
            var session = await Required(id, ct);
            RequireActive(session);
            var serie = session.Exercicios.SelectMany(e => e.Series).SingleOrDefault(s => s.Id == serieId)
                ?? throw new ApiException(404, "Série não encontrada nesta sessão.");
            if (dto.Status is not (StatusSerie.Pendente or StatusSerie.Concluida or StatusSerie.Pulada))
                throw new ApiException(400, "Status da série inválido.");
            if (dto.Status == StatusSerie.Concluida)
            {
                if (dto.Repeticoes is null or <= 0 || dto.Carga is null || !double.IsFinite(dto.Carga.Value) || dto.Carga < 0)
                    throw new ApiException(400, "Informe repetições inteiras positivas e carga finita maior ou igual a zero.");
                var otherVolume = session.Exercicios.SelectMany(e => e.Series)
                    .Where(s => s.Id != serieId && s.Status == StatusSerie.Concluida)
                    .Sum(s => s.Carga!.Value * s.Repeticoes!.Value);
                if (!double.IsFinite(otherVolume + dto.Carga.Value * dto.Repeticoes.Value))
                    throw new ApiException(400, "A carga informada excede o limite numérico do volume registrado.");
                if (serie.Status != dto.Status || serie.Carga != dto.Carga || serie.Repeticoes != dto.Repeticoes)
                    serie.ConcluidaEm = DateTime.UtcNow;
                serie.Carga = dto.Carga;
                serie.Repeticoes = dto.Repeticoes;
            }
            else
            {
                if (dto.Carga is not null || dto.Repeticoes is not null)
                    throw new ApiException(400, "Séries pendentes ou puladas não devem conter resultados realizados.");
                serie.Carga = null;
                serie.Repeticoes = null;
                serie.ConcluidaEm = null;
            }
            serie.Status = dto.Status;
            return SessaoResponseDto.FromEntity(session);
        }, ct);

    public Task<SessaoResponseDto> ConcluirAsync(Guid id, ConcluirSessaoDto dto, CancellationToken ct) =>
        sessions.EmTransacaoAsync(async () =>
        {
            var session = await Required(id, ct);
            if (session.Status == StatusSessao.Concluida) return SessaoResponseDto.FromEntity(session);
            RequireActive(session);
            var note = dto.Observacao?.Trim();
            if (note?.Length > 2000) throw new ApiException(400, "Observação deve ter até 2000 caracteres.");
            if (string.IsNullOrEmpty(note)) note = null;
            var series = session.Exercicios.SelectMany(e => e.Series).ToList();
            if (!series.Any(s => s.Status == StatusSerie.Concluida))
                throw new ApiException(400, "Conclua pelo menos uma série antes de finalizar o treino.");
            foreach (var serie in series.Where(s => s.Status == StatusSerie.Pendente)) serie.Status = StatusSerie.Pulada;
            session.Status = StatusSessao.Concluida;
            session.Observacao = note;
            session.Fim = EndTime(session);
            return SessaoResponseDto.FromEntity(session);
        }, ct);

    public Task<SessaoResponseDto> CancelarAsync(Guid id, CancellationToken ct) =>
        sessions.EmTransacaoAsync(async () =>
        {
            var session = await Required(id, ct);
            if (session.Status == StatusSessao.Cancelada) return SessaoResponseDto.FromEntity(session);
            RequireActive(session);
            session.Status = StatusSessao.Cancelada;
            session.Fim = EndTime(session);
            return SessaoResponseDto.FromEntity(session);
        }, ct);
    private static DateTime EndTime(SessaoTreino session)
    {
        var now = DateTime.UtcNow;
        return now < session.Inicio ? session.Inicio : now;
    }
}
