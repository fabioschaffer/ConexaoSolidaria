namespace Application.Campanhas;

public sealed record CriarCampanhaDados(
    string Titulo,
    string Descricao,
    DateTimeOffset DataInicio,
    DateTimeOffset DataFim,
    decimal MetaFinanceira);
