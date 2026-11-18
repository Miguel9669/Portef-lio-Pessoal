using Portfolio.Core.Comum;
using Portfolio.Core.Entidades;
using Portfolio.Core.Projetos;

namespace Portfolio.Core.Imagens;

// Imagem já validada e reprocessada, pronta a gravar.
public sealed record ImagemProcessada(byte[] Conteudo, string Extensao, string TipoMime);

// SEG17, SEG19: valida o tipo real pelo conteúdo (JPEG, PNG ou WebP), volta a gravar a imagem
// e remove os metadados (EXIF, com a localização GPS). Devolve null se não for uma imagem aceite.
public interface IProcessadorImagens
{
    ImagemProcessada? Processar(Stream conteudo);
}

// RNF05, SEG20: os ficheiros ficam fora da base de dados e fora da pasta pública.
public interface IArmazenamentoImagens
{
    Task GuardarAsync(string ficheiro, byte[] conteudo, CancellationToken ct);
    Stream? Abrir(string ficheiro);
    void Apagar(string ficheiro);
}

public interface IRepositorioImagens
{
    Task<ImagemProjeto?> ObterAsync(int id, CancellationToken ct);

    // Por ordem.
    Task<List<ImagemProjeto>> ListarDoProjetoAsync(int projetoId, CancellationToken ct);
    void Adicionar(ImagemProjeto imagem);
    void Remover(ImagemProjeto imagem);
    Task GravarAsync(CancellationToken ct);
}

public sealed class OpcoesImagens
{
    // SEG18
    public long TamanhoMaximoBytes { get; set; } = 5 * 1024 * 1024;

    // Uma imagem pequena em bytes pode ter dimensões enormes e ocupar gigabytes ao abrir.
    public int MaximoMegapixeis { get; set; } = 40;

    public string Pasta { get; set; } = "armazenamento/imagens";
}

// Imagens dos projetos (RF13).
public sealed class ImagemService(
    IRepositorioImagens imagens,
    IRepositorioProjetos projetos,
    IProcessadorImagens processador,
    IArmazenamentoImagens armazenamento,
    OpcoesImagens opcoes)
{
    private const string CampoFicheiro = "ficheiro";

    public async Task<Resultado<ImagemProjeto>> EnviarAsync(int projetoId, Stream conteudo, string nomeOriginal, long tamanho, CancellationToken ct)
    {
        if (await projetos.ObterAsync(projetoId, ct) is null)
            return Erro.NaoEncontrado("Projeto não encontrado.");
        if (tamanho > opcoes.TamanhoMaximoBytes)
            return Erro.Invalido(CampoFicheiro, $"A imagem tem de ter no máximo {opcoes.TamanhoMaximoBytes / (1024 * 1024)} MB.");
        if (processador.Processar(conteudo) is not { } processada)
            return Erro.Invalido(CampoFicheiro, "Só são aceites imagens JPEG, PNG ou WebP.");

        var existentes = await imagens.ListarDoProjetoAsync(projetoId, ct);
        var imagem = new ImagemProjeto
        {
            ProjetoId = projetoId,
            Ficheiro = $"{Guid.NewGuid():N}{processada.Extensao}",
            NomeOriginal = NomeParaMostrar(nomeOriginal),
            TipoMime = processada.TipoMime,
            TamanhoBytes = processada.Conteudo.Length,
            Ordem = existentes.Count == 0 ? 0 : existentes.Max(i => i.Ordem) + 1,
        };

        // Primeiro o ficheiro, depois a linha. Se a linha falhar, apaga-se o ficheiro.
        await armazenamento.GuardarAsync(imagem.Ficheiro, processada.Conteudo, ct);
        try
        {
            imagens.Adicionar(imagem);
            await imagens.GravarAsync(ct);
        }
        catch
        {
            armazenamento.Apagar(imagem.Ficheiro);
            throw;
        }
        return imagem;
    }

    // O ficheiro só é apagado depois de a linha sair da base de dados.
    public async Task<Erro?> RemoverAsync(int id, CancellationToken ct)
    {
        var imagem = await imagens.ObterAsync(id, ct);
        if (imagem is null)
            return NaoEncontrada();

        imagens.Remover(imagem);
        await imagens.GravarAsync(ct);
        armazenamento.Apagar(imagem.Ficheiro);
        return null;
    }

    public async Task<Erro?> ReordenarAsync(int projetoId, IReadOnlyList<int> ids, CancellationToken ct)
    {
        if (await projetos.ObterAsync(projetoId, ct) is null)
            return Erro.NaoEncontrado("Projeto não encontrado.");

        var todas = await imagens.ListarDoProjetoAsync(projetoId, ct);
        var posicoes = ids.Distinct().Select((id, posicao) => (id, posicao)).ToDictionary(p => p.id, p => p.posicao);
        if (ids.Count != todas.Count || posicoes.Count != ids.Count || !todas.All(i => posicoes.ContainsKey(i.Id)))
            return Erro.Invalido("ids", "A lista tem de ter todas as imagens do projeto, cada uma uma vez.");

        foreach (var imagem in todas)
            imagem.Ordem = posicoes[imagem.Id];
        await imagens.GravarAsync(ct);
        return null;
    }

    public async Task<Resultado<(ImagemProjeto Imagem, Stream Conteudo)>> AbrirAsync(int id, CancellationToken ct)
    {
        var imagem = await imagens.ObterAsync(id, ct);
        if (imagem is null || armazenamento.Abrir(imagem.Ficheiro) is not { } conteudo)
            return NaoEncontrada();
        return (imagem, conteudo);
    }

    // Só para mostrar: sem caminho e com no máximo 255 caracteres. Nunca é usado para gravar.
    private static string NomeParaMostrar(string nome)
    {
        var semCaminho = Path.GetFileName(nome.Replace('\\', '/'));
        var limpo = new string(semCaminho.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (limpo.Length == 0)
            return "imagem";
        return limpo.Length <= 255 ? limpo : limpo[..255];
    }

    private static Erro NaoEncontrada() => Erro.NaoEncontrado("Imagem não encontrada.");
}
