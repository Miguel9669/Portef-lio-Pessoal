namespace Portfolio.Core.Comum;

public enum TipoErro { NaoEncontrado, Invalido }

// Erro de uma operação dos services. Campo indica o campo do formulário com o problema.
public sealed record Erro(TipoErro Tipo, string Mensagem, string? Campo = null)
{
    public static Erro NaoEncontrado(string mensagem) => new(TipoErro.NaoEncontrado, mensagem);

    public static Erro Invalido(string campo, string mensagem) => new(TipoErro.Invalido, mensagem, campo);
}

// O valor de uma operação ou o erro que a impediu.
public sealed class Resultado<T>
{
    private Resultado(T? valor, Erro? erro)
    {
        Valor = valor;
        Erro = erro;
    }

    public T? Valor { get; }
    public Erro? Erro { get; }

    public static implicit operator Resultado<T>(T valor) => new(valor, null);

    public static implicit operator Resultado<T>(Erro erro) => new(default, erro);
}
