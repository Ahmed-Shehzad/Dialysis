import { useQuery } from '@tanstack/react-query'
import { getAlarmsBySeverity } from '../api'

interface Props {
  from?: string
  to?: string
}

export function AlarmsBySeverityCard({ from, to }: Props) {
  const { data, error, isLoading } = useQuery({
    queryKey: ['alarms-by-severity', from, to],
    queryFn: () => getAlarmsBySeverity(from, to),
    enabled: Boolean(from ?? to),
  })

  const cardBase = 'p-5 border border-gray-200 rounded-lg bg-white shadow-sm'
  const cardError = 'p-5 border border-red-300 rounded-lg bg-red-50 shadow-sm'

  const errorMessage = error instanceof Error ? error.message : error ? String(error) : null
  if (errorMessage) return <div className={cardError}>Error: {errorMessage}</div>
  if (isLoading || !data) return <div className={cardBase}>Loading...</div>

  const entries = Object.entries(data.bySeverity)
  return (
    <div className={cardBase}>
      <h3 className="m-0 mb-4 text-base font-semibold">Alarms by Severity</h3>
      {entries.length === 0 ? (
        <p className="my-2">No alarms</p>
      ) : (
        <ul className="list-none p-0">
          {entries.map(([severity, count]) => (
            <li key={severity}>{severity}: <strong>{count}</strong></li>
          ))}
        </ul>
      )}
      <p className="text-xs text-gray-500 mt-4">{new Date(data.from).toLocaleDateString()} – {new Date(data.to).toLocaleDateString()}</p>
    </div>
  )
}
