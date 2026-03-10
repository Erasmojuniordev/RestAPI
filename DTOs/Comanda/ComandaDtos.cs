using RestauranteAPI.Enums;

namespace RestauranteAPI.DTOs.Comanda;

// ── Input DTOs ──────────────────────────────────

public record AbrirComandaDto(
    int NumeroDaMesa,
    string? Observacao
);

public record AdicionarItemComandaDto(
    Guid ItemId,
    int Quantidade,
    string? Observacao
);

public record AtualizarStatusDto(
    StatusComanda NovoStatus
);

// ── Output DTOs ─────────────────────────────────

public record ItemComandaDto(
    Guid Id,
    Guid ItemId,
    string NomeItem,
    int Quantidade,
    decimal PrecoUnitario,
    decimal PrecoTotal,
    string? Observacao
);

public record ComandaResumoDto(
    Guid Id,
    int NumeroDaMesa,
    string Status,
    decimal PrecoTotal,
    int TotalItens,
    DateTime CriadoEm
);

public record ComandaDetalheDto(
    Guid Id,
    int NumeroDaMesa,
    string Status,
    decimal PrecoTotal,
    string? Observacao,
    DateTime CriadoEm,
    DateTime? AtualizadoEm,
    string? AbertoPorNome,
    IEnumerable<ItemComandaDto> Itens
);
