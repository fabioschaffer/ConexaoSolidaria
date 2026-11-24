using Application.Campanhas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/transparencia/campanhas")]
public sealed class TransparenciaController(CampanhaService campanhas) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CampanhaTransparenciaDto>>> Listar(
        [FromQuery] int? id,
        CancellationToken cancellationToken)
        => Ok(await campanhas.ListarParaTransparenciaAsync(id, cancellationToken));
}
