using RestauranteAPI.DTOs.Item;

namespace RestauranteAPI.Services.Interfaces;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> ListarAsync(bool? apenasDisponiveis = null);
    Task<ItemDto> ObterPorIdAsync(Guid id);
    Task<ItemDto> CriarAsync(CriarItemDto dto);
    Task<ItemDto> AtualizarAsync(Guid id, AtualizarItemDto dto);
    Task AlterarDisponibilidadeAsync(Guid id, bool disponivel);
    Task DeletarAsync(Guid id);
}
