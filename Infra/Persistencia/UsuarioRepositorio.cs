using Application.Usuarios;
using Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infra.Persistencia;

public sealed class UsuarioRepositorio(ConexaoSolidariaContext contexto) : IUsuarioRepositorio
{
    public void Adicionar(Usuario usuario) => contexto.Usuarios.Add(usuario);

    public Task<Usuario?> ObterAsync(int id, CancellationToken cancellationToken)
        => contexto.Usuarios.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken cancellationToken)
        => await contexto.Usuarios.AsNoTracking()
            .OrderBy(u => u.Nome)
            .ThenBy(u => u.Id)
            .Select(u => new UsuarioDto(u.Id, u.Nome, u.Email, u.Cpf, u.Perfil))
            .ToListAsync(cancellationToken);

    public void Remover(Usuario usuario) => contexto.Usuarios.Remove(usuario);

    public async Task<ResultadoAlteracaoUsuario> SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            return ResultadoAlteracaoUsuario.Sucesso;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoAlteracaoUsuario.Conflito;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_Doacoes_Usuarios_DoadorId" })
        {
            return ResultadoAlteracaoUsuario.PossuiDoacoes;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation } erro)
        {
            switch (erro.ConstraintName)
            {
                case "IX_Usuarios_Email":
                    return ResultadoAlteracaoUsuario.EmailDuplicado;
                case "IX_Usuarios_Cpf":
                    return ResultadoAlteracaoUsuario.CpfDuplicado;
                default:
                    throw;
            }
        }
    }
}
