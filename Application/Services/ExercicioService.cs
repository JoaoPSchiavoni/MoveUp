using MoveUp.Domain.Interfaces;
using MoveUp.DTOs.Exercicio;
using MoveUp.Entities;

namespace MoveUp.Application.Services;

public class ExercicioService(IExercicioRepository repository)
{
    public async Task<List<ExercicioResponseDto>> ListarAsync(CancellationToken ct) =>
        (await repository.ObterTodosAsync(ct)).Select(ExercicioResponseDto.FromEntity).ToList();
    public async Task<ExercicioResponseDto> ObterAsync(Guid id, CancellationToken ct) =>
        ExercicioResponseDto.FromEntity(await repository.ObterPorIdAsync(id, ct)
            ?? throw new ApiException(404, "Exercício não encontrado."));
    public async Task<ExercicioResponseDto> SalvarAsync(Guid id, ExercicioSaveDto dto, bool create, CancellationToken ct)
    {
        if (!create) await ObterAsync(id, ct);
        var entity = new Exercicio { Id = id,
            Nome = Validation.Required(dto.Nome, "Nome", 120),
            GrupoMuscular = Validation.Required(dto.GrupoMuscular, "Grupo muscular", 80),
            Descricao = Validation.Description(dto.Descricao) };
        await repository.SalvarAsync(entity, ct);
        return await ObterAsync(id, ct);
    }
    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        if (await repository.EmUsoAsync(id, ct)) throw new ApiException(409, "Exercício utilizado em um treino. Remova os vínculos antes de excluí-lo.");
        if (!await repository.ExcluirAsync(id, ct)) throw new ApiException(404, "Exercício não encontrado.");
    }
}
