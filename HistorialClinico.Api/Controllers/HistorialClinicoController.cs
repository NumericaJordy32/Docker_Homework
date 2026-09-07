using HistorialClinico.Api.Data;
using HistorialClinico.Api.Dtos;
using HistorialClinico.Api.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Historial = HistorialClinico.Api.Models.HistorialClinico;

namespace HistorialClinico.Api.Controllers;

[Authorize]
[ApiController, Route("api/historiales")]
public sealed class HistorialClinicoController(IHistorialRepository repository, IRabbitMqPublisher publisher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Historial>>> GetAll(CancellationToken ct) => Ok(await repository.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Historial>> GetById(int id, CancellationToken ct)
    {
        var item = await repository.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Historial>> Create(HistorialRequest request, CancellationToken ct)
    {
        try
        {
            var item = await repository.CreateAsync(request, ct);
            await publisher.PublishAsync("historial.created", "HistorialCreado", item, ct);
            return CreatedAtAction(nameof(GetById), new { id = item.IdHistorialClinico }, item);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { return Conflict(new { message = "Ya existe un historial con ese número de historia." }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, HistorialRequest request, CancellationToken ct)
    {
        try
        {
            if (!await repository.UpdateAsync(id, request, ct)) return NotFound();
            await publisher.PublishAsync("historial.updated", "HistorialActualizado", new { IdHistorialClinico = id, request.IdPaciente, request.NumHistoria, request.Diagnostico, request.Tratamiento, request.Fecha }, ct);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { return Conflict(new { message = "Ya existe un historial con ese número de historia." }); }
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await repository.DeleteAsync(id, ct)) return NotFound();
        await publisher.PublishAsync("historial.deleted", "HistorialEliminado", new { IdHistorialClinico = id }, ct);
        return NoContent();
    }
}
