using Microsoft.AspNetCore.Mvc;
using MoveUp.Application.Services;
using MoveUp.DTOs.Treino;

namespace MoveUp.API.Controllers;

[ApiController]
[Route("api/treinos")]
public class TreinosController(TreinoService service) : ControllerBase
{
    [HttpGet]
    public Task<List<TreinoResponseDto>> Listar(CancellationToken ct) => service.ListarAsync(ct);
    [HttpGet("{id:guid}")]
    public Task<TreinoResponseDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);
    [HttpPost]
    public async Task<ActionResult<TreinoResponseDto>> Criar(TreinoCreateDto dto, CancellationToken ct)
    {
        var result = await service.SalvarAsync(Guid.NewGuid(), dto, ct);
        return CreatedAtAction(nameof(Obter), new { id = result.Id }, result);
    }
    // Upsert with a client-generated UUID makes retries safe after a lost response.
    [HttpPut("{id:guid}")]
    public Task<TreinoResponseDto> Salvar(Guid id, TreinoCreateDto dto, CancellationToken ct) => service.SalvarAsync(id, dto, ct);
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    { await service.ExcluirAsync(id, ct); return NoContent(); }
}
