import type { Bloco, Projeto, Timeline } from '@/lib/api'

export const blocos: Bloco[] = [
  { titulo: 'Olá, sou o Miguel', slug: 'ola-sou-o-miguel', html: '<p>Desenvolvedor <strong>full-stack</strong>.</p>' },
  { titulo: 'Sobre mim', slug: 'sobre-mim', html: '<p>Gosto de construir coisas.</p>' },
  { titulo: 'Contactos', slug: 'contactos', html: '<p><a href="mailto:ola@exemplo.pt">Email</a></p>' },
]

export const projeto = (dados: Partial<Projeto> = {}): Projeto => ({
  nome: 'Portfólio Pessoal',
  slug: 'portfolio-pessoal',
  descricao: 'Um portfólio com backoffice e 2FA.',
  repositorioGithub: 'https://github.com/dono/repo',
  criadoEm: '2026-11-18T10:00:00Z',
  imagens: [
    { url: '/api/publico/imagens/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.png', nomeOriginal: 'inicio.png' },
    { url: '/api/publico/imagens/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.png', nomeOriginal: 'backoffice.png' },
  ],
  ...dados,
})

export const timeline = (dados: Partial<Timeline> = {}): Timeline => ({
  estado: 'atual',
  atualizadoEm: '2026-11-19T10:00:00Z',
  commits: [
    { sha: 'a'.repeat(40), mensagem: 'Fase 7: site público', autor: 'Miguel', data: '2026-11-19T09:00:00Z', url: `https://github.com/dono/repo/commit/${'a'.repeat(40)}` },
    { sha: 'b'.repeat(40), mensagem: 'docs: requisitos', autor: 'Miguel', data: '2026-10-17T09:00:00Z', url: `https://github.com/dono/repo/commit/${'b'.repeat(40)}` },
  ],
  ...dados,
})
