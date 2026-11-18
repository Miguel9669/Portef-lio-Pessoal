import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ErroApi } from '@/api/cliente'

// Mostra os erros da API nos campos certos do formulário; o resto vai para a mensagem geral.
export function aplicarErrosApi<T extends FieldValues>(erro: unknown, campos: readonly Path<T>[], setError: UseFormSetError<T>): string {
  if (!(erro instanceof ErroApi)) return 'Ocorreu um erro. Tente de novo.'
  for (const [campo, mensagens] of Object.entries(erro.erros)) {
    if ((campos as readonly string[]).includes(campo)) setError(campo as Path<T>, { message: mensagens[0] })
  }
  return erro.message
}

// O mesmo formato que a API aceita (LinkGithub): só serve para avisar mais cedo;
// quem decide é a API, e a base de dados tem um CHECK (SEG29).
export const formatoGithub = /^(?:https?:\/\/)?(?:www\.)?github\.com\/[A-Za-z0-9-]{1,39}\/[A-Za-z0-9._-]{1,100}\/?$/i
