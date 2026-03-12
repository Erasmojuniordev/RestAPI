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

    public async Task<ComandaDetalheDto> AbrirAsync(AbrirComandaDto dto, string userId)
    {
        var mesaOcupada = await _db.Comandas.AnyAsync(c =>
            c.NumeroDaMesa == dto.NumeroDaMesa &&
            c.Status != StatusComanda.Paga &&
            c.Status != StatusComanda.Cancelada);

        if (mesaOcupada)
            throw new InvalidOperationException($"Mesa {dto.NumeroDaMesa} já possui uma comanda aberta.");

        var comanda = new Comanda
        {
            Id = Guid.NewGuid(),
            NumeroDaMesa = dto.NumeroDaMesa,
            Status = StatusComanda.Aberta,
            Observacao = dto.Observacao,
            AbertoPorId = userId,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
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

        var item = await _db.Itens
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.Disponivel)
            ?? throw new KeyNotFoundException("Item não encontrado ou indisponível.");

        var novoItem = new ItemComanda
        {
            Id = Guid.NewGuid(),
            ComandaId = comandaId,
            ItemId = item.Id,
            Quantidade = dto.Quantidade,
            PrecoUnitario = item.Preco,
            Observacao = dto.Observacao,
        };

        // Regra de negócio fica no domínio
        comanda.AdicionarItem(novoItem);

        await _db.SaveChangesAsync();

        await NotificarCozinhaAsync("NovoPedido", new
        {
            comandaId = comanda.Id,
            mesa = comanda.NumeroDaMesa,
            item = item.Nome,
            quantidade = dto.Quantidade,
        });

        return await CarregarDetalheAsync(comandaId);
    }

    public async Task<ComandaDetalheDto> RemoverItemAsync(Guid comandaId, Guid itemComandaId)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens).ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(c => c.Id == comandaId)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        comanda.RemoverItem(itemComandaId);

        await _db.SaveChangesAsync();

        return await CarregarDetalheAsync(comandaId);
    }

    public async Task<ComandaDetalheDto> AtualizarStatusAsync(Guid comandaId, StatusComanda novoStatus)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens).ThenInclude(i => i.Item)
            .Include(c => c.AbertoPor)
            .FirstOrDefaultAsync(c => c.Id == comandaId)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        comanda.TransicionarStatus(novoStatus);

        await _db.SaveChangesAsync();

        await EnviarNotificacaoStatusAsync(comanda, novoStatus);

        return MapToDetalheDto(comanda);
    }

    // ── Métodos privados de suporte ──────────────────────────────

    private async Task<ComandaDetalheDto> CarregarDetalheAsync(Guid comandaId)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens).ThenInclude(i => i.Item)
            .Include(c => c.AbertoPor)
            .FirstOrDefaultAsync(c => c.Id == comandaId)
            ?? throw new KeyNotFoundException("Comanda não encontrada.");

        return MapToDetalheDto(comanda);
    }

    private async Task NotificarCozinhaAsync(string evento, object payload)
    {
        await _hub.Clients.Group("Cozinha").SendAsync(evento, payload);
    }

    private async Task EnviarNotificacaoStatusAsync(Comanda comanda, StatusComanda novoStatus)
    {
        await _hub.Clients.Group("Cozinha").SendAsync("StatusAtualizado", new
        {
            comandaId = comanda.Id,
            mesa = comanda.NumeroDaMesa,
            status = novoStatus.ToString(),
        });

        if (novoStatus == StatusComanda.Pronto)
            await _hub.Clients.Group("Garcom").SendAsync("PedidoPronto", new
            {
                comandaId = comanda.Id,
                mesa = comanda.NumeroDaMesa,
            });

        if (novoStatus == StatusComanda.Entregue)
            await _hub.Clients.Group("Caixa").SendAsync("ComandaProntoParaPagar", new
            {
                comandaId = comanda.Id,
                mesa = comanda.NumeroDaMesa,
                precoTotal = comanda.PrecoTotal,
            });
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
