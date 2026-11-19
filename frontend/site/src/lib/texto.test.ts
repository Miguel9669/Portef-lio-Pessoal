import { describe, expect, it } from 'vitest'
import type { Commit } from './api'
import { agruparPorMes, dataCurta, resumir, tempoRelativo, textoSimples } from './texto'

const commit = (data: string, mensagem = 'm'): Commit => ({ sha: data, mensagem, autor: 'Ana', data, url: 'https://github.com/x/y/commit/1' })

describe('textoSimples', () => {
  it('tira as etiquetas e descodifica as entidades, sem nunca devolver < nem >', () => {
    expect(textoSimples('<p>Olá <strong>mundo</strong> &amp; &lt;amigos&gt;</p>\n<p>Fim</p>')).toBe('Olá mundo & ‹amigos› Fim')
  })

  it.each([
    '<scr<script>ipt>alert(1)</script>',
    '<<script>script>alert(1)<</script>/script>',
    '&lt;script&gt;alert(1)&lt;/script&gt;',
    '&amp;lt;script&amp;gt;',
    '<img src=x onerror=alert(1)',
    'texto > solto e < outro',
  ])('não deixa restos de etiquetas: %s', (html) => {
    expect(textoSimples(html)).not.toMatch(/[<>]/)
  })

  it('não deixa espaços antes da pontuação a seguir a uma etiqueta', () => {
    expect(textoSimples('<p>Sou <strong>full-stack</strong>. Ver <a href="x">site</a>!</p><ul><li>um</li><li>dois</li></ul>')).toBe('Sou full-stack. Ver site! um dois')
  })

  it('separa as palavras no fim de parágrafos, títulos, itens e quebras de linha', () => {
    expect(textoSimples('<h2>Título</h2><p>Um<br>dois<br/>três</p><ul><li>a</li><li>b</li></ul>')).toBe('Título Um dois três a b')
  })

  it('corta no máximo com reticências', () => {
    expect(textoSimples(`<p>${'a'.repeat(200)}</p>`, 10)).toBe('aaaaaaaaa…')
  })
})

describe('resumir', () => {
  it('mantém o texto simples tal como está, só junta os espaços e corta', () => {
    expect(resumir('Uso  List<T>\ne   genéricos')).toBe('Uso List<T> e genéricos')
    expect(resumir('abcdefghij', 5)).toBe('abcd…')
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
