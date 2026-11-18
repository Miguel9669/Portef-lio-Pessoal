import { DndContext, type DragEndEvent, KeyboardSensor, PointerSensor, closestCenter, useSensor, useSensors } from '@dnd-kit/core'
import { SortableContext, sortableKeyboardCoordinates, useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { GripVertical } from 'lucide-react'
import type { ReactNode } from 'react'
import { novaOrdem } from '@/lib/ordem'

// Lista que se reordena a arrastar (rato, toque ou teclado), com dnd-kit.
export function ListaOrdenavel({
  ids,
  onReordenar,
  estrategia,
  children,
}: {
  ids: number[]
  onReordenar: (ids: number[]) => void
  estrategia?: Parameters<typeof SortableContext>[0]['strategy']
  children: ReactNode
}) {
  const sensores = useSensors(useSensor(PointerSensor), useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }))

  function aoLargar({ active, over }: DragEndEvent) {
    const ordem = novaOrdem(ids, Number(active.id), over ? Number(over.id) : undefined)
    if (ordem) onReordenar(ordem)
  }

  return (
    <DndContext sensors={sensores} collisionDetection={closestCenter} onDragEnd={aoLargar}>
      <SortableContext items={ids} strategy={estrategia}>{children}</SortableContext>
    </DndContext>
  )
}

export function ItemOrdenavel({ id, rotulo, children, className }: { id: number; rotulo: string; children: ReactNode; className?: string }) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id })
  return (
    <li ref={setNodeRef} style={{ transform: CSS.Transform.toString(transform), transition }} className={className}>
      <button type="button" className="cursor-grab touch-none text-zinc-400 hover:text-zinc-700" aria-label={`Arrastar ${rotulo}`} {...attributes} {...listeners}>
        <GripVertical className="size-4" />
      </button>
      {children}
    </li>
  )
}
