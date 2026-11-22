using Application.Doacoes;
using Domain.Entidades;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistencia;

public sealed class DoacaoRepositorio(ConexaoSolidariaContext contexto) : IDoacaoRepositorio
{
    public async Task<bool> ProcessarAsync(int doacaoId, CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);
        var consulta = contexto.Doacoes.Where(d => d.Id == doacaoId);
        var dataProcessamento = DateTimeOffset.UtcNow;
        var alteradas = await consulta.Where(d => d.Status == StatusDoacao.Recebida)
            .ExecuteUpdateAsync(d => d
                .SetProperty(x => x.Status, StatusDoacao.Processada)
                .SetProperty(x => x.DataProcessamento, dataProcessamento), cancellationToken);

        if (alteradas == 0)
        {
            return await consulta.AnyAsync(d => d.Status == StatusDoacao.Processada, cancellationToken);
        }

        var doacao = await consulta.Select(d => new { d.CampanhaId, d.Valor }).SingleAsync(cancellationToken);
        var campanhasAlteradas = await contexto.Campanhas
            .Where(c => c.Id == doacao.CampanhaId)
            .ExecuteUpdateAsync(c => c.SetProperty(
                x => x.ValorTotalArrecadado, x => x.ValorTotalArrecadado + doacao.Valor), cancellationToken);

        if (campanhasAlteradas != 1)
        {
            throw new InvalidOperationException($"Campanha {doacao.CampanhaId} não encontrada para a doação {doacaoId}.");
        }

        await transacao.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<Usuario?> ObterDoadorAsync(int id, CancellationToken cancellationToken)
        => await contexto.Usuarios.FindAsync([id], cancellationToken);

    public async Task<Campanha?> ObterCampanhaAsync(int id, CancellationToken cancellationToken)
        => await contexto.Campanhas.FindAsync([id], cancellationToken);

    public async Task SalvarAsync(Doacao doacao, CancellationToken cancellationToken)
    {
        contexto.Doacoes.Add(doacao);
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
