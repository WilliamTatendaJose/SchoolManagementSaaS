import { useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, BookOpen, BookPlus, Library as LibraryIcon, Plus } from 'lucide-react'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { returnBook, fetchBooks, fetchOverdueLoans } from '../../api/library'
import { getErrorMessage } from '../../api/errors'
import type { BookDto } from '../../api/types'
import { useAuthStore } from '../../auth/authStore'
import { Badge } from '../../components/ui/Badge'
import { Button } from '../../components/ui/Button'
import { EmptyState } from '../../components/ui/EmptyState'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchInput } from '../../components/ui/SearchInput'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { BorrowBookDrawer } from './BorrowBookDrawer'
import { CreateBookDrawer } from './CreateBookDrawer'

export function LibraryPage() {
  const queryClient = useQueryClient()
  const canManage = useAuthStore((s) => s.hasPermission('library.manage'))
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'overdue' ? 'overdue' : 'books'
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search, 300)
  const [createBookOpen, setCreateBookOpen] = useState(false)
  const [borrowingBook, setBorrowingBook] = useState<BookDto | null>(null)
  const [returnError, setReturnError] = useState<string | null>(null)
  const [returningId, setReturningId] = useState<string | null>(null)

  function setTab(next: 'books' | 'overdue') {
    const p = new URLSearchParams(params)
    if (next === 'books') p.delete('tab')
    else p.set('tab', next)
    setParams(p, { replace: true })
  }

  const { data: books, isLoading: loadingBooks } = useQuery({
    queryKey: ['books', debouncedSearch],
    queryFn: () => fetchBooks(debouncedSearch || undefined),
    enabled: tab === 'books',
  })

  const { data: overdue, isLoading: loadingOverdue } = useQuery({
    queryKey: ['overdue-loans'],
    queryFn: fetchOverdueLoans,
    enabled: tab === 'overdue',
  })

  async function handleReturn(loanId: string, lost: boolean) {
    setReturnError(null)
    setReturningId(loanId)
    try {
      await returnBook({ loanId, lost })
      await queryClient.invalidateQueries({ queryKey: ['overdue-loans'] })
      await queryClient.invalidateQueries({ queryKey: ['books'] })
    } catch (err) {
      setReturnError(getErrorMessage(err, 'Could not return book'))
    } finally {
      setReturningId(null)
    }
  }

  return (
    <div>
      <PageHeader
        title="Library"
        description="Books, copies and loans"
        actions={
          tab === 'books' &&
          canManage && (
            <Button onClick={() => setCreateBookOpen(true)}>
              <Plus className="h-4 w-4" strokeWidth={2.5} />
              Add book
            </Button>
          )
        }
      />

      <div className="mb-4 flex gap-1 border-b border-slate-200 dark:border-slate-800">
        <button
          onClick={() => setTab('books')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'books'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <LibraryIcon className="h-4 w-4" strokeWidth={2} />
          Books
        </button>
        <button
          onClick={() => setTab('overdue')}
          className={`flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'overdue'
              ? 'border-brand-600 text-brand-700 dark:text-brand-400'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          <AlertTriangle className="h-4 w-4" strokeWidth={2} />
          Overdue loans
        </button>
      </div>

      {tab === 'books' ? (
        <>
          <SearchInput
            placeholder="Search by title or author…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="mb-4 sm:max-w-xs"
          />
          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingBooks ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : books && books.length > 0 ? (
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-3 font-medium">Title</th>
                    <th className="px-4 py-3 font-medium">Author</th>
                    <th className="px-4 py-3 font-medium">Category</th>
                    <th className="px-4 py-3 font-medium text-right">Availability</th>
                    {canManage && <th className="w-28 px-4 py-3" />}
                  </tr>
                </thead>
                <tbody>
                  {books.map((b) => (
                    <tr key={b.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                      <td className="px-4 py-3">
                        <p className="font-medium text-slate-900 dark:text-white">{b.title}</p>
                        {b.isbn && <p className="text-xs text-slate-400">ISBN {b.isbn}</p>}
                      </td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{b.author}</td>
                      <td className="px-4 py-3">{b.category ? <Badge tone="slate">{b.category}</Badge> : '—'}</td>
                      <td className="px-4 py-3 text-right">
                        <span
                          className={`font-medium ${b.availableCopies > 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-red-600 dark:text-red-400'}`}
                        >
                          {b.availableCopies}/{b.totalCopies}
                        </span>
                        <p className="text-xs text-slate-400">{b.onLoan} on loan</p>
                      </td>
                      {canManage && (
                        <td className="px-4 py-3 text-right">
                          <button
                            onClick={() => setBorrowingBook(b)}
                            disabled={b.availableCopies === 0}
                            className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline disabled:cursor-not-allowed disabled:opacity-40 dark:text-brand-400"
                          >
                            <BookPlus className="h-3.5 w-3.5" strokeWidth={2} />
                            Borrow
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <EmptyState
                icon={BookOpen}
                title="No books found"
                description={search ? 'Try a different search.' : 'Add a book to start lending.'}
                action={
                  canManage &&
                  !search && (
                    <Button onClick={() => setCreateBookOpen(true)}>
                      <Plus className="h-4 w-4" strokeWidth={2.5} />
                      Add book
                    </Button>
                  )
                }
              />
            )}
          </div>
        </>
      ) : (
        <>
          {returnError && (
            <p className="mb-4 rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
              {returnError}
            </p>
          )}
          <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
            {loadingOverdue ? (
              <div className="space-y-3 p-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
                ))}
              </div>
            ) : overdue && overdue.length > 0 ? (
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs uppercase tracking-wider text-slate-400 dark:border-slate-800">
                    <th className="px-4 py-3 font-medium">Book</th>
                    <th className="px-4 py-3 font-medium">Student</th>
                    <th className="px-4 py-3 font-medium">Borrowed</th>
                    <th className="px-4 py-3 font-medium">Due</th>
                    {canManage && <th className="w-40 px-4 py-3" />}
                  </tr>
                </thead>
                <tbody>
                  {overdue.map((l) => (
                    <tr key={l.id} className="border-b border-slate-100 last:border-0 dark:border-slate-800/60">
                      <td className="px-4 py-3 font-medium text-slate-900 dark:text-white">{l.bookTitle}</td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">{l.studentName}</td>
                      <td className="px-4 py-3 text-slate-600 dark:text-slate-300">
                        {new Date(l.borrowedDate).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3">
                        <Badge tone="amber">{new Date(l.dueDate).toLocaleDateString()}</Badge>
                      </td>
                      {canManage && (
                        <td className="px-4 py-3">
                          <div className="flex items-center justify-end gap-3">
                            <button
                              onClick={() => handleReturn(l.id, false)}
                              disabled={returningId === l.id}
                              className="text-xs font-medium text-emerald-600 hover:underline disabled:opacity-50 dark:text-emerald-400"
                            >
                              Returned
                            </button>
                            <button
                              onClick={() => handleReturn(l.id, true)}
                              disabled={returningId === l.id}
                              className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50 dark:text-red-400"
                            >
                              Lost
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : (
              <EmptyState icon={AlertTriangle} title="No overdue loans" description="All borrowed books are within their due date." />
            )}
          </div>
        </>
      )}

      <CreateBookDrawer open={createBookOpen} onClose={() => setCreateBookOpen(false)} />
      <BorrowBookDrawer book={borrowingBook} onClose={() => setBorrowingBook(null)} />
    </div>
  )
}
