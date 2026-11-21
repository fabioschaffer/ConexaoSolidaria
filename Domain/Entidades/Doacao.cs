using Domain.Enums;

namespace Domain.Entidades;

public sealed class Doacao
{
    private Doacao()
    {
    }

    public int Id { get; private set; }
    public int CampanhaId { get; private set; }
    public int DoadorId { get; private set; }
    public Campanha Campanha { get; private set; } = null!;
    public Usuario Doador { get; private set; } = null!;
    public decimal Valor { get; private set; }
    public StatusDoacao Status { get; private set; }
    public DateTimeOffset DataRecebimento { get; private set; }
    public DateTimeOffset? DataProcessamento { get; private set; }

    public static Doacao Registrar(Campanha campanha, Usuario doador, decimal valor, DateTimeOffset dataRecebimento)
    {
        ArgumentNullException.ThrowIfNull(campanha);
        ArgumentNullException.ThrowIfNull(doador);

        if (valor <= 0 || decimal.Round(valor, 2) != valor)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "O valor deve ser positivo e expresso em reais e centavos.");
        }

        if (doador.Perfil != PerfilUsuario.Doador)
        {
            throw new InvalidOperationException("Apenas usuários com perfil Doador podem registrar doações.");
        }

        if (!campanha.AceitaDoacaoEm(dataRecebimento))
        {
            throw new InvalidOperationException("A campanha não aceita doações na data de recebimento.");
        }

        return new Doacao
        {
            CampanhaId = campanha.Id,
            DoadorId = doador.Id,
            Campanha = campanha,
            Doador = doador,
            Valor = valor,
            Status = StatusDoacao.Recebida,
            DataRecebimento = dataRecebimento.ToUniversalTime()
        };
    }

    public void RegistrarProcessamento(DateTimeOffset dataProcessamento)
    {
        if (dataProcessamento < DataRecebimento)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dataProcessamento), "A data de processamento não pode ser anterior à data de recebimento.");
        }

        if (DataProcessamento.HasValue && DataProcessamento.Value != dataProcessamento)
        {
            throw new InvalidOperationException("A doação já foi processada em outra data.");
        }

        DataProcessamento = dataProcessamento.ToUniversalTime();
        Status = StatusDoacao.Processada;
    }
}
