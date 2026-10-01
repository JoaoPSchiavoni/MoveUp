using MoveUp.Domain.Interfaces;
using MoveUp.DTOs.Treino;
using MoveUp.Entities;

namespace MoveUp.Application.Services;

public class TreinoService(ITreinoRepository treinos, IExercicioRepository exercicios)
{
    public async Task<List<TreinoResponseDto>> ListarAsync(CancellationToken ct) =>
        (await treinos.ObterTodosAsync(ct)).Select(TreinoResponseDto.FromEntity).ToList();
    public async Task<TreinoResponseDto> ObterAsync(Guid id, CancellationToken ct) =>
        TreinoResponseDto.FromEntity(await treinos.ObterPorIdAsync(id, ct)
            ?? throw new ApiException(404, "Treino não encontrado."));

    public async Task<TreinoResponseDto> SalvarAsync(Guid id, TreinoCreateDto dto, CancellationToken ct)
    {
        if (id == Guid.Empty) throw new ApiException(400, "ID do treino inválido.");
        var nome = Validation.Required(dto.Nome, "Nome", 120);
        var descricao = Validation.Description(dto.Descricao);
        if (dto.DiaSemana is < 1 or > 7) throw new ApiException(400, "Dia da semana deve estar entre 1 e 7.");
        if (dto.Exercicios is not { Count: > 0 }) throw new ApiException(400, "Selecione pelo menos um exercício.");
        var items = new List<TreinoExercicio>();
        var ids = new HashSet<Guid>();
        foreach (var item in dto.Exercicios)
        {
            if (item is null) throw new ApiException(400, "Exercício inválido.");
            if (item.ExercicioId == Guid.Empty || !ids.Add(item.ExercicioId))
                throw new ApiException(400, "Exercícios devem ter IDs válidos e não podem se repetir.");
            if (item.Series <= 0 || item.Repeticoes <= 0)
                throw new ApiException(400, "Séries e repetições devem ser maiores que zero.");
            if (!double.IsFinite(item.CargaInicial) || item.CargaInicial < 0 || item.TempoDescanso < 0)
                throw new ApiException(400, "Carga e descanso devem ser valores válidos, maiores ou iguais a zero.");
            items.Add(new TreinoExercicio { TreinoId = id, ExercicioId = item.ExercicioId,
                Ordem = items.Count, Series = item.Series, Repeticoes = item.Repeticoes,
                CargaInicial = item.CargaInicial, TempoDescanso = item.TempoDescanso });
        }
        if (!await exercicios.ExistemAsync(ids, ct)) throw new ApiException(400, "Um ou mais exercícios não existem no catálogo.");
        await treinos.SalvarAsync(new Treino { Id = id, Nome = nome, Descricao = descricao,
            DiaSemana = dto.DiaSemana, Ativo = dto.Ativo, TreinoExercicios = items }, ct);
        return await ObterAsync(id, ct);
    }
    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        if (!await treinos.ExcluirAsync(id, ct)) throw new ApiException(404, "Treino não encontrado.");
    }
}
