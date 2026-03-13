using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs.Comanda;
using RestauranteAPI.Enums;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComandasController : ControllerBase
{
    private readonly IComandaService _service;

    public ComandasController(IComandaService service) => _service = service;

    /// <summary>Listar comandas, com filtro opcional por status</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Garcom,Cozinha,Caixa")]
    public async Task<IActionResult> Listar([FromQuery] StatusComanda? status)
    {
        var comandas = await _service.ListarAsync(status);
        return Ok(comandas);
    }

    /// <summary>Detalhes de uma comanda</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Garcom,Cozinha,Caixa")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var comanda = await _service.ObterPorIdAsync(id);
        return Ok(comanda);
    }

    /// <summary>Abrir nova comanda para uma mesa</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Garcom")]
    public async Task<IActionResult> Abrir([FromBody] AbrirComandaDto dto)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var comanda = await _service.AbrirComandaAsync(dto, usuarioId);
        return CreatedAtAction(nameof(ObterPorId), new { id = comanda.Id }, comanda);
    }

    /// <summary>Adicionar item à comanda</summary>
    [HttpPost("{id:guid}/itens")]
    [Authorize(Roles = "Admin,Garcom")]
    public async Task<IActionResult> AdicionarItem(Guid id, [FromBody] AdicionarItemComandaDto dto)
    {
        var comanda = await _service.AdicionarItemAsync(id, dto);
        return Ok(comanda);
    }

    /// <summary>Remover item da comanda (apenas enquanto não em preparo)</summary>
    [HttpDelete("{id:guid}/itens/{itemComandaId:guid}")]
    [Authorize(Roles = "Admin,Garcom")]
    public async Task<IActionResult> RemoverItem(Guid id, Guid itemComandaId)
    {
        await _service.RemoverItemAsync(id, itemComandaId);
        return NoContent();
    }

    /// <summary>Atualizar status da comanda</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Garcom,Cozinha,Caixa")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusDto dto)
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var comanda = await _service.AtualizarStatusAsync(id, dto.NovoStatus, usuarioId);
        return Ok(comanda);
    }
}
