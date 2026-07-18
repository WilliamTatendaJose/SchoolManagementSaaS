import { useQuery } from '@tanstack/react-query'
import { fetchDormitoryOccupants } from '../../api/hostel'
import type { DormitoryDto } from '../../api/types'
import { Avatar } from '../../components/ui/Avatar'
import { Button } from '../../components/ui/Button'
import { Drawer } from '../../components/ui/Drawer'

export function DormitoryOccupantsDrawer({
  dormitory,
  onClose,
}: {
  dormitory: DormitoryDto | null
  onClose: () => void
}) {
  const { data: occupants, isLoading } = useQuery({
    queryKey: ['dormitory-occupants', dormitory?.id],
    queryFn: () => fetchDormitoryOccupants(dormitory!.id),
    enabled: !!dormitory,
  })

  return (
    <Drawer
      open={!!dormitory}
      onClose={onClose}
      title={dormitory?.name ?? ''}
      description={dormitory ? `${dormitory.occupants}/${dormitory.capacity} beds occupied` : undefined}
      footer={
        <Button variant="secondary" type="button" onClick={onClose}>
          Close
        </Button>
      }
    >
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100 dark:bg-slate-800/60" />
          ))}
        </div>
      ) : occupants && occupants.length > 0 ? (
        <ul className="space-y-2">
          {occupants.map((o) => (
            <li
              key={o.studentId}
              className="flex items-center gap-3 rounded-xl border border-slate-100 p-3 dark:border-slate-800"
            >
              <Avatar name={o.fullName} size="sm" />
              <div>
                <p className="text-sm font-medium text-slate-800 dark:text-slate-100">{o.fullName}</p>
                <p className="text-xs text-slate-400">
                  {o.studentNumber}
                  {o.className ? ` · ${o.className}` : ''}
                </p>
              </div>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-slate-400">No students assigned to this dormitory yet.</p>
      )}
    </Drawer>
  )
}
