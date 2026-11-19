import { GeistMono } from 'geist/font/mono'
import { GeistSans } from 'geist/font/sans'
import type { Metadata, Viewport } from 'next'
import type { ReactNode } from 'react'
import { enderecoSite, nomeSite } from '@/lib/api'
import './globals.css'

export const metadata: Metadata = {
  metadataBase: new URL(enderecoSite()),
  title: { default: nomeSite(), template: `%s — ${nomeSite()}` },
  openGraph: { siteName: nomeSite(), locale: 'pt_PT', type: 'website' },
}

export const viewport: Viewport = {
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: '#fafaf9' },
    { media: '(prefers-color-scheme: dark)', color: '#0c0a09' },
  ],
}

export default function Raiz({ children }: { children: ReactNode }) {
  return (
    <html lang="pt" className={`${GeistSans.variable} ${GeistMono.variable}`}>
      <body className="min-h-screen bg-stone-50 font-sans text-stone-900 antialiased selection:bg-violet-200 dark:bg-stone-950 dark:text-stone-100 dark:selection:bg-violet-800">
        {children}
      </body>
    </html>
  )
}
