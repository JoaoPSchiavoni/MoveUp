namespace MoveUp.DTOs.Acompanhamento;

public record ConfigurarAcompanhamentoDto(string Fuso, int[] DiasSemana, int MetaSemanal);
public record RotinaDto(DateOnly Inicio, int[] DiasSemana);
public record MetaDto(DateOnly Inicio, int Dias);
public record ConfiguracaoDto(bool Ativado, string Fuso, DateOnly Hoje, DateOnly? Inicio,
    IReadOnlyList<RotinaDto> Rotinas, IReadOnlyList<MetaDto> Metas);
public record SessaoDiaDto(Guid Id, string NomeTreino);
public record DiaDto(DateOnly Data, string Estado, bool Planejado, IReadOnlyList<SessaoDiaDto> Sessoes);
public record CalendarioDto(DateOnly Hoje, string Fuso, IReadOnlyList<DiaDto> Dias);
public record SemanaDto(DateOnly Inicio, DateOnly Fim, int DiasTreinados, int Sessoes,
    int? Meta, int PlanejadosEncerrados, int PlanejadosCumpridos, double? Adesao,
    double DuracaoSegundos, double Volume);
public record PainelDto(DateOnly Hoje, string Fuso, bool Ativado, int SequenciaAtual,
    int MelhorSequencia, int DiasTreinadosMes, SemanaDto Semana);
