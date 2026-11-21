using Domain.Entidades;
using Domain.Enums;

namespace Application.Campanhas;

public sealed record CampanhaDto(
    Guid Id,
    string Titulo,
    string Descricao,
    DateTimeOffset DataInicio,
    DateTimeOffset DataFim,
    decimal MetaFinanceira,
    StatusCampanha Status)
{
    internal static CampanhaDto De(Campanha campanha) => new(
        campanha.Id, campanha.Titulo, campanha.Descricao, campanha.DataInicio,
        campanha.DataFim, campanha.MetaFinanceira, campanha.Status);
}
