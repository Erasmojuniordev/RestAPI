using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs.Item;
using RestauranteAPI.Services.Interfaces;

namespace RestauranteAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItensController : ControllerBase
{
    private readonly IItemService _service;

    public ItensController(IItemService service) => _service = service;

    /// <summary>Listar itens do cardápio (público para o front do cliente)</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar([FromQuery] bool? apenasDisponiveis)
    {
        var itens = await _service.ListarAsync(apenasDisponiveis);
        return Ok(itens);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var item = await _service.ObterPorIdAsync(id);
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Criar([FromBody] CriarItemDto dto)
    {
        var item = await _service.CriarAsync(dto);
        return CreatedAtAction(nameof(ObterPorId), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarItemDto dto)
    {
        var item = await _service.AtualizarAsync(id, dto);
        return Ok(item);
    }

    [HttpPatch("{id:guid}/disponibilidade")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AlterarDisponibilidade(Guid id, [FromQuery] bool disponivel)
    {
        await _service.AlterarDisponibilidadeAsync(id, disponivel);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        await _service.DeletarAsync(id);
        return NoContent();
    }
}
