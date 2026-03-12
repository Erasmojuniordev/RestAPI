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

    public void AdicionarItem(ItemComanda novoItem)
    {
        if (Status == StatusComanda.Paga || Status == StatusComanda.Cancelada)
            throw new InvalidOperationException("Não é possível adicionar itens a uma comanda encerrada.");

        var existente = Itens.FirstOrDefault(i =>
            i.ItemId == novoItem.ItemId && i.Observacao == novoItem.Observacao);

        if (existente is not null)
            existente.Quantidade += novoItem.Quantidade;
        else
            Itens.Add(novoItem);

        if (Status == StatusComanda.Aberta ||
            Status == StatusComanda.EmPreparo ||
            Status == StatusComanda.Pronto)
            Status = StatusComanda.Pendente;

        RecalcularPrecoTotal();
        AtualizadoEm = DateTime.UtcNow;
    }

    public void RemoverItem(Guid itemComandaId)
    {
        if (Status >= StatusComanda.EmPreparo)
            throw new InvalidOperationException("Não é possível remover itens de uma comanda em preparo ou posterior.");

        var item = Itens.FirstOrDefault(i => i.Id == itemComandaId)
            ?? throw new KeyNotFoundException("Item não encontrado na comanda.");

        Itens.Remove(item);

        if (!Itens.Any())
            Status = StatusComanda.Aberta;

        RecalcularPrecoTotal();
        AtualizadoEm = DateTime.UtcNow;
    }

    public void TransicionarStatus(StatusComanda novoStatus)
    {
        if (!PodeTransicionarPara(novoStatus))
            throw new InvalidOperationException(
                $"Transição inválida: {Status} → {novoStatus}.");

        Status = novoStatus;
        AtualizadoEm = DateTime.UtcNow;

        if (novoStatus == StatusComanda.Paga || novoStatus == StatusComanda.Cancelada)
            RecalcularPrecoTotal(); // congela o valor final
    }

    private void RecalcularPrecoTotal()
    {
        if (Status == StatusComanda.Paga || Status == StatusComanda.Cancelada)
            return;
        PrecoTotal = Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    }
}
