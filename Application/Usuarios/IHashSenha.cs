namespace Application.Usuarios;

public interface IHashSenha
{
    string Gerar(string senha);
    bool Verificar(string hash, string senha);
}
