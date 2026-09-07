using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Pacientes.Api.Data;
using Pacientes.Api.Dtos;
using Pacientes.Api.Messaging;
using Pacientes.Api.Models;

namespace Pacientes.Api.Controllers;

[Authorize]
[ApiController, Route("api/pacientes")]
public sealed class PacientesController(IPacienteRepository repository, IRabbitMqPublisher publisher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Paciente>>> GetAll(CancellationToken ct) => Ok(await repository.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Paciente>> GetById(int id, CancellationToken ct)
    {
        var item = await repository.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Paciente>> Create(PacienteRequest request, CancellationToken ct)
    {
        try
        {
            var item = await repository.CreateAsync(request, ct);
            await publisher.PublishAsync("paciente.created", "PacienteCreado", item, ct);
            return CreatedAtAction(nameof(GetById), new { id = item.IdPaciente }, item);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { return Conflict(new { message = "Ya existe un paciente con esa cédula." }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PacienteRequest request, CancellationToken ct)
    {
        try
        {
            if (!await repository.UpdateAsync(id, request, ct)) return NotFound();
            await publisher.PublishAsync("paciente.updated", "PacienteActualizado", new { IdPaciente = id, request.Cedula, request.Nombre, request.Apellido, request.Direccion }, ct);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) { return Conflict(new { message = "Ya existe un paciente con esa cédula." }); }
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await repository.DeleteAsync(id, ct)) return NotFound();
        await publisher.PublishAsync("paciente.deleted", "PacienteEliminado", new { IdPaciente = id }, ct);
        return NoContent();
    }
}
