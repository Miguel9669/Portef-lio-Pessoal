import type { MetadataRoute } from 'next'
import { enderecoSite } from '@/lib/api'

export default function robots(): MetadataRoute.Robots {
  // Sem bloquear /api/publico/imagens: o LinkedIn e o WhatsApp precisam das imagens para as pré-visualizações.
  return { rules: { userAgent: '*', allow: '/' }, sitemap: `${enderecoSite()}/sitemap.xml` }
}
