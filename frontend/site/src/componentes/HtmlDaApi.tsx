// HTML dos blocos, gerado e sanitizado pela API (Markdig sem HTML embutido + HtmlSanitizer, SEG21).
// É o único sítio do site onde se injeta HTML; tudo o resto, incluindo os commits, é texto (SEG29).
export function HtmlDaApi({ html, className }: { html: string; className?: string }) {
  // eslint-disable-next-line no-restricted-syntax -- HTML já sanitizado pela API (SEG21)
  return <div className={className} dangerouslySetInnerHTML={{ __html: html }} />
}
