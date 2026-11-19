import type { MetadataRoute } from 'next'
import { api, enderecoSite, ouIndisponivel } from '@/lib/api'

export const revalidate = 3600

// Para os motores de busca: a página inicial e os projetos públicos.
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const projetos = await ouIndisponivel(api.projetos())
  const base = enderecoSite()
  return [
    { url: `${base}/`, changeFrequency: 'weekly', priority: 1 },
    ...(projetos === 'indisponivel' ? [] : projetos).map((p) => ({ url: `${base}/projetos/${p.slug}`, changeFrequency: 'weekly' as const, priority: 0.8 })),
  ]
}
