import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

const variantes = cva(
  'inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-zinc-400 disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-zinc-900 text-white hover:bg-zinc-800',
        outline: 'border border-zinc-300 bg-white hover:bg-zinc-100',
        destructive: 'bg-red-600 text-white hover:bg-red-700',
        ghost: 'hover:bg-zinc-100',
      },
      size: { default: 'h-9 px-4', sm: 'h-8 px-3', icon: 'size-8' },
    },
    defaultVariants: { variant: 'default', size: 'default' },
  },
)

export function Button({
  className,
  variant,
  size,
  asChild = false,
  ...props
}: ComponentProps<'button'> & VariantProps<typeof variantes> & { asChild?: boolean }) {
  const Componente = asChild ? Slot : 'button'
  return <Componente className={cn(variantes({ variant, size }), className)} {...props} />
}
