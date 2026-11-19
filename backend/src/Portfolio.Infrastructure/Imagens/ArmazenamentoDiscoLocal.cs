using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Portfolio.Core.Imagens;

namespace Portfolio.Infrastructure.Imagens;

// Imagens em disco, numa pasta fora do wwwroot (SEG20, RNF05). Troca-se por blob storage
// implementando IArmazenamentoImagens, sem mexer nos services.
public sealed partial class ArmazenamentoDiscoLocal : IArmazenamentoImagens
{
    private readonly string _pasta;

    public ArmazenamentoDiscoLocal(OpcoesImagens opcoes, IHostEnvironment ambiente)
    {
        _pasta = Path.GetFullPath(opcoes.Pasta, ambiente.ContentRootPath);
        Directory.CreateDirectory(_pasta);
    }

    // Grava primeiro num ficheiro temporário: quem lê nunca vê uma imagem a meio.
    // A pasta é garantida em cada gravação, e não só ao arrancar: se for apagada ou se o disco
    // for montado depois, os envios continuam a funcionar sem reiniciar a API.
    public async Task GuardarAsync(string ficheiro, byte[] conteudo, CancellationToken ct)
    {
        var caminho = Caminho(ficheiro);
        Directory.CreateDirectory(_pasta);
        var temporario = caminho + ".tmp";
        await File.WriteAllBytesAsync(temporario, conteudo, ct);
        File.Move(temporario, caminho);
    }

    public Stream? Abrir(string ficheiro)
    {
        var caminho = Caminho(ficheiro);
        return File.Exists(caminho) ? File.OpenRead(caminho) : null;
    }

    // Chamado depois de a base de dados confirmar a remoção. Se o disco falhar, o ficheiro
    // fica órfão, mas a operação já teve sucesso e não deve devolver erro por isso.
    public void Apagar(string ficheiro)
    {
        try
        {
            File.Delete(Caminho(ficheiro));
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
        }
    }

    // Só nomes gerados pela aplicação (GUID + extensão): nada de "../" nem caminhos absolutos.
    private string Caminho(string ficheiro)
    {
        if (!NomeGerado().IsMatch(ficheiro))
            throw new ArgumentException($"Nome de ficheiro inválido: {ficheiro}", nameof(ficheiro));
        return Path.Combine(_pasta, ficheiro);
    }

    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|png|webp)$")]
    private static partial Regex NomeGerado();
}
