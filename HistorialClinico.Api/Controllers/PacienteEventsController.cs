using HistorialClinico.Api.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HistorialClinico.Api.Controllers;

[Authorize]
[ApiController, Route("api/historiales/eventos-pacientes")]
public sealed class PacienteEventsController(IPacienteEventInbox eventInbox) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<PacienteEventReceived>> GetReceivedEvents() =>
        Ok(eventInbox.GetAll());
}
