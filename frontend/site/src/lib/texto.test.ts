import { describe, expect, it } from 'vitest'
import type { Commit } from './api'
import { agruparPorMes, dataCurta, tempoRelativo, textoSimples } from './texto'

const commit = (data: string, mensagem = 'm'): Commit => ({ sha: data, mensagem, autor: 'Ana', data, url: 'https://github.com/x/y/commit/1' })

describe('textoSimples', () => {
  it('tira as etiquetas e descodifica as entidades', () => {
    expect(textoSimples('<p>Olá <strong>mundo</strong> &amp; &lt;amigos&gt;</p>\n<p>Fim</p>')).toBe('Olá mundo & <amigos> Fim')
  })

  it('não deixa espaços antes da pontuação a seguir a uma etiqueta', () => {
    expect(textoSimples('<p>Sou <strong>full-stack</strong>. Ver <a href="x">site</a>!</p><ul><li>um</li><li>dois</li></ul>')).toBe('Sou full-stack. Ver site! um dois')
  })

  it('corta no máximo com reticências', () => {
    expect(textoSimples(`<p>${'a'.repeat(200)}</p>`, 10)).toBe('aaaaaaaaa…')
  })
})

describe('agruparPorMes', () => {
  it('junta os commits seguidos do mesmo mês, mantendo a ordem', () => {
    const grupos = agruparPorMes([commit('2026-11-18T10:00:00Z', 'a'), commit('2026-11-02T10:00:00Z', 'b'), commit('2026-10-30T10:00:00Z', 'c')])

    expect(grupos.map((g) => [g.mes, g.commits.map((c) => c.mensagem)])).toEqual([
      ['novembro de 2026', ['a', 'b']],
      ['outubro de 2026', ['c']],
    ])
  })
})

describe('datas', () => {
  const agora = new Date('2026-11-19T12:00:00Z')

  it.each([
    ['2026-11-19T11:59:30Z', 'agora mesmo'],
    ['2026-11-19T10:00:00Z', 'há 2 horas'],
    ['2026-11-18T12:00:00Z', 'ontem'],
    ['2026-11-12T12:00:00Z', 'semana passada'],
    ['2025-11-19T12:00:00Z', 'ano passado'],
  ])('%s fica "%s"', (data, esperado) => {
    expect(tempoRelativo(data, agora)).toBe(esperado)
  })

  it('formata a data curta em português', () => {
    expect(dataCurta('2026-11-19T12:00:00Z')).toBe('19/11/2026')
  })
})
