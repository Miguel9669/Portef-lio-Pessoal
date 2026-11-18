import type { ComponentProps } from 'react'
import { cn } from '@/lib/utils'

export function Card({ className, ...props }: ComponentProps<'div'>) {
  return <div className={cn('rounded-lg border border-zinc-200 bg-white shadow-sm', className)} {...props} />
}

export function CardHeader({ className, ...props }: ComponentProps<'div'>) {
  return <div className={cn('flex flex-col gap-1 p-6 pb-2', className)} {...props} />
}

export function CardTitle({ className, ...props }: ComponentProps<'h2'>) {
  return <h2 className={cn('text-lg font-semibold', className)} {...props} />
}

export function CardContent({ className, ...props }: ComponentProps<'div'>) {
  return <div className={cn('p-6 pt-2', className)} {...props} />
}

export function Badge({ className, ...props }: ComponentProps<'span'>) {
  return <span className={cn('inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium', className)} {...props} />
}
