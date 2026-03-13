using RestauranteAPI.DTOs.Comanda;
using RestauranteAPI.Enums;

namespace RestauranteAPI.Services.Interfaces;

public interface IComandaService
{
    Task<IEnumerable<ComandaDetalheDto>> ListarAsync(StatusComanda? filtroStatus = null);
    Task<ComandaDetalheDto> ObterPorIdAsync(Guid id);
    Task<ComandaDetalheDto> AbrirAsync(AbrirComandaDto dto, string usuarioId);
    Task<ComandaDetalheDto> AdicionarItemAsync(Guid comandaId, AdicionarItemComandaDto dto);
    Task<ComandaDetalheDto> AtualizarStatusAsync(Guid comandaId, StatusComanda novoStatus);
    Task<ComandaDetalheDto> RemoverItemAsync(Guid comandaId, Guid itemComandaId);
}
