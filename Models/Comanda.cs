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
    private static readonly Dictionary<StatusComanda, IEnumerable<StatusComanda>> _transicoesPermitidas = new()
    {
        [StatusComanda.Aberta]    = [StatusComanda.Pendente, StatusComanda.Cancelada],
        [StatusComanda.Pendente]  = [StatusComanda.EmPreparo, StatusComanda.Cancelada],
        [StatusComanda.EmPreparo] = [StatusComanda.Pronto, StatusComanda.Cancelada],
        [StatusComanda.Pronto]    = [StatusComanda.Entregue],
        [StatusComanda.Entregue]  = [StatusComanda.Paga],
        [StatusComanda.Paga]      = [],
        [StatusComanda.Cancelada] = [],
    };

    public bool PodeTransicionarPara(StatusComanda novoStatus)
    {
        return _transicoesPermitidas.TryGetValue(Status, out var permitidos)
            && permitidos.Contains(novoStatus);
    }
}
