import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { abrir, falha, ok, sessao, simularApi } from '@/testes/ajudantes'

const painel = { 'GET /api/backoffice/blocos': ok([]), 'GET /api/backoffice/projetos': ok([]) }

describe('login', () => {
  it('mostra a mensagem genérica da API quando as credenciais estão erradas', async () => {
    simularApi({ 'POST /api/backoffice/auth/login': falha(401, 'Email ou password incorretos') })
    abrir('/login')

    await userEvent.type(screen.getByLabelText('Email'), 'ana@exemplo.pt')
    await userEvent.type(screen.getByLabelText('Password'), 'errada')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Email ou password incorretos')
  })

  it('valida os campos antes de enviar', async () => {
    const chamadas = simularApi({})
    abrir('/login')

    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByText('Indique o email.')).toBeInTheDocument()
    expect(screen.getByText('Indique a password.')).toBeInTheDocument()
    expect(chamadas).toHaveLength(0)
  })

  it('com o 2FA ativo pede o código e depois abre o backoffice', async () => {
    const chamadas = simularApi({
      'POST /api/backoffice/auth/login': ok({ proximoPasso: 'codigo' }),
      'POST /api/backoffice/auth/2fa': ok({ nome: 'Ana', email: 'ana@exemplo.pt' }),
      ...sessao,
      ...painel,
    })
    abrir('/login')

    await userEvent.type(screen.getByLabelText('Email'), 'ana@exemplo.pt')
    await userEvent.type(screen.getByLabelText('Password'), 'uma-password-comprida')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
    await userEvent.type(await screen.findByLabelText('Código'), '123456')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(await screen.findByText('Ana')).toBeInTheDocument()
    expect(chamadas.find((c) => c.url.endsWith('/2fa'))?.corpo).toEqual({ codigo: '123456' })
  })

  it('no primeiro login configura o 2FA e mostra os códigos de recuperação uma vez', async () => {
    simularApi({
      'POST /api/backoffice/auth/login': ok({ proximoPasso: 'configurar-2fa' }),
      'POST /api/backoffice/auth/2fa/configurar': ok({ segredo: 'JBSWY3DPEHPK3PXP', uri: 'otpauth://totp/x', qrCode: 'data:image/png;base64,AAAA' }),
      'POST /api/backoffice/auth/2fa/ativar': ok({ codigosRecuperacao: ['AAAA-BBBB-CCCC-DDDD', 'EEEE-FFFF-GGGG-HHHH'] }),
      ...sessao,
      ...painel,
    })
    abrir('/login')

    await userEvent.type(screen.getByLabelText('Email'), 'ana@exemplo.pt')
    await userEvent.type(screen.getByLabelText('Password'), 'uma-password-comprida')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByAltText('QR code para a app autenticadora')).toHaveAttribute('src', 'data:image/png;base64,AAAA')
    expect(screen.getByText('JBSWY3DPEHPK3PXP')).toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('Código da app'), '123456')
    await userEvent.click(screen.getByRole('button', { name: 'Ativar' }))
    expect(await screen.findByText('AAAA-BBBB-CCCC-DDDD')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Já guardei os códigos' }))

    expect(await screen.findByText('Ana')).toBeInTheDocument()
  })

  it('mostra o erro de um código de ativação errado', async () => {
    simularApi({
      'POST /api/backoffice/auth/2fa/configurar': ok({ segredo: 'S', uri: 'u', qrCode: 'data:image/png;base64,AAAA' }),
      'POST /api/backoffice/auth/2fa/ativar': falha(401, 'Código inválido'),
    })
    abrir('/configurar-2fa')

    await userEvent.type(await screen.findByLabelText('Código da app'), '000000')
    await userEvent.click(screen.getByRole('button', { name: 'Ativar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Código inválido')
  })

  it('sem sessão parcial, a configuração do 2FA volta ao login', async () => {
    simularApi({ 'POST /api/backoffice/auth/2fa/configurar': () => ({ estado: 403 }) })
    abrir('/configurar-2fa')

    expect(await screen.findByText('Entre de novo para configurar o 2FA.')).toBeInTheDocument()
  })

  it('um código errado mostra o erro e deixa tentar de novo', async () => {
    simularApi({ 'POST /api/backoffice/auth/2fa': falha(401, 'Código inválido') })
    abrir('/login/2fa')

    await userEvent.type(screen.getByLabelText('Código'), '000000')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Código inválido')
    expect(screen.getByLabelText('Código')).toBeInTheDocument()
  })

  it.each([
    ['Demasiadas tentativas. Volte a introduzir a password.', 'Demasiadas tentativas. Volte a introduzir a password.'],
    [undefined, 'O tempo para introduzir o código terminou. Entre de novo.'],
  ])('ao fim das tentativas ou com a sessão parcial expirada, recomeça pela password', async (titulo, aviso) => {
    simularApi({ 'POST /api/backoffice/auth/2fa': titulo ? falha(401, titulo) : () => ({ estado: 401 }) })
    abrir('/login/2fa')

    await userEvent.type(screen.getByLabelText('Código'), '000000')
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))

    expect(await screen.findByText(aviso)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeInTheDocument()
  })
})

describe('rotas protegidas', () => {
  it('sem sessão vão para o login', async () => {
    simularApi({ 'GET /api/backoffice/auth/eu': () => ({ estado: 401 }) })
    abrir('/projetos')

    expect(await screen.findByRole('button', { name: 'Entrar' })).toBeInTheDocument()
  })

  it('outro erro da API é mostrado', async () => {
    simularApi({ 'GET /api/backoffice/auth/eu': falha(500, 'Erro interno') })
    abrir('/')

    expect(await screen.findByRole('alert')).toHaveTextContent('Erro interno')
  })

  it('o painel mostra os totais e sair termina a sessão', async () => {
    const chamadas = simularApi({
      ...sessao,
      'GET /api/backoffice/blocos': ok([{ id: 1, titulo: 'A', slug: 'a', conteudo: '', ordem: 0, publico: true }]),
      'GET /api/backoffice/projetos': ok([]),
      'POST /api/backoffice/auth/logout': ok(),
    })
    abrir('/')

    expect(await screen.findByText('1 blocos, 1 visíveis')).toBeInTheDocument()
    expect(await screen.findByText('0 projetos, 0 públicos')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Sair' }))

    expect(await screen.findByRole('button', { name: 'Entrar' })).toBeInTheDocument()
    await waitFor(() => expect(chamadas.some((c) => c.url.endsWith('/logout'))).toBe(true))
  })
})
