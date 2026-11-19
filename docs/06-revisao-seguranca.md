# Revisão de Segurança — Portfólio Pessoal

**Versão:** 1.0 · **Data:** 2026-11-19 · **Base:** [03-seguranca.md](03-seguranca.md) · **Fase:** 8 do [plano](05-plano-implementacao.md)

## Visão geral

Cada medida de segurança (SEG01–SEG29) foi revista contra o código: onde está implementada e que teste automático prova que funciona. Os testes correm no CI em cada pull request (GitHub Actions) e antes de cada push (`./ci.sh`, hook `pre-push`).

| Resultado | Medidas |
| --- | --- |
| Implementada e com teste | 26 |
| Implementada, verificada de outra forma (análise estática, CI, configuração) | 2 (SEG03, SEG25) |
| Fora da v1 (decisão) | 1 (SEG15) |
| Por fazer, na fase 9 (deploy) | Partes de SEG06, SEG11 e SEG22, ver [Pendentes](#pendentes) |

Nesta revisão foram acrescentados os testes que faltavam a SEG22, SEG23 (site), SEG24, SEG26 e SEG27. Não se encontrou nenhuma falha nas medidas; os problemas encontrados durante as fases estão em [Problemas encontrados](#problemas-encontrados-durante-a-implementação).

Os nomes dos ficheiros são relativos a `backend/src/`, `backend/tests/Portfolio.Tests/` e `frontend/`.

## Base de dados

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG01** Utilizadores separados | `docker/postgres/init/01-utilizadores.sh`; connection strings `Leitura`, `Escrita` e `Migracoes` | `docker/postgres/verificar-permissoes.sh` (job `base-de-dados` do CI); `BaseDeDadosTests.O_utilizador_de_leitura_e_recusado_pela_base_de_dados` |
| **SEG02** Dois `DbContext` | `PublicoDbContext` (sem tracking, `SaveChanges` lança exceção), `BackofficeDbContext`; o site público só usa o `RepositorioPublico` | `BaseDeDadosTests.O_contexto_publico_nao_grava` |
| **SEG03** Sem SQL concatenado | Só Entity Framework com consultas LINQ (parametrizadas) | Procura em `backend/src`: nenhum `FromSqlRaw`, `ExecuteSqlRaw`, `SqlQueryRaw` nem `CommandText`; CodeQL (C#) em cada PR |

## Login

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG04** Mensagem genérica | `AutenticacaoController.Login`: a mesma resposta para email inexistente, password errada e conta bloqueada | `LoginTests.Password_errada_e_email_inexistente_devolvem_a_mesma_resposta`, `LoginTests.A_conta_bloqueia_depois_de_5_falhas` |
| **SEG05** Tempo igual | `AutenticacaoService`: com email inexistente verifica um hash Argon2 falso, calculado uma só vez | `AutenticacaoServiceTests.Com_um_email_inexistente_verifica_na_mesma_uma_password` |
| **SEG06** Rate limit e bloqueio | Rate limiter por IP (`ConfiguracaoAutenticacao.AddRateLimit`); bloqueio progressivo 1/5/15 min (`AutenticacaoService.RegistarFalha`) | `LoginTests.O_rate_limit_trava_os_logins_do_mesmo_ip`, `AutenticacaoServiceTests.O_bloqueio_e_progressivo_1_5_e_15_minutos`, `LoginTests.A_conta_bloqueia_depois_de_5_falhas` |
| **SEG07** Password forte, Argon2id | `ContaService` (12 a 256 caracteres); `Argon2PasswordHasher` (PHC, 19 MiB, t=2, p=1) | `ComandosContaTests.Criar_admin_recusa_passwords_fracas_ou_diferentes`, `Argon2PasswordHasherTests` |
| **SEG08** Conta só por comando | `ComandosConta` (`criar-admin`, `repor-password`); password pedida sem eco (`ConsolaSistema`) | `ComandosContaTests` (conta única, repor termina sessões e desativa o 2FA) |
| **SEG09** 2FA obrigatório | `DoisFatoresService`, `TotpOtpNet`, `CifraDataProtection`; códigos de recuperação com SHA-256 (80 bits) | `LoginTests.Um_codigo_2fa_reutilizado_e_recusado`, `…Um_codigo_de_recuperacao_so_serve_uma_vez`, `…Cinco_codigos_errados_obrigam_a_recomecar_pela_password`, `…Sem_2fa_a_sessao_parcial_nao_acede_ao_backoffice`, `CifraDataProtectionTests` |

## Sessão e autorização

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG10** Cookie seguro | `ConfiguracaoAutenticacao.AddCookie` (`HttpOnly`, `Secure`, `SameSite=Strict`) | `LoginTests.O_cookie_de_sessao_e_httponly_secure_e_samesite_strict` |
| **SEG11** CSRF | `FiltroCsrf` em todos os pedidos de escrita; token reemitido sempre que a sessão muda (`GestorSessao.EmitirTokenCsrf`) | `LoginTests.Um_pedido_de_escrita_sem_token_csrf_e_recusado`; frontends: `cliente.test.ts` (backoffice) |
| **SEG12** Expiração e logout real | `SessoesEmMemoria` (sessão guardada no servidor), `ValidadorSessao` (8 h, security stamp), expiração deslizante de 30 min | `LoginTests.O_logout_invalida_a_sessao_no_servidor`, `…A_sessao_expira_ao_fim_de_30_minutos_sem_atividade`, `…A_sessao_termina_ao_fim_de_8_horas_mesmo_com_atividade`, `…A_sessao_parcial_expira_ao_fim_de_5_minutos`, `ComandosContaTests.Repor_password_termina_as_sessoes_e_desativa_o_2fa` |
| **SEG13** Fechado por defeito | `FallbackPolicy` exige a sessão completa; o site público abre com `[AllowAnonymous]` | `LoginTests.Sem_sessao_o_backoffice_responde_401_em_problem_details`, `BackofficeConteudoTests.Sem_login_os_blocos_e_projetos_nao_estao_acessiveis`, `ArranqueTests.Em_producao_a_documentacao_da_api_nao_existe` |
| **SEG14** Rotas separadas | `/api/backoffice/...` e `/api/publico/...` | Todos os testes de API usam as duas rotas |
| **SEG15** Utilizadores de projeto | Fora da v1 (decisão em [03-seguranca.md](03-seguranca.md)) | — |
| **SEG16** Não públicos não saem | `RepositorioPublico`: o filtro `Publico = true` está só aí, em todas as consultas | `SitePublicoTests.Nada_de_um_projeto_nao_publico_aparece_em_nenhum_endpoint_publico`, `…Despublicar_tira_logo_o_projeto_e_as_imagens_do_site`; site: `paginas.test.tsx` (404) |

## Imagens

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG17** Validar o conteúdo | `ProcessadorSkia`: formato pelos primeiros bytes, só JPEG/PNG/WebP | `ProcessadorSkiaTests.Recusa_o_que_nao_e_uma_imagem_aceite` (texto, SVG, GIF), `ImagensApiTests.Um_ficheiro_que_nao_e_imagem_e_recusado_sem_deixar_nada_no_disco` |
| **SEG18** Limite de tamanho | 5 MB por imagem (`ImagemService`), 6 MB por pedido (Kestrel e `[RequestSizeLimit]`), máximo de 40 megapíxeis | `ImagensApiTests.Uma_imagem_acima_do_limite_e_recusada`, `ProcessadorSkiaTests.Recusa_imagens_com_dimensoes_exageradas` |
| **SEG19** Reprocessar | `ProcessadorSkia`: aplica a orientação e grava de novo a partir dos píxeis (sem EXIF, IPTC, XMP) | `ProcessadorSkiaTests.Remove_o_exif_e_aplica_a_orientacao`, `ImagensApiTests.Envia_uma_imagem_e_ve_a_no_projeto` |
| **SEG20** Fora da pasta pública | `ArmazenamentoDiscoLocal` (só nomes GUID + extensão); imagens públicas encontradas na base de dados antes de tocar no disco | `ArmazenamentoDiscoLocalTests.So_aceita_nomes_gerados_pela_aplicacao`, `SitePublicoTests.Pedidos_de_ficheiros_que_nao_sao_imagens_publicas_nao_servem_nada`, `ImagensApiTests.Apagar_um_projeto_apaga_os_ficheiros_das_imagens` |

## Conteúdo

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG21** Markdown sanitizado | `ConversorMarkdig` (HTML embutido desativado + HtmlSanitizer, `rel="noopener noreferrer"`); nos frontends, `dangerouslySetInnerHTML` só no componente `HtmlDaApi` (regra do ESLint) | `ConversorMarkdigTests` (`<script>`, `onerror`, `javascript:`, `data:`), `BackofficeConteudoTests.A_pre_visualizacao_devolve_o_html_sanitizado`, `SitePublicoTests.Os_blocos_publicos_vem_por_ordem_com_o_html_sanitizado` |

## Infraestrutura

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG22** HTTPS e HSTS | `UseHttpsRedirection`; `UseHsts` fora de desenvolvimento (1 ano, subdomínios); a API corre em HTTPS também em desenvolvimento | `InfraestruturaTests.Em_producao_as_respostas_levam_hsts`, `…Em_desenvolvimento_nao_ha_hsts` |
| **SEG23** Cabeçalhos de segurança | API: `ConfiguracaoApi.CabecalhosSeguranca` (CSP `default-src 'none'`, nosniff, DENY, `no-referrer`). Site: `next.config.ts` (CSP, nosniff, DENY, `Permissions-Policy`) | `LoginTests.As_respostas_levam_os_cabecalhos_de_seguranca`; site: `configuracao.test.ts` |
| **SEG24** CORS restrito | `Cors:Origens` (vazio por defeito; os dois frontends em desenvolvimento) | `InfraestruturaTests.O_cors_so_aceita_os_frontends` (incluindo `http://localhost:5173.evil.example`) |
| **SEG25** Segredos fora do Git | user-secrets (desenvolvimento) e variáveis de ambiente (produção); `.env` no `.gitignore` | gitleaks em todo o histórico, em cada PR; Trivy (segredos no repositório) |
| **SEG26** Erros sem detalhes | `AddProblemDetails`, `UseExceptionHandler`, `UseStatusCodePages`; site: `error.tsx` | `InfraestruturaTests.Em_producao_os_erros_nao_mostram_detalhes_internos`, `LoginTests.Sem_sessao_o_backoffice_responde_401_em_problem_details`; site: `paginas.test.tsx` |
| **SEG27** Registo de acessos | `AutenticacaoController`: resultado, email tentado e IP de cada login e código 2FA | `RegistoLoginsTests.Os_logins_sao_registados_sem_a_password` |
| **SEG28** Dependências atualizadas | NuGetAudit (avisos como erros), Dependabot (NuGet, npm com 14 dias de espera, Actions, Docker), Trivy, `npm audit`, Dependency Review | CI em cada PR; ver [Vulnerabilidades](#vulnerabilidades-nas-dependências) |

## GitHub

| Medida | Implementação | Prova |
| --- | --- | --- |
| **SEG29** Timeline do GitHub | `LinkGithub` (formato fixo), CHECK na base de dados, `GithubApi` (endereço montado pela API, links dos commits montados pela API), `CacheTimeline` (10 min, 1 min após falha); frontends mostram os commits como texto | `LinkGithubTests`, `BaseDeDadosTests.A_base_de_dados_so_aceita_links_de_repositorios_do_github`, `BackofficeConteudoTests.Um_link_que_nao_seja_do_github_e_recusado`, `GithubApiTests`, `CacheTimelineTests`; backoffice e site: testes da timeline com mensagens com HTML |

## Vulnerabilidades nas dependências

Verificado a 2026-11-19:

| Onde | Resultado |
| --- | --- |
| .NET (`dotnet list package --vulnerable --include-transitive`) | Nenhuma |
| Backoffice e site, dependências de produção (`npm audit --omit=dev`) | Nenhuma |
| Backoffice e site, dependências de desenvolvimento | 1 moderada: minimatch ([GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249)) |

**minimatch — risco aceite até ao fim de novembro de 2026.** Só existe nas ferramentas do ESLint, nunca vai para o browser nem para o servidor, e só é explorável com padrões *glob* maliciosos, que aqui são escritos no próprio projeto. A correção (typescript-eslint 8.73.0, eslint 10.15.0) tem menos de 14 dias; o Dependabot propõe-a quando passar o período de espera.

## Pendentes

Ficam para a fase 9 (deploy), porque dependem do servidor:

- **SEG06 e SEG11 atrás do proxy:** configurar `ForwardedHeaders` (`X-Forwarded-For` e `X-Forwarded-Proto`). Sem o IP real, o rate limit vê todos os pedidos a vir do nginx; sem o protocolo, a API julga que o pedido é HTTP e recusa emitir o token CSRF.
- **SEG22:** certificado Let's Encrypt e redirecionamento para HTTPS no nginx.
- **Chaves da Data Protection:** numa pasta própria (`DataProtection__PastaChaves`), com cópia de segurança. Se se perderem, terminam as sessões e o 2FA tem de ser reposto com `repor-password`.

## Problemas encontrados durante a implementação

Todos corrigidos, com teste:

| Fase | Problema | Como foi encontrado |
| --- | --- | --- |
| 3 | O teste do Markdown procurava texto escapado em vez de elementos HTML | Revisão do teste: passou a analisar o HTML como um browser |
| 6 | Login com erro 500 em desenvolvimento: o token CSRF só circula por HTTPS e a API corria em HTTP | Ao preparar o ambiente no browser; a API passou a correr em HTTPS também em desenvolvimento |
| 7 | Descrição das meta tags com espaços antes da pontuação | Testes do site |
| 7 | `textoSimples` podia deixar restos de etiquetas e recriar `<` a partir de `&lt;` (não explorável: o Next.js escapa as meta tags) | CodeQL (alerta alto); passou a percorrer o texto carácter a carácter |
| 8 | Testes do backend a falhar por esgotar os observadores de ficheiros (inotify) | Revisão: o recarregamento da configuração fica desligado nos testes |
