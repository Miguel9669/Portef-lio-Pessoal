import * as DialogPrimitive from '@radix-ui/react-dialog'
import { type ReactNode, useState } from 'react'
import { Button } from './button'

// Pede confirmação antes de uma ação destrutiva (remover um bloco, projeto ou imagem).
export function ConfirmarDialogo({
  titulo,
  descricao,
  acao = 'Remover',
  onConfirmar,
  children,
}: {
  titulo: string
  descricao: string
  acao?: string
  onConfirmar: () => void
  children: ReactNode
}) {
  const [aberto, setAberto] = useState(false)
  return (
    <DialogPrimitive.Root open={aberto} onOpenChange={setAberto}>
      <DialogPrimitive.Trigger asChild>{children}</DialogPrimitive.Trigger>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 bg-black/40" />
        <DialogPrimitive.Content className="fixed top-1/2 left-1/2 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white p-6 shadow-lg">
          <DialogPrimitive.Title className="text-lg font-semibold">{titulo}</DialogPrimitive.Title>
          <DialogPrimitive.Description className="mt-2 text-sm text-zinc-600">{descricao}</DialogPrimitive.Description>
          <div className="mt-6 flex justify-end gap-2">
            <DialogPrimitive.Close asChild>
              <Button variant="outline">Cancelar</Button>
            </DialogPrimitive.Close>
            <Button
              variant="destructive"
              onClick={() => {
                setAberto(false)
                onConfirmar()
              }}
            >
              {acao}
            </Button>
          </div>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  )
}
