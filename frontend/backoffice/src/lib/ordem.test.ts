import { describe, expect, it } from 'vitest'
import { aplicarErrosApi, formatoGithub, linkOficialValido } from './formularios'
import { novaOrdem } from './ordem'
import { ErroApi } from '@/api/cliente'

describe('novaOrdem', () => {
  it('move o item arrastado para a posição do outro', () => {
    expect(novaOrdem([1, 2, 3], 3, 1)).toEqual([3, 1, 2])
    expect(novaOrdem([1, 2, 3], 1, 3)).toEqual([2, 3, 1])
  })

  it('não muda nada se largar fora, no mesmo sítio ou num id desconhecido', () => {
    expect(novaOrdem([1, 2, 3], 1, undefined)).toBeNull()
    expect(novaOrdem([1, 2, 3], 2, 2)).toBeNull()
    expect(novaOrdem([1, 2, 3], 9, 1)).toBeNull()
  })
})

describe('formatoGithub', () => {
  it.each(['https://github.com/dono/repo', 'github.com/dono/repo.git', 'https://www.github.com/dono/repo/'])('aceita %s', (link) => {
    expect(formatoGithub.test(link)).toBe(true)
  })

  it.each(['https://gitlab.com/dono/repo', 'https://github.com.evil.example/dono/repo', 'https://github.com/dono', 'javascript:alert(1)'])(
    'recusa %s',
    (link) => {
      expect(formatoGithub.test(link)).toBe(false)
    },
  )
})

describe('linkOficialValido', () => {
  it.each(['https://meuapp.pt', 'https://www.meuapp.pt/produto?x=1'])('aceita %s', (link) => {
    expect(linkOficialValido(link)).toBe(true)
  })

  it.each(['http://meuapp.pt', 'meuapp.pt', 'javascript:alert(1)', 'https://banco.pt@evil.example', 'https://localhost'])('recusa %s', (link) => {
    expect(linkOficialValido(link)).toBe(false)
  })
})

describe('aplicarErrosApi', () => {
  it('põe os erros conhecidos nos campos e devolve a mensagem geral', () => {
    const campos: string[] = []
    const mensagem = aplicarErrosApi(new ErroApi(400, 'Corrija os campos assinalados.', { nome: ['Obrigatório'], outro: ['x'] }), ['nome'], (c) => campos.push(c))

    expect(campos).toEqual(['nome'])
    expect(mensagem).toBe('Corrija os campos assinalados.')
    expect(aplicarErrosApi(new Error('rede'), ['nome'], () => {})).toBe('Ocorreu um erro. Tente de novo.')
  })
})
