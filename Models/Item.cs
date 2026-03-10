namespace RestauranteAPI.Models;

public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public decimal Preco { get; set; }
    public string? Categoria { get; set; }
    public string? ImagemUrl { get; set; }
    public bool Disponivel { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    // Navegação
    public ICollection<ItemComanda> ItensComanda { get; set; } = [];
}
