using Domain.Enums;

namespace Application.Campanhas;

public sealed record CampanhaDados(
    string Titulo,
    string Descricao,
    DateTimeOffset DataInicio,
    DateTimeOffset DataFim,
    decimal MetaFinanceira,
    StatusCampanha Status);
