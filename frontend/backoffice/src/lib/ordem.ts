import { arrayMove } from '@dnd-kit/sortable'

// Nova ordem dos ids depois de arrastar o item "ativo" para a posição do item "sobre".
export function novaOrdem(ids: number[], ativo: number, sobre: number | undefined): number[] | null {
  if (sobre === undefined || ativo === sobre) return null
  const de = ids.indexOf(ativo)
  const para = ids.indexOf(sobre)
  if (de < 0 || para < 0) return null
  return arrayMove(ids, de, para)
}
