import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { createAsset, updateAsset } from '../../api/assets'
import { getErrorMessage } from '../../api/errors'
import { fetchStaff } from '../../api/staff'
import { ASSET_CONDITIONS, type AssetDto } from '../../api/types'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'
import { SelectField, TextareaField, TextField } from '../../components/ui/Field'

interface AssetFormDrawerProps {
  open: boolean
  onClose: () => void
  /** Present for edit mode; omitted for create. */
  asset?: AssetDto
}

export function AssetFormDrawer({ open, onClose, asset }: AssetFormDrawerProps) {
  const isEdit = !!asset
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [category, setCategory] = useState('')
  const [description, setDescription] = useState('')
  const [location, setLocation] = useState('')
  const [purchasePrice, setPurchasePrice] = useState('')
  const [purchaseDate, setPurchaseDate] = useState('')
  const [condition, setCondition] = useState<string>('Good')
  const [assignedToId, setAssignedToId] = useState('')
  const [isActive, setIsActive] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const { data: staff } = useQuery({
    queryKey: ['staff', { isActive: true }],
    queryFn: () => fetchStaff({ pageSize: 200, isActive: true }),
    enabled: open,
  })

  useEffect(() => {
    if (open) {
      setName(asset?.name ?? '')
      setCategory(asset?.category ?? '')
      setDescription(asset?.description ?? '')
      setLocation(asset?.location ?? '')
      setPurchasePrice(asset?.purchasePrice != null ? String(asset.purchasePrice) : '')
      setPurchaseDate(asset?.purchaseDate ? asset.purchaseDate.slice(0, 10) : '')
      setCondition(asset?.condition ?? 'Good')
      setAssignedToId(asset?.assignedToId ?? '')
      setIsActive(asset?.isActive ?? true)
      setError(null)
    }
  }, [open, asset])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const payload = {
        name,
        category,
        description: description || undefined,
        location: location || undefined,
        purchasePrice: purchasePrice ? Number(purchasePrice) : undefined,
        purchaseDate: purchaseDate || undefined,
        condition,
        assignedToId: assignedToId || undefined,
      }
      if (isEdit && asset) {
        await updateAsset({ ...payload, id: asset.id, isActive })
      } else {
        await createAsset(payload)
      }
      await queryClient.invalidateQueries({ queryKey: ['assets'] })
      onClose()
    } catch (err) {
      setError(getErrorMessage(err, isEdit ? 'Could not update asset' : 'Could not create asset'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title={isEdit ? 'Edit asset' : 'Add asset'}
      description={isEdit ? asset?.assetNumber : 'Register a new school asset'}
      footer={
        <>
          <Button variant="secondary" type="button" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" form="asset-form" loading={submitting}>
            {isEdit ? 'Save changes' : 'Add asset'}
          </Button>
        </>
      }
    >
      <form id="asset-form" onSubmit={handleSubmit} className="space-y-4">
        <TextField label="Name" required value={name} onChange={(e) => setName(e.target.value)} />
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Category"
            required
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            placeholder="Furniture, IT equipment…"
          />
          <SelectField label="Condition" required value={condition} onChange={(e) => setCondition(e.target.value)}>
            {ASSET_CONDITIONS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </SelectField>
        </div>
        <TextareaField label="Description" rows={2} value={description} onChange={(e) => setDescription(e.target.value)} />
        <div className="grid grid-cols-2 gap-4">
          <TextField label="Location" value={location} onChange={(e) => setLocation(e.target.value)} />
          <TextField
            label="Purchase price"
            type="number"
            min={0}
            step="0.01"
            value={purchasePrice}
            onChange={(e) => setPurchasePrice(e.target.value)}
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <TextField
            label="Purchase date"
            type="date"
            value={purchaseDate}
            onChange={(e) => setPurchaseDate(e.target.value)}
          />
          <SelectField label="Assigned to" value={assignedToId} onChange={(e) => setAssignedToId(e.target.value)}>
            <option value="">Unassigned</option>
            {staff?.items.map((s) => (
              <option key={s.id} value={s.id}>
                {s.fullName}
              </option>
            ))}
          </SelectField>
        </div>

        {isEdit && (
          <label className="flex items-center gap-2.5 text-sm text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
            />
            Active (in service)
          </label>
        )}

        {error && (
          <p className="rounded-lg bg-red-50 px-3 py-2.5 text-sm text-red-700 dark:bg-red-950/40 dark:text-red-300">
            {error}
          </p>
        )}
      </form>
    </Drawer>
  )
}
