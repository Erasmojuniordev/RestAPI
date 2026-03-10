namespace RestauranteAPI.Models;

public class ItemComanda
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ComandaId { get; set; }
    public Comanda Comanda { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public int Quantidade { get; set; }

    // Snapshot do preço no momento do pedido
    // Garante que mudanças futuras no cardápio não afetam pedidos já feitos
    public decimal PrecoUnitario { get; set; }

    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Calculado — não mapeado no banco
    public decimal PrecoTotal => Quantidade * PrecoUnitario;
}
