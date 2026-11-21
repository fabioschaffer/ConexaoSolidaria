using Domain.Enums;

namespace Application.Usuarios;

public sealed record CriarUsuarioDados(
    string Nome, string Email, string Cpf, PerfilUsuario Perfil, string Senha);

public sealed record AtualizarUsuarioDados(
    string Nome, string Email, string Cpf, PerfilUsuario Perfil, string? Senha = null);
