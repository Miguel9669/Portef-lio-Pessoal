import { expect, test } from '@playwright/test'
import { BACKOFFICE, CONTA, SITE } from '../ambiente'
import { codigoTotp, imagemPng } from './apoio'

// O fluxo principal da v1 (docs/05-plano-implementacao.md, fase 8), num browser real, com a API,
// a base de dados, o backoffice e o site a correr: login com 2FA, criar conteúdo, publicar e ver no site.
test('do backoffice ao site: login com 2FA, criar, publicar e ver', async ({ page }) => {
  await test.step('primeiro login: password e configuração do 2FA', async () => {
    await page.goto(`${BACKOFFICE}/login`)
    await page.getByLabel('Email').fill(CONTA.email)
    await page.getByLabel('Password').fill(CONTA.password)
    await page.getByRole('button', { name: 'Entrar' }).click()

    await expect(page.getByAltText('QR code para a app autenticadora')).toBeVisible()
    const segredo = (await page.locator('code').first().textContent())!.trim()
    await page.getByLabel('Código da app').fill(codigoTotp(segredo))
    await page.getByRole('button', { name: 'Ativar' }).click()

    await expect(page.getByRole('list', { name: 'Códigos de recuperação' }).getByRole('listitem')).toHaveCount(10)
    await page.getByRole('button', { name: 'Já guardei os códigos' }).click()
    await expect(page.getByText(CONTA.nome)).toBeVisible()
  })

  await test.step('criar um bloco visível, com a pré-visualização do Markdown', async () => {
    await page.getByRole('navigation').getByRole('link', { name: 'Blocos', exact: true }).click()
    await page.getByRole('link', { name: 'Novo bloco' }).click()
    await page.getByLabel('Título').fill('Olá, sou o teste E2E')
    await page.getByLabel('Texto (Markdown)').fill('Texto **importante** e <script>alert(1)</script>')
    await page.getByRole('switch', { name: 'Visível no site' }).click()

    // A pré-visualização é o HTML sanitizado pela API: o negrito sim, o <script> nunca (SEG21).
    const previsualizacao = page.getByLabel('Pré-visualização')
    await expect(previsualizacao.locator('strong')).toHaveText('importante')
    await expect(previsualizacao.locator('script')).toHaveCount(0)

    await page.getByRole('button', { name: 'Gravar' }).click()
    await expect(page.getByRole('link', { name: 'Olá, sou o teste E2E' })).toBeVisible()
  })

  await test.step('criar um projeto com repositório e imagem, e publicá-lo', async () => {
    await page.getByRole('navigation').getByRole('link', { name: 'Projetos', exact: true }).click()
    await page.getByRole('link', { name: 'Novo projeto' }).click()
    await page.getByLabel('Nome').fill('Projeto E2E')
    await page.getByLabel('Descrição').fill('Criado pelo teste de ponta a ponta.')
    await page.getByLabel('Repositório no GitHub (opcional)').fill('github.com/Miguel9669/Portef-lio-Pessoal')
    await page.getByLabel('Link oficial (opcional)').fill('https://meuapp.pt')
    await page.getByRole('button', { name: 'Criar projeto' }).click()
    await expect(page.getByText('Endereço no site: /projetos/projeto-e2e')).toBeVisible()

    await page.getByLabel('Adicionar imagens').setInputFiles({ name: 'capa.png', mimeType: 'image/png', buffer: imagemPng() })
    await expect(page.getByAltText('capa.png')).toBeVisible()

    await page.getByRole('switch', { name: 'Público no site' }).click()
    await expect(page.getByText('Público no site', { exact: true })).toBeVisible()
  })

  await test.step('criar um projeto que fica por publicar', async () => {
    await page.getByRole('navigation').getByRole('link', { name: 'Projetos', exact: true }).click()
    await page.getByRole('link', { name: 'Novo projeto' }).click()
    await page.getByLabel('Nome').fill('Projeto Secreto')
    await page.getByRole('button', { name: 'Criar projeto' }).click()
    await expect(page.getByText('Endereço no site: /projetos/projeto-secreto')).toBeVisible()
  })

  await test.step('o site mostra o bloco e só o projeto público', async () => {
    // O site guarda os dados da API em cache até 60 segundos (ISR): uma alteração no backoffice
    // aparece no máximo um minuto depois. Recarrega-se até lá, como faria um visitante.
    await expect(async () => {
      await page.goto(SITE)
      await expect(page.getByRole('heading', { level: 1 })).toHaveText('Olá, sou o teste E2E', { timeout: 2_000 })
    }).toPass({ timeout: 90_000, intervals: [2_000, 5_000, 10_000] })
    await expect(page.getByText('importante')).toBeVisible()
    await expect(page.getByRole('link', { name: /Projeto E2E/ })).toBeVisible()
    await expect(page.getByText('Projeto Secreto')).toHaveCount(0)
  })

  await test.step('a página do projeto mostra a imagem, o GitHub e o histórico', async () => {
    await page.getByRole('link', { name: /Projeto E2E/ }).click()
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Projeto E2E')
    await expect(page.getByRole('link', { name: /Ver código no GitHub/ })).toHaveAttribute('href', 'https://github.com/Miguel9669/Portef-lio-Pessoal')
    // SEG30: o site oficial abre noutro separador, sem acesso a esta página.
    const siteOficial = page.getByRole('link', { name: /Visitar site/ })
    await expect(siteOficial).toHaveAttribute('href', 'https://meuapp.pt/')
    await expect(siteOficial).toHaveAttribute('rel', 'noopener noreferrer')

    // A imagem chega mesmo ao browser, pelo rewrite do site para a API.
    const imagem = page.getByAltText('Projeto E2E: capa.png')
    await expect(imagem).toBeVisible()
    // Espera que acabe de carregar: se o carregamento falhar, a largura fica 0 e o teste falha.
    await expect.poll(() => imagem.evaluate((i: HTMLImageElement) => (i.complete ? i.naturalWidth : -1))).toBe(160)

    // Os commits vêm do GitHub real: se não responder, aparece o aviso em vez da lista.
    await expect(page.getByRole('heading', { name: 'Histórico' })).toBeVisible()
  })

  await test.step('o projeto por publicar não existe no site (SEG16)', async () => {
    const resposta = await page.goto(`${SITE}/projetos/projeto-secreto`)
    expect(resposta?.status()).toBe(404)
    await expect(page.getByRole('heading', { name: 'Página não encontrada' })).toBeVisible()
  })

  await test.step('sair do backoffice volta ao login', async () => {
    await page.goto(BACKOFFICE)
    await page.getByRole('button', { name: 'Sair' }).click()
    await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible()
    await page.goto(`${BACKOFFICE}/projetos`)
    await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible()
  })
})
