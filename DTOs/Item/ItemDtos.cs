namespace RestauranteAPI.DTOs.Item;

public record CriarItemDto(
    string Nome,
    string? Descricao,
    decimal Preco,
    string? Categoria,
    string? ImagemUrl
);

public record AtualizarItemDto(
    string? Nome,
    string? Descricao,
    decimal? Preco,
    string? Categoria,
    string? ImagemUrl
);

public record ItemDto(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    string? Categoria,
    string? ImagemUrl,
    bool Disponivel
);
