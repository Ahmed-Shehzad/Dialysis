import { useQuery } from '@tanstack/react-query'
import { getPrescriptionCompliance } from '../api'

interface Props {
  from?: string
  to?: string
}

export function PrescriptionComplianceCard({ from, to }: Props) {
  const { data, error, isLoading } = useQuery({
    queryKey: ['prescription-compliance', from, to],
    queryFn: () => getPrescriptionCompliance(from, to),
    enabled: Boolean(from ?? to),
  })

  const cardBase = 'p-5 border border-gray-200 rounded-lg bg-white shadow-sm'
  const cardError = 'p-5 border border-red-300 rounded-lg bg-red-50 shadow-sm'

  const errorMessage = error instanceof Error ? error.message : error ? String(error) : null
  if (errorMessage) return <div className={cardError}>Error: {errorMessage}</div>
  if (isLoading || !data) return <div className={cardBase}>Loading...</div>

  return (
    <div className={cardBase}>
      <h3 className="m-0 mb-4 text-base font-semibold">Prescription Compliance</h3>
      <p className="my-2"><strong>{data.compliancePercent}%</strong> compliant</p>
      <p className="my-2">{data.compliantCount} / {data.totalEvaluated} sessions</p>
      <p className="text-xs text-gray-500 mt-4">{new Date(data.from).toLocaleDateString()} – {new Date(data.to).toLocaleDateString()}</p>
    </div>
  )
}
