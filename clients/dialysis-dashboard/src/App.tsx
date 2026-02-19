import { useState } from 'react'
import { SessionsSummaryCard } from './components/SessionsSummaryCard'
import { AlarmsBySeverityCard } from './components/AlarmsBySeverityCard'
import { PrescriptionComplianceCard } from './components/PrescriptionComplianceCard'

function App() {
  const weekAgo = new Date()
  weekAgo.setDate(weekAgo.getDate() - 7)
  const [from] = useState(weekAgo.toISOString())
  const [to] = useState(new Date().toISOString())

  return (
    <div className="max-w-[960px] mx-auto p-8 font-sans">
      <header className="mb-8">
        <h1 className="m-0 mb-2 text-[1.75rem]">Dialysis PDMS Dashboard</h1>
        <p className="m-0 text-gray-600 text-sm">Reports from PDMS Gateway (ensure services are running)</p>
      </header>
      <main className="grid gap-6 grid-cols-[repeat(auto-fill,minmax(280px,1fr))]">
        <SessionsSummaryCard from={from} to={to} />
        <AlarmsBySeverityCard from={from} to={to} />
        <PrescriptionComplianceCard from={from} to={to} />
      </main>
    </div>
  )
}

export default App
