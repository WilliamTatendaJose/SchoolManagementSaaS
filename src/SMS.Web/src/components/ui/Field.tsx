import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'

const fieldClasses =
  'w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 transition-shadow focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/10 disabled:bg-slate-50 disabled:text-slate-400 dark:border-slate-700 dark:bg-slate-900 dark:text-white dark:disabled:bg-slate-800/50'

function Wrapper({
  label,
  required,
  error,
  hint,
  children,
}: {
  label: string
  required?: boolean
  error?: string
  hint?: string
  children: ReactNode
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
        {label}
        {required && <span className="text-red-500"> *</span>}
      </span>
      {children}
      {error ? (
        <span className="mt-1 block text-xs text-red-600 dark:text-red-400">{error}</span>
      ) : hint ? (
        <span className="mt-1 block text-xs text-slate-400">{hint}</span>
      ) : null}
    </label>
  )
}

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string
  error?: string
  hint?: string
}

export function TextField({ label, error, hint, required, className = '', ...props }: TextFieldProps) {
  return (
    <Wrapper label={label} required={required} error={error} hint={hint}>
      <input required={required} className={`${fieldClasses} ${className}`} {...props} />
    </Wrapper>
  )
}

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string
  error?: string
  hint?: string
}

export function SelectField({ label, error, hint, required, className = '', children, ...props }: SelectFieldProps) {
  return (
    <Wrapper label={label} required={required} error={error} hint={hint}>
      <select required={required} className={`${fieldClasses} ${className}`} {...props}>
        {children}
      </select>
    </Wrapper>
  )
}

interface TextareaFieldProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label: string
  error?: string
  hint?: string
}

export function TextareaField({ label, error, hint, required, className = '', ...props }: TextareaFieldProps) {
  return (
    <Wrapper label={label} required={required} error={error} hint={hint}>
      <textarea required={required} className={`${fieldClasses} resize-none ${className}`} {...props} />
    </Wrapper>
  )
}
