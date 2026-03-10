using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;
using RestauranteAPI.DTOs.Comanda;
using RestauranteAPI.Enums;
using RestauranteAPI.Hubs;
using RestauranteAPI.Models;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Services;

public class ComandaService : IComandaService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<CozinhaHub> _hub;

    public ComandaService(AppDbContext db, IHubContext<CozinhaHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<IEnumerable<ComandaResumoDto>> ListarAsync(StatusComanda? filtroStatus = null)
    {
        var query = _db.Comandas.Include(c => c.Itens).AsQueryable();

        if (filtroStatus.HasValue)
            query = query.Where(c => c.Status == filtroStatus.Value);

        return await query
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new ComandaResumoDto(
                c.Id,
                c.NumeroDaMesa,
                c.Status.ToString(),
                c.PrecoTotal,
                c.Itens.Sum(i => i.Quantidade),
                c.CriadoEm
            ))
            .ToListAsync();
    }

    public async Task<ComandaDetalheDto> ObterPorIdAsync(Guid id)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens).ThenInclude(i => i.Item)
            .Include(c => c.AbertoPor)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        return MapToDetalheDto(comanda);
    }

    public async Task<ComandaDetalheDto> AbrirComandaAsync(AbrirComandaDto dto, string usuarioId)
    {
        // Valida se já existe comanda aberta/ativa para a mesa
        var mesaOcupada = await _db.Comandas.AnyAsync(c =>
            c.NumeroDaMesa == dto.NumeroDaMesa &&
            c.Status != StatusComanda.Paga &&
            c.Status != StatusComanda.Cancelada);

        if (mesaOcupada)
            throw new InvalidOperationException($"Mesa {dto.NumeroDaMesa} já possui uma comanda ativa.");

        var comanda = new Comanda
        {
            NumeroDaMesa = dto.NumeroDaMesa,
            Observacao = dto.Observacao,
            AbertoPorId = usuarioId
        };

        _db.Comandas.Add(comanda);
        await _db.SaveChangesAsync();

        return MapToDetalheDto(comanda);
    }

    public async Task<ComandaDetalheDto> AdicionarItemAsync(Guid comandaId, AdicionarItemComandaDto dto)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(c => c.Id == comandaId)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        if (comanda.Status == StatusComanda.Paga || comanda.Status == StatusComanda.Cancelada)
            throw new InvalidOperationException("Não é possível adicionar itens a uma comanda encerrada.");

        var item = await _db.Itens.FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.Disponivel)
            ?? throw new KeyNotFoundException("Item não encontrado ou indisponível.");

        var itemExistente = comanda.Itens.FirstOrDefault(i =>
            i.ItemId == dto.ItemId &&
            i.Observacao == dto.Observacao);

        if (itemExistente is not null)
        {
            // Só atualiza — EF já está rastreando essa entidade
            itemExistente.Quantidade += dto.Quantidade;
        }
        else
        {
            // Novo item
            var novoItem = new ItemComanda
            {
                ComandaId = comandaId,
                ItemId = item.Id,
                Quantidade = dto.Quantidade,
                PrecoUnitario = item.Preco,
                Observacao = dto.Observacao
            };
            _db.ItensComanda.Add(novoItem);
            comanda.Itens.Add(novoItem);
        }

        RecalcularPrecoTotal(comanda);
        comanda.AtualizadoEm = DateTime.UtcNow;

        if (comanda.Status == StatusComanda.Aberta)
            comanda.Status = StatusComanda.Pendente;

        await _db.SaveChangesAsync();

        await _hub.Clients.Group("Cozinha").SendAsync("NovoPedido", new
        {
            comandaId = comanda.Id,
            mesa = comanda.NumeroDaMesa,
            item = item.Nome,
            quantidade = dto.Quantidade,
            observacao = dto.Observacao
        });

        return MapToDetalheDto(comanda);
    }

    public async Task<ComandaDetalheDto> AtualizarStatusAsync(Guid comandaId, StatusComanda novoStatus, string usuarioId)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(c => c.Id == comandaId)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        if (!comanda.PodeTransicionarPara(novoStatus))
            throw new InvalidOperationException(
                $"Transição inválida: {comanda.Status} → {novoStatus}.");

        var statusAnterior = comanda.Status;
        comanda.Status = novoStatus;
        comanda.AtualizadoEm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Notificações por status
        await NotificarMudancaStatusAsync(comanda, statusAnterior);

        return MapToDetalheDto(comanda);
    }

    public async Task RemoverItemAsync(Guid comandaId, Guid itemComandaId)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == comandaId)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        // Só permite remoção enquanto não estiver em preparo
        if (comanda.Status >= StatusComanda.EmPreparo)
            throw new InvalidOperationException("Não é possível remover itens de uma comanda em preparo ou posterior.");

        var itemComanda = comanda.Itens.FirstOrDefault(i => i.Id == itemComandaId)
            ?? throw new KeyNotFoundException("Item não encontrado na comanda.");

        _db.ItensComanda.Remove(itemComanda);
        comanda.Itens.Remove(itemComanda);
        RecalcularPrecoTotal(comanda);
        comanda.AtualizadoEm = DateTime.UtcNow;

        // Se removeu todos os itens, volta para Aberta
        if (!comanda.Itens.Any())
            comanda.Status = StatusComanda.Aberta;

        await _db.SaveChangesAsync();
    }

    // ── Helpers ──────────────────────────────────────────────

    private static void RecalcularPrecoTotal(Comanda comanda)
    {
        // Não permite recálculo em comandas já encerradas
        if (comanda.Status == StatusComanda.Paga ||
            comanda.Status == StatusComanda.Cancelada)
            return;

        comanda.PrecoTotal = comanda.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    }

    private async Task NotificarMudancaStatusAsync(Comanda comanda, StatusComanda statusAnterior)
    {
        var payload = new
        {
            comandaId = comanda.Id,
            mesa = comanda.NumeroDaMesa,
            statusAnterior = statusAnterior.ToString(),
            novoStatus = comanda.Status.ToString()
        };

        // Pronto → notifica garçom para buscar o pedido
        if (comanda.Status == StatusComanda.Pronto)
            await _hub.Clients.Group("Garcom").SendAsync("PedidoPronto", payload);

        // Entregue → notifica caixa que mesa pode pagar
        if (comanda.Status == StatusComanda.Entregue)
            await _hub.Clients.Group("Caixa").SendAsync("ComandaProntoParaPagar", payload);

        // Qualquer mudança → cozinha acompanha
        await _hub.Clients.Group("Cozinha").SendAsync("StatusAtualizado", payload);
    }

    private static ComandaDetalheDto MapToDetalheDto(Comanda c) => new(
        c.Id,
        c.NumeroDaMesa,
        c.Status.ToString(),
        c.PrecoTotal,
        c.Observacao,
        c.CriadoEm,
        c.AtualizadoEm,
        c.AbertoPor?.NomeCompleto,
        c.Itens.Select(i => new ItemComandaDto(
            i.Id,
            i.ItemId,
            i.Item?.Nome ?? "",
            i.Quantidade,
            i.PrecoUnitario,
            i.Quantidade * i.PrecoUnitario,
            i.Observacao
        ))
    );
}
