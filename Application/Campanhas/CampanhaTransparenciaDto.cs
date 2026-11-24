using Domain.Enums;

namespace Application.Campanhas;

public sealed record CampanhaTransparenciaDto(
    int Id,
    string Titulo,
    decimal MetaFinanceira,
    decimal ValorTotalArrecadado,
    StatusCampanha Status);
