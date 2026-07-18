import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createBook } from '../../api/library'
import { getErrorMessage } from '../../api/errors'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { TextField } from '../../components/ui/Field'

export function CreateBookDrawer({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [title, setTitle] = useState('')
  const [author, setAuthor] = useState('')
  const [isbn, setIsbn] = useState('')
  const [category, setCategory] = useState('')
  const [totalCopies, setTotalCopies] = useState('1')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (open) {
      setTitle('')
      setAuthor('')
      setIsbn('')
      setCategory('')
      setTotalCopies('1')
      setError(null)
    }
  }, [open])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await createBook({
        title,
        author,
        isbn: isbn || undefined,
        category: category || undefined,
        totalCopies: Number(totalCopies),
      })
      await queryClient.invalidateQueries({ queryKey: ['books'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, 'Could not add book'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Add book"
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="create-book-form" loading={submitting}>
            Add book
          </Button>
        </>
      }
    >
      <form id="create-book-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Title" required value={title} onChange={(e) => setTitle(e.target.value)} />
        <TextField label="Author" required value={author} onChange={(e) => setAuthor(e.target.value)} />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="ISBN" value={isbn} onChange={(e) => setIsbn(e.target.value)} />
          <TextField label="Category" value={category} onChange={(e) => setCategory(e.target.value)} />
        </div>
        <TextField
          label="Total copies"
          type="number"
          min={1}
          required
          value={totalCopies}
          onChange={(e) => setTotalCopies(e.target.value)}
        />

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
