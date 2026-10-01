using Microsoft.AspNetCore.Mvc;
using MoveUp.Application.Services;
using MoveUp.DTOs.Exercicio;

namespace MoveUp.API.Controllers;

[ApiController]
[Route("api/exercicios")]
public class ExerciciosController(ExercicioService service) : ControllerBase
{
    [HttpGet]
    public Task<List<ExercicioResponseDto>> Listar(CancellationToken ct) => service.ListarAsync(ct);
    [HttpGet("{id:guid}")]
    public Task<ExercicioResponseDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);
    [HttpPost]
    public async Task<ActionResult<ExercicioResponseDto>> Criar(ExercicioSaveDto dto, CancellationToken ct)
    {
        var result = await service.SalvarAsync(Guid.NewGuid(), dto, true, ct);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }
    [HttpPut("{id:guid}")]
    public Task<ExercicioResponseDto> Atualizar(Guid id, ExercicioSaveDto dto, CancellationToken ct) => service.SalvarAsync(id, dto, false, ct);
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    { await service.ExcluirAsync(id, ct); return NoContent(); }
}
