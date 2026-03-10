using RestauranteAPI.DTOs.Comanda;
using RestauranteAPI.Enums;

namespace RestauranteAPI.Services.Interfaces;

public interface IComandaService
{
    Task<IEnumerable<ComandaResumoDto>> ListarAsync(StatusComanda? filtroStatus = null);
    Task<ComandaDetalheDto> ObterPorIdAsync(Guid id);
    Task<ComandaDetalheDto> AbrirComandaAsync(AbrirComandaDto dto, string usuarioId);
    Task<ComandaDetalheDto> AdicionarItemAsync(Guid comandaId, AdicionarItemComandaDto dto);
    Task<ComandaDetalheDto> AtualizarStatusAsync(Guid comandaId, StatusComanda novoStatus, string usuarioId);
    Task RemoverItemAsync(Guid comandaId, Guid itemComandaId);
}
