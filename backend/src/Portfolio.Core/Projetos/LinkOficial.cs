namespace Portfolio.Core.Projetos;

// Link oficial de um projeto: o site do produto (por exemplo https://meuapp.pt), aberto pelo botão
// "Visitar site" (SEG30). A API nunca o visita: é só um link para o visitante.
public static class LinkOficial
{
    public const int TamanhoMaximo = 200;

    // Devolve o link na forma canónica, ou null se não for aceite:
    // - só https:// (nada de javascript:, data:, nem http:// sem cifra);
    // - sem utilizador nem password ("https://banco.pt@evil.example" engana quem lê);
    // - com um domínio a sério (com ponto), não "localhost" nem um nome interno.
    public static string? Ler(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;
        if (!Uri.TryCreate(texto.Trim(), UriKind.Absolute, out var uri))
            return null;

        var aceite = uri.Scheme == Uri.UriSchemeHttps
            && uri.UserInfo.Length == 0
            && uri.IdnHost.Contains('.')
            && !uri.IdnHost.StartsWith('.')
            && !uri.IdnHost.EndsWith('.');
        // O domínio fica sempre em ASCII (café.pt passa a xn--caf-dma.pt), como a base de dados exige.
        var porta = uri.IsDefaultPort ? "" : $":{uri.Port}";
        var canonico = $"https://{uri.IdnHost}{porta}{uri.PathAndQuery}{uri.Fragment}";
        return aceite && canonico.Length <= TamanhoMaximo ? canonico : null;
    }
}
