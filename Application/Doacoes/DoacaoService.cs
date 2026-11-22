using Domain.Entidades;
using Domain.Enums;

namespace Application.Doacoes;

public sealed record RegistrarDoacaoDados(int IdCampanha, decimal ValorDoacao);

public sealed record DoacaoRecebidaDto(int Id, int CampanhaId, decimal Valor, StatusDoacao Status);

public enum ResultadoRegistroDoacao
{
    Recebida,
    DoadorInvalido,
    CampanhaNaoEncontrada,
    CampanhaIndisponivel,
    PublicacaoNaoConfirmada
}

public sealed class DoacaoService(
    IDoacaoRepositorio repositorio, IDoacaoRecebidaPublicador publicador, TimeProvider relogio)
{
    public async Task<(ResultadoRegistroDoacao Resultado, DoacaoRecebidaDto? Doacao)> RegistrarAsync(
        int doadorId, RegistrarDoacaoDados dados, CancellationToken cancellationToken)
    {
        var doador = await repositorio.ObterDoadorAsync(doadorId, cancellationToken);
        if (doador?.Perfil != PerfilUsuario.Doador)
        {
            return (ResultadoRegistroDoacao.DoadorInvalido, null);
        }

        var campanha = await repositorio.ObterCampanhaAsync(dados.IdCampanha, cancellationToken);
        if (campanha is null)
        {
            return (ResultadoRegistroDoacao.CampanhaNaoEncontrada, null);
        }

        var agora = relogio.GetUtcNow();
        if (!campanha.AceitaDoacaoEm(agora))
        {
            return (ResultadoRegistroDoacao.CampanhaIndisponivel, null);
        }

        var doacao = Doacao.Registrar(campanha, doador, dados.ValorDoacao, agora);
        await repositorio.SalvarAsync(doacao, cancellationToken);
        var evento = new DoacaoRecebidaEvent(doacao.Id, doacao.CampanhaId, doacao.Valor, doacao.DataRecebimento);
        var publicada = await publicador.PublicarAsync(evento, cancellationToken);
        var resposta = new DoacaoRecebidaDto(doacao.Id, doacao.CampanhaId, doacao.Valor, doacao.Status);
        return (publicada ? ResultadoRegistroDoacao.Recebida : ResultadoRegistroDoacao.PublicacaoNaoConfirmada,
            resposta);
    }
}
