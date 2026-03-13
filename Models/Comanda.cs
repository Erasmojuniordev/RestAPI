using RestauranteAPI.Enums;

namespace RestauranteAPI.Models;

public class Comanda
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int NumeroDaMesa { get; set; }
    public StatusComanda Status { get; set; } = StatusComanda.Aberta;
    public string? Observacao { get; set; }

    // Preço total calculado via propriedade — não armazenado no banco
    // Mas guardamos também para histórico/performance em relatórios
    public decimal PrecoTotal { get; set; } = 0;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }

    // Quem abriu e fechou a comanda (rastreabilidade)
    public string? AbertoPorId { get; set; }
    public ApplicationUser? AbertoPor { get; set; }

    // Navegação
    public ICollection<ItemComanda> Itens { get; set; } = [];

    // Regras de transição de status — lógica centralizada no modelo
    public bool PodeTransicionarPara(StatusComanda novo) => (Status, novo) switch
    {
        (StatusComanda.Aberta, StatusComanda.Pendente) => true,
        (StatusComanda.Aberta, StatusComanda.Fechada) => true,  // garçom fecha
        (StatusComanda.Aberta, StatusComanda.Cancelada) => true,
        (StatusComanda.Pendente, StatusComanda.EmPreparo) => true,
        (StatusComanda.Pendente, StatusComanda.Cancelada) => true,
        (StatusComanda.EmPreparo, StatusComanda.Pronto) => true,
        (StatusComanda.EmPreparo, StatusComanda.Cancelada) => true,
        (StatusComanda.Pronto, StatusComanda.Aberta) => true,  // loop — cozinha entrega
        (StatusComanda.Pronto, StatusComanda.Fechada) => true,  // garçom fecha direto
        (StatusComanda.Fechada, StatusComanda.Paga) => true,
        _ => false
    };
}
