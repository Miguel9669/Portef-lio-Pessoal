import { describe, expect, it } from 'vitest'
import { falha, ok, simularApi } from '@/testes/ajudantes'
import { ErroApi, pedir } from './cliente'

describe('pedir', () => {
  it('envia o token CSRF do cookie nos pedidos de escrita, e só nesses', async () => {
    document.cookie = 'XSRF-TOKEN=abc%2Fdef'
    const chamadas = simularApi({ 'GET /x': ok({ a: 1 }), 'POST /x': ok({ b: 2 }) })

    expect(await pedir('GET', '/x')).toEqual({ a: 1 })
    expect(await pedir('POST', '/x', { nome: 'n' })).toEqual({ b: 2 })

    expect(chamadas[0]!.cabecalhos['X-XSRF-TOKEN']).toBeUndefined()
    expect(chamadas[1]!.cabecalhos['X-XSRF-TOKEN']).toBe('abc/def')
    expect(chamadas[1]!.cabecalhos['Content-Type']).toBe('application/json')
    expect(chamadas[1]!.corpo).toEqual({ nome: 'n' })
  })

  it('envia FormData sem o converter para JSON', async () => {
    const chamadas = simularApi({ 'POST /envio': ok({}) })
    const formulario = new FormData()

    await pedir('POST', '/envio', formulario)

    expect(chamadas[0]!.corpo).toBe(formulario)
    expect(chamadas[0]!.cabecalhos['Content-Type']).toBeUndefined()
  })

  it('devolve undefined numa resposta 204', async () => {
    simularApi({ 'DELETE /x': ok() })
    expect(await pedir('DELETE', '/x')).toBeUndefined()
  })

  it('converte o ProblemDetails num ErroApi com os erros por campo em camelCase', async () => {
    simularApi({ 'POST /x': falha(400, 'Validação', { RepositorioGithub: ['Tem de ser do GitHub.'] }) })

    const erro = await pedir('POST', '/x', {}).catch((e: unknown) => e)

    expect(erro).toBeInstanceOf(ErroApi)
    expect((erro as ErroApi).estado).toBe(400)
    expect((erro as ErroApi).erros).toEqual({ repositorioGithub: ['Tem de ser do GitHub.'] })
    expect((erro as ErroApi).message).toBe('Corrija os campos assinalados.')
  })

  it('usa o título do ProblemDetails, ou uma mensagem pelo estado se não houver corpo', async () => {
    simularApi({ 'POST /a': falha(401, 'Email ou password incorretos'), 'GET /b': () => ({ estado: 429 }) })

    await expect(pedir('POST', '/a')).rejects.toThrow('Email ou password incorretos')
    await expect(pedir('GET', '/b')).rejects.toThrow('Demasiados pedidos')
  })
})
