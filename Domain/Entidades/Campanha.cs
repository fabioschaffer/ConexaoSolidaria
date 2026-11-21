using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entidades;

public sealed class Campanha
{
    private Campanha()
    {
    }

    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public DateTimeOffset DataInicio { get; private set; }
    public DateTimeOffset DataFim { get; private set; }
    public decimal MetaFinanceira { get; private set; }
    public StatusCampanha Status { get; private set; }

    public static Campanha Criar(
        string titulo, string descricao, DateTimeOffset dataInicio, DateTimeOffset dataFim,
        decimal metaFinanceira, StatusCampanha status, DateTimeOffset agora)
    {
        if (dataFim < agora)
        {
            throw new ArgumentOutOfRangeException(nameof(dataFim), "A data de término não pode estar no passado.");
        }

        status = StatusCampanha.Ativa;

        var campanha = new Campanha { Id = Guid.NewGuid() };
        campanha.Editar(titulo, descricao, dataInicio, dataFim, metaFinanceira, status);
        return campanha;
    }

    public void Editar(
        string titulo, string descricao, DateTimeOffset dataInicio, DateTimeOffset dataFim,
        decimal metaFinanceira, StatusCampanha status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);

        if (dataFim <= dataInicio)
        {
            throw new ArgumentOutOfRangeException(nameof(dataFim), "O término deve ser posterior ao início.");
        }

        if (metaFinanceira <= 0 || decimal.Round(metaFinanceira, 2) != metaFinanceira)
        {
            throw new ArgumentOutOfRangeException(nameof(metaFinanceira), "A meta deve ser positiva e expressa em reais e centavos.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (Status == StatusCampanha.Cancelada && status != StatusCampanha.Cancelada)
        {
            throw new CampanhaCanceladaException();
        }

        Titulo = titulo.Trim();
        Descricao = descricao.Trim();
        DataInicio = dataInicio.ToUniversalTime();
        DataFim = dataFim.ToUniversalTime();
        MetaFinanceira = metaFinanceira;
        Status = status;
    }

    public void Cancelar() => Status = StatusCampanha.Cancelada;

    public bool AceitaDoacaoEm(DateTimeOffset dataRecebimento)
        => Status == StatusCampanha.Ativa && dataRecebimento >= DataInicio && dataRecebimento < DataFim;
}
