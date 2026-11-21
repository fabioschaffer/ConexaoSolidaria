using Domain.Entidades;
using Domain.Enums;

namespace Application.Usuarios;

public sealed record UsuarioDto(int Id, string Nome, string Email, string Cpf, PerfilUsuario Perfil)
{
    internal static UsuarioDto De(Usuario usuario)
        => new(usuario.Id, usuario.Nome, usuario.Email, usuario.Cpf, usuario.Perfil);
}
