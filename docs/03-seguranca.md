# Segurança — Portfólio Pessoal

**Versão:** 0.2 (rascunho) · **Data:** 2026-11-18 · **Base:** [01-requisitos.md](01-requisitos.md), [02-base-de-dados.md](02-base-de-dados.md)

## Visão geral

O ponto principal é proteger o acesso ao backoffice: só o administrador entra e só ele escreve. O site público só lê e, por isso, mesmo que alguém o imite, não consegue alterar nada.

| Área | Site público | Backoffice |
| --- | --- | --- |
| Autenticação | Nenhuma | Login obrigatório |
| Operações | Só leitura | Leitura e escrita |
| Utilizador da base de dados | `portfolio_leitura` (só `SELECT`) | `portfolio_escrita` (`SELECT`, `INSERT`, `UPDATE`, `DELETE`) |
| Rate limit | Geral, contra abuso | Apertado no login |

## Medidas

Cada medida tem um ID (SEG01…) para ser referida nos componentes e no código.

### Base de dados

- **SEG01 — Utilizadores separados por permissões.**
  - `portfolio_leitura` (site público): só `SELECT`. Mesmo com um erro no código, o site não consegue escrever.
  - `portfolio_escrita` (backoffice): `SELECT`, `INSERT`, `UPDATE` e `DELETE`, sem permissão para alterar tabelas.
  - `portfolio_migracoes`: o único com permissões de estrutura (`CREATE`, `ALTER`, `DROP`). É usado só no deploy, para aplicar migrações, e nunca pela aplicação em execução.
- **SEG02 — Dois `DbContext`.** Um `PublicoDbContext` com a connection string de leitura e um `BackofficeDbContext` com a de escrita. O de leitura usa `AsNoTracking` por defeito e não expõe `SaveChanges`.
- **SEG03 — Sem SQL concatenado.** O Entity Framework parametriza as consultas. Nunca usar `FromSqlRaw` com texto montado à mão.

### Login

- **SEG04 — Mensagem de erro genérica.** Qualquer falha devolve sempre "Email ou password incorretos", nunca qual dos dois falhou.
- **SEG05 — Tempo de resposta igual.** Se o utilizador não existir, verifica-se a password contra um hash Argon2 falso. Sem isto, a resposta "não existe" é muito mais rápida que "password errada" (cerca de 0 ms contra cerca de 50 ms), e esse tempo denuncia quais nomes existem, mesmo com a mensagem genérica.
- **SEG06 — Rate limit no login.** Usa o rate limiter nativo do ASP.NET Core:
  - por IP, por exemplo 5 tentativas a cada 15 minutos;
  - por conta, com bloqueio progressivo (1 min, 5 min, 15 min…), para travar ataques distribuídos por vários IPs.

  Atrás de um proxy (nginx, Cloudflare), é preciso configurar `ForwardedHeaders`; senão todos os pedidos parecem vir do IP do proxy e um atacante bloqueia toda a gente.
- **SEG07 — Password forte.** Mínimo de 12 caracteres, guardada com Argon2id ([RNF01](01-requisitos.md#requisitos-não-funcionais)).
- **SEG08 — Gestão da conta só por comando, sem endpoint.** Criar a conta e recuperar o acesso fazem-se por comandos de linha, que só quem tem acesso ao servidor (SSH) consegue correr. Na internet não fica exposto nada.
  - `criar-admin`: cria o utilizador do backoffice. Só funciona se ainda não existir nenhum. Um endpoint de "registo inicial" ficaria exposto na internet até alguém o usar, e quem lá chegasse primeiro ficava com o backoffice.
  - `repor-password`: define uma password nova, desativa o 2FA (obriga a configurá-lo de novo no login seguinte), apaga os códigos de recuperação, muda o `SecurityStamp` (termina todas as sessões) e limpa o bloqueio da conta. Serve para uma password esquecida e para a perda do telemóvel do 2FA.
  - Não há "Esqueci-me da password" por email: seria mais uma porta de entrada (quem entrasse no email entrava no backoffice) e obrigaria a um serviço de envio de emails.
  - A password nova é pedida de forma interativa, sem aparecer no ecrã, e nunca passada como argumento (ficaria no histórico da shell).
- **SEG09 — Autenticação de dois fatores (obrigatória).** O login pede a password e depois um código TOTP de 6 dígitos (Google Authenticator, Authy, Microsoft Authenticator). Com uma só conta, é a medida que mais protege: uma password roubada deixa de chegar para entrar.
  - **Ativação:** no primeiro login, o backoffice mostra um QR code com o segredo TOTP. O 2FA só fica ativo depois de se introduzir um código válido, para confirmar que a app ficou bem configurada.
  - **Segredo guardado cifrado:** o segredo TOTP precisa de ser lido para validar os códigos, por isso não pode ser um hash. Fica cifrado com a Data Protection API do ASP.NET Core, e a chave de cifra não fica na base de dados.
  - **Códigos de recuperação:** 10 códigos de uso único, mostrados uma só vez na ativação, para o caso de perderes o telemóvel. Na base de dados ficam só os hashes.
  - **O passo do código também tem rate limit** (SEG06). Um código de 6 dígitos tem 1 milhão de combinações e, sem limite, adivinha-se.
  - **Um código não serve duas vezes:** guarda-se o último intervalo de tempo usado, para que um código intercetado não possa ser reutilizado nos 30 segundos em que ainda é válido.

### Sessão

- **SEG10 — Cookie de sessão, não JWT no `localStorage`.** O backoffice é um site no browser, e um cookie com estes atributos é a opção mais segura:
  - `HttpOnly`: o JavaScript não lhe consegue ler, por isso um ataque XSS não o rouba;
  - `Secure`: só circula por HTTPS;
  - `SameSite=Strict`: o browser não o envia em pedidos vindos de outros sites.

  Um JWT guardado no `localStorage` é legível por qualquer script na página.
- **SEG11 — Proteção CSRF.** O `SameSite=Strict` já trava a maioria dos casos. Juntar o antiforgery token do ASP.NET Core nos pedidos de escrita.
- **SEG12 — Expiração e logout real.** A sessão expira, por exemplo, ao fim de 30 minutos sem atividade e 8 horas no máximo. O logout invalida a sessão no servidor. Mudar a password termina todas as sessões abertas (security stamp).

### Autorização

- **SEG13 — Fechado por defeito.** Uma `FallbackPolicy` global exige login em todos os endpoints. Os endpoints públicos são abertos um a um, com `[AllowAnonymous]`. Assim, um controller novo em que alguém se esqueça do `[Authorize]` fica protegido, em vez de exposto.
- **SEG14 — Rotas separadas.** O backoffice fica em `/api/backoffice/...` e o site em `/api/publico/...`, para se poderem aplicar políticas, rate limits e regras de proxy diferentes a cada um.
- **SEG15 — Utilizadores de projeto só veem o seu projeto (fora da v1).** Se um dia existirem utilizadores ligados a projetos, cada pedido tem de confirmar que o projeto pedido é o do utilizador com sessão. Sem esta verificação, basta trocar o id no URL para ver outro projeto (ataque IDOR).
- **SEG16 — Projetos não públicos não saem.** Todas as consultas públicas filtram por `Publico = true`, incluindo as imagens e a timeline (commits) desses projetos.

### Imagens

- **SEG17 — Validar o conteúdo, não o nome.** Verificar os primeiros bytes do ficheiro (assinatura JPEG, PNG ou WebP), e não só a extensão ou o `Content-Type`, que o cliente controla. SVG não é permitido, porque pode conter JavaScript.
- **SEG18 — Limite de tamanho.** 5 MB por imagem, aplicado no servidor (Kestrel e endpoint, com 6 MB para o pedido inteiro). Também se recusam imagens com mais de 40 megapíxeis: um ficheiro pequeno pode ter dimensões enormes e ocupar gigabytes de memória ao ser aberto.
- **SEG19 — Reprocessar as imagens.** Abrir e voltar a gravar cada imagem a partir dos píxeis (com o SkiaSharp), depois de aplicar a orientação guardada no EXIF. Isto remove os metadados EXIF, IPTC e XMP, que podem incluir a localização GPS de onde a foto foi tirada, que podem incluir a localização GPS de onde a foto foi tirada, e neutraliza ficheiros malformados.
- **SEG20 — Guardar fora da pasta pública.** Os ficheiros não ficam em `wwwroot`. São servidos por um endpoint que verifica se o projeto é público (SEG16) e que envia `X-Content-Type-Options: nosniff`.

### Conteúdo

- **SEG21 — Sanitizar o Markdown.** Os blocos são escritos em Markdown, e o Markdown aceita HTML lá dentro. Sem proteção, um `<script>` num bloco executa no browser de quem visita o site (XSS).
  - O conversor de Markdown corre com o HTML embutido desativado (no Markdig, `DisableHtml()`).
  - O HTML gerado passa ainda por um sanitizador (por exemplo HtmlSanitizer), que só deixa passar etiquetas seguras: parágrafos, títulos, listas, negrito, itálico, código, citações e links.
  - Links com `javascript:` são removidos; os links externos levam `rel="noopener noreferrer"`.
  - A conversão para HTML é feita no servidor, e o frontend mostra o HTML já sanitizado.

### GitHub

- **SEG29 — Timeline do GitHub sem abrir portas.** A timeline são os commits do repositório de cada projeto, lidos pela API (RF11).
  - **Só links do GitHub.** O link é validado ao gravar, com um formato fixo: `https://github.com/dono/repositorio`. A API só fala com `api.github.com` e monta ela própria o endereço a partir do dono e do repositório. Nunca segue um URL escrito por alguém, por isso não pode ser usada para fazer pedidos a outros servidores (SSRF).
  - **Os commits são texto, não HTML.** As mensagens e os nomes dos autores vêm de fora e podem conter `<script>`: a API devolve-os como texto simples e os frontends mostram-nos como texto, nunca como HTML nem como Markdown.
  - **Cache e limites.** Sem token, o GitHub aceita 60 pedidos por hora por IP. A API guarda os commits de cada repositório em cache durante 10 minutos, com tempo limite de 10 segundos por pedido. Se o GitHub falhar, mostra a última versão em cache; se não houver, a timeline aparece como indisponível, sem derrubar a página do projeto.
  - **Token opcional e só de leitura.** Um token do GitHub (`Github:Token`, em user-secrets ou variável de ambiente, SEG25) sobe o limite para 5000 pedidos por hora. Deve ser um *fine-grained token* só com leitura de conteúdos.
  - **Repositórios privados.** Com um token com acesso a repositórios privados, a API consegue ler os seus commits, e um projeto público passa a mostrar as mensagens desses commits a toda a gente. Por isso, só se associa a um projeto público um repositório cujas mensagens de commit possam ser públicas.

### Infraestrutura

- **SEG22 — HTTPS em tudo,** com HSTS.
- **SEG23 — Cabeçalhos de segurança:** `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `Referrer-Policy` e `frame-ancestors 'none'` (contra clickjacking).
- **SEG24 — CORS restrito.** A API só aceita pedidos vindos dos domínios dos dois frontends.
- **SEG25 — Segredos fora do Git.** O repositório é público no GitHub: connection strings, passwords e chaves ficam em `dotnet user-secrets` (desenvolvimento) e em variáveis de ambiente (produção), nunca no `appsettings.json`.
- **SEG26 — Erros sem detalhes em produção.** Respostas em formato ProblemDetails, sem stack traces nem mensagens internas.
- **SEG27 — Registo de acessos.** Registar os logins com e sem sucesso (data, IP, email tentado), sem nunca registar passwords.
- **SEG28 — Dependências atualizadas.** Correr `dotnet list package --vulnerable` regularmente, ou ativar o Dependabot no GitHub.

## O "código do lado do cliente" e o fingir ser cliente

Num site, não é possível garantir que um pedido vem do teu frontend e não de outro programa:

- Tudo o que está no frontend é público: qualquer pessoa vê o código no browser, incluindo qualquer chave ou segredo que lá se ponha.
- Um programa como o `curl` ou o Postman envia exatamente os mesmos pedidos que o browser.
- O CORS só protege utilizadores dentro do browser. Não impede pedidos feitos fora dele.

A proteção real vem das outras medidas:

| Situação | O que protege |
| --- | --- |
| Alguém imita o site público | Nada a proteger: só lê dados que já são públicos (SEG01, SEG16) |
| Alguém imita o backoffice | Sem sessão válida é recusado (SEG13); para ter sessão precisa da password e, com 2FA, do telemóvel (SEG09) |
| Alguém faz muitos pedidos | Rate limit (SEG06) |
| Outro site tenta usar a sessão do administrador | `SameSite=Strict` e token CSRF (SEG10, SEG11) |

Se quiseres reduzir ainda mais a exposição do backoffice, ele pode ficar acessível só a partir de IPs conhecidos ou através de uma VPN, por exemplo com Tailscale ou Cloudflare Access. Assim, o login nem sequer fica visível na internet.

## Valores por defeito

Valores iniciais propostos. Ficam na configuração (`appsettings.json`), por isso podem ser ajustados sem mexer no código.

| Parâmetro | Valor | Medida |
| --- | --- | --- |
| Tentativas de login por IP | 5 a cada 15 minutos | SEG06 |
| Bloqueio por conta | Após 5 falhas seguidas: 1 min, depois 5 min, depois 15 min | SEG06 |
| Tentativas de código 2FA | 5 por sessão de login; depois, recomeça desde a password | SEG06, SEG09 |
| Password | Mínimo 12 caracteres | SEG07 |
| Sessão sem atividade | 30 minutos | SEG12 |
| Duração máxima da sessão | 8 horas | SEG12 |
| Tamanho máximo por imagem | 5 MB | SEG18 |
| Cache dos commits de cada repositório | 10 minutos | SEG29 |
| Tempo limite de um pedido ao GitHub | 10 segundos | SEG29 |
| Rate limit geral do site público | 100 pedidos por minuto por IP | — |

## Decisões

| Decisão | Resultado |
| --- | --- |
| 2FA (SEG09) | Obrigatório na v1 |
| Backoffice restrito a IPs ou VPN | Fora da v1; decide-se quando se escolher o alojamento |
| Utilizadores de projeto (SEG15) | Fora da v1; só existe o utilizador do backoffice |
| Valores do rate limit, sessão e imagens | Os valores por defeito acima |
| Captcha no login | Não; o CSRF já está coberto por SEG10 e SEG11 |
