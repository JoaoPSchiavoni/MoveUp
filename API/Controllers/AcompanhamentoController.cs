using Microsoft.AspNetCore.Mvc;
using MoveUp.Application.Services;
using MoveUp.DTOs.Acompanhamento;

namespace MoveUp.API.Controllers;

[ApiController]
[Route("api/acompanhamento")]
public class AcompanhamentoController(AcompanhamentoService service) : ControllerBase
{
    [HttpGet("configuracao")]
    public Task<ConfiguracaoDto> Configuracao(CancellationToken ct) => service.ConfiguracaoAsync(ct);
    [HttpPut("configuracao")]
    public Task<ConfiguracaoDto> Configurar(ConfigurarAcompanhamentoDto dto, CancellationToken ct) => service.ConfigurarAsync(dto, ct);
    [HttpGet("calendario")]
    public Task<CalendarioDto> Calendario(int ano, int mes, CancellationToken ct) => service.CalendarioAsync(ano, mes, ct);
    [HttpGet("painel")]
    public Task<PainelDto> Painel(CancellationToken ct) => service.PainelAsync(ct);
    [HttpGet("semanas")]
    public Task<SemanaDto> Semana(DateOnly inicio, CancellationToken ct) => service.SemanaAsync(inicio, ct);
}
