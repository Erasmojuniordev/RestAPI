using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;
using RestauranteAPI.DTOs.Item;
using RestauranteAPI.Models;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Services;

public class ItemService : IItemService
{
    private readonly AppDbContext _db;

    public ItemService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ItemDto>> ListarAsync(bool? apenasDisponiveis = null)
    {
        var query = _db.Itens.AsQueryable();

        if (apenasDisponiveis.HasValue)
            query = query.Where(i => i.Disponivel == apenasDisponiveis.Value);

        return await query
            .OrderBy(i => i.Categoria).ThenBy(i => i.Nome)
            .Select(i => MapToDto(i))
            .ToListAsync();
    }

    public async Task<ItemDto> ObterPorIdAsync(Guid id)
    {
        var item = await _db.Itens.FindAsync(id)
            ?? throw new KeyNotFoundException("Item não encontrado.");
        return MapToDto(item);
    }

    public async Task<ItemDto> CriarAsync(CriarItemDto dto)
    {
        var item = new Item
        {
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            Preco = dto.Preco,
            Categoria = dto.Categoria,
            ImagemUrl = dto.ImagemUrl
        };

        _db.Itens.Add(item);
        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<ItemDto> AtualizarAsync(Guid id, AtualizarItemDto dto)
    {
        var item = await _db.Itens.FindAsync(id)
            ?? throw new KeyNotFoundException("Item não encontrado.");

        // Atualiza apenas os campos enviados (partial update)
        if (dto.Nome is not null) item.Nome = dto.Nome;
        if (dto.Descricao is not null) item.Descricao = dto.Descricao;
        if (dto.Preco.HasValue) item.Preco = dto.Preco.Value;
        if (dto.Categoria is not null) item.Categoria = dto.Categoria;
        if (dto.ImagemUrl is not null) item.ImagemUrl = dto.ImagemUrl;

        item.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task AlterarDisponibilidadeAsync(Guid id, bool disponivel)
    {
        var item = await _db.Itens.FindAsync(id)
            ?? throw new KeyNotFoundException("Item não encontrado.");

        item.Disponivel = disponivel;
        item.AtualizadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeletarAsync(Guid id)
    {
        var item = await _db.Itens.FindAsync(id)
            ?? throw new KeyNotFoundException("Item não encontrado.");

        // Verifica se o item está em alguma comanda ativa antes de deletar
        var emUso = await _db.ItensComanda.AnyAsync(ic =>
            ic.ItemId == id &&
            ic.Comanda.Status != RestauranteAPI.Enums.StatusComanda.Paga &&
            ic.Comanda.Status != RestauranteAPI.Enums.StatusComanda.Cancelada);

        if (emUso)
            throw new InvalidOperationException("Item está em uso em uma comanda ativa. Desative-o em vez de deletar.");

        _db.Itens.Remove(item);
        await _db.SaveChangesAsync();
    }

    private static ItemDto MapToDto(Item i) => new(i.Id, i.Nome, i.Descricao, i.Preco, i.Categoria, i.ImagemUrl, i.Disponivel);
}
