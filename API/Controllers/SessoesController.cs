using Microsoft.AspNetCore.Mvc;
using MoveUp.Application.Services;
using MoveUp.DTOs.Sessao;

namespace MoveUp.API.Controllers;

[ApiController]
[Route("api/sessoes")]
public class SessoesController(SessaoService service) : ControllerBase
{
    [HttpGet("ativa")]
    public async Task<ActionResult<SessaoResponseDto>> Ativa(CancellationToken ct)
    {
        var session = await service.AtivaAsync(ct);
        if (session is null) return NoContent();
        return Ok(session);
    }
    [HttpGet("{id:guid}")]
    public Task<SessaoResponseDto> Obter(Guid id, CancellationToken ct) => service.ObterAsync(id, ct);
    [HttpGet]
    public Task<HistoricoSessaoDto> Historico(CancellationToken ct, [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20, [FromQuery] string status = "concluida") =>
        service.HistoricoAsync(pagina, tamanhoPagina, status, ct);
    [HttpPut("{id:guid}")]
    public Task<SessaoResponseDto> Iniciar(Guid id, IniciarSessaoDto dto, CancellationToken ct) => service.IniciarAsync(id, dto, ct);
    [HttpPut("{id:guid}/series/{serieId:guid}")]
    public Task<SessaoResponseDto> Registrar(Guid id, Guid serieId, RegistrarSerieDto dto, CancellationToken ct) => service.RegistrarAsync(id, serieId, dto, ct);
    [HttpPut("{id:guid}/conclusao")]
    public Task<SessaoResponseDto> Concluir(Guid id, ConcluirSessaoDto dto, CancellationToken ct) => service.ConcluirAsync(id, dto, ct);
    [HttpPut("{id:guid}/cancelamento")]
    public Task<SessaoResponseDto> Cancelar(Guid id, CancellationToken ct) => service.CancelarAsync(id, ct);
}
