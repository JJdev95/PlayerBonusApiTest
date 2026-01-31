using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayerBonusApi.Application.Contracts;
using PlayerBonusApi.Application.Dtos;

namespace PlayerBonusApi.Controllers;

[ApiController]
[Route("api/bonus")]
[Authorize]
public sealed class BonusController(IPlayerBonusService service) : ControllerBase
{
    private readonly IPlayerBonusService _service = service;

    [HttpGet("all")]
    [ProducesResponseType(typeof(PagedResult<BonusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BonusDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(page, pageSize, ct));

    [HttpPost("create")]
    [ProducesResponseType(typeof(BonusDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<BonusDto>> Create([FromBody] CreateBonusRequest request, CancellationToken ct = default)
    {
        var dto = await _service.CreateAsync(request.PlayerId, request.BonusType, request.Amount, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BonusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BonusDto>> GetById([FromRoute] int id, CancellationToken ct = default)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPut("update/{id}")]
    [ProducesResponseType(typeof(BonusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BonusDto>> Update(
        [FromRoute] int id,
        [FromBody] UpdateBonusRequest request,
        CancellationToken ct = default)
        => Ok(await _service.UpdateAsync(id, request.Amount, request.IsActive, ct));

    [HttpDelete("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
    {
        await _service.SoftDeleteAsync(id, ct);
        return NoContent();
    }
}
