import nextVitals from 'eslint-config-next/core-web-vitals'
import nextTs from 'eslint-config-next/typescript'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  ...nextVitals,
  ...nextTs,
  globalIgnores(['.next/**', '.next-e2e/**', 'out/**', 'coverage/**', 'next-env.d.ts']),
  {
    rules: {
      // SEG21/SEG29: nada de HTML injetado, exceto o do componente HtmlDaApi (já sanitizado pela API).
      'no-restricted-syntax': ['error', {
        selector: "JSXAttribute[name.name='dangerouslySetInnerHTML']",
        message: 'Use o componente HtmlDaApi, só com HTML sanitizado pela API.',
      }],
    },
  },
])
