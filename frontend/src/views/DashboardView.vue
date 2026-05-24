<template>
  <div class="space-y-6">
    <!-- Stat cards -->
    <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
      <div v-for="stat in statCards" :key="stat.label" class="card flex flex-col gap-2">
        <div class="flex items-center justify-between">
          <span class="text-gray-500 text-xs font-mono uppercase tracking-widest">{{ stat.label }}</span>
          <span class="text-lg">{{ stat.icon }}</span>
        </div>
        <p class="font-display text-3xl font-bold" :class="stat.color">
          {{ stat.loading ? '—' : stat.value }}
        </p>
        <p class="text-gray-600 text-xs font-mono">{{ stat.sub }}</p>
      </div>
    </div>

    <!-- Charts row -->
    <div class="grid grid-cols-1 xl:grid-cols-3 gap-4">
      <!-- Timeline chart -->
      <div class="card xl:col-span-2">
        <h3 class="font-display font-semibold text-white mb-4 text-sm">Cases — Last 30 Days</h3>
        <div class="h-48">
          <Line v-if="timelineData" :data="timelineData" :options="lineOpts" />
          <div v-else class="h-full flex items-center justify-center text-gray-600 text-sm font-mono">Loading...</div>
        </div>
      </div>

      <!-- Risk distribution -->
      <div class="card">
        <h3 class="font-display font-semibold text-white mb-4 text-sm">Risk Distribution</h3>
        <div class="h-48">
          <Doughnut v-if="riskData" :data="riskData" :options="doughnutOpts" />
          <div v-else class="h-full flex items-center justify-center text-gray-600 text-sm font-mono">Loading...</div>
        </div>
      </div>
    </div>

    <!-- Recent cases -->
    <div class="card">
      <div class="flex items-center justify-between mb-4">
        <h3 class="font-display font-semibold text-white text-sm">Active Cases — Highest Risk</h3>
        <RouterLink to="/cases" class="text-signal text-xs font-mono hover:underline">View all →</RouterLink>
      </div>
      <div class="space-y-2">
        <div v-if="store.loading" class="text-gray-500 text-sm font-mono">Loading cases...</div>
        <RouterLink v-for="c in topCases" :key="c.id" :to="`/cases/${c.id}`"
          class="flex items-center gap-4 px-4 py-3 rounded-lg bg-ink-900 hover:bg-ink-700 transition-colors border border-ink-600 hover:border-signal/30 group">
          <span :class="`badge-${(c.riskLevel || '').toLowerCase()}`">{{ c.riskLevel }}</span>
          <div class="flex-1 min-w-0">
            <p class="text-white text-sm font-display font-medium truncate group-hover:text-signal transition-colors">{{ c.title }}</p>
            <p class="text-gray-500 text-xs font-mono">{{ c.caseNumber }} · {{ c.category }}</p>
          </div>
          <div class="text-right flex-shrink-0">
            <p class="text-white text-sm font-mono font-bold">{{ c.riskScore.toFixed(0) }}</p>
            <p class="text-gray-500 text-xs">risk score</p>
          </div>
          <div class="w-16 bg-ink-700 rounded-full h-1.5">
            <div class="h-1.5 rounded-full transition-all"
              :style="{ width: c.riskScore + '%' }"
              :class="{
                'bg-threat-critical': c.riskLevel === 'Critical',
                'bg-threat-high': c.riskLevel === 'High',
                'bg-threat-medium': c.riskLevel === 'Medium',
                'bg-threat-low': c.riskLevel === 'Low',
              }"></div>
          </div>
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Line, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS, CategoryScale, LinearScale, PointElement,
  LineElement, ArcElement, Tooltip, Legend, Filler
} from 'chart.js'
import { useCasesStore } from '@/stores/cases'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Tooltip, Legend, Filler)

const store = useCasesStore()

onMounted(async () => {
  await Promise.all([store.fetchDashboard(), store.fetchCases()])
})

const topCases = computed(() =>
  [...store.cases].sort((a, b) => b.riskScore - a.riskScore).slice(0, 5)
)

const statCards = computed(() => [
  {
    label: 'Total Cases',
    value: store.stats?.total ?? '—',
    icon: '⊡',
    color: 'text-white',
    sub: 'all time',
    loading: !store.stats,
  },
  {
    label: 'Active',
    value: store.stats?.active ?? '—',
    icon: '◉',
    color: 'text-signal',
    sub: 'under investigation',
    loading: !store.stats,
  },
  {
    label: 'Critical',
    value: store.stats?.critical ?? '—',
    icon: '⚠',
    color: 'text-threat-critical',
    sub: 'require immediate action',
    loading: !store.stats,
  },
  {
    label: 'Flagged Txns',
    value: store.stats?.flaggedTransactions ?? '—',
    icon: '⊘',
    color: 'text-threat-high',
    sub: `€${((store.stats?.flaggedValue ?? 0) / 1000).toFixed(0)}k value`,
    loading: !store.stats,
  },
])

// Chart data
const timelineData = computed(() => {
  if (!store.timeline?.timeline) return null
  const t = store.timeline.timeline
  return {
    labels: t.map((d: any) => d.date),
    datasets: [{
      label: 'Cases',
      data: t.map((d: any) => d.count),
      borderColor: '#00e5ff',
      backgroundColor: 'rgba(0,229,255,0.08)',
      tension: 0.4,
      fill: true,
      pointRadius: 3,
      pointBackgroundColor: '#00e5ff',
    }]
  }
})

const riskData = computed(() => {
  if (!store.riskDist?.byRisk) return null
  const colorMap: Record<string, string> = {
    Critical: '#ff2d55',
    High: '#ff6b35',
    Medium: '#ffd60a',
    Low: '#34c759',
  }
  return {
    labels: store.riskDist.byRisk.map((d: any) => d.level),
    datasets: [{
      data: store.riskDist.byRisk.map((d: any) => d.count),
      backgroundColor: store.riskDist.byRisk.map((d: any) => colorMap[d.level] + '60'),
      borderColor: store.riskDist.byRisk.map((d: any) => colorMap[d.level]),
      borderWidth: 1,
    }]
  }
})

const lineOpts = {
  responsive: true, maintainAspectRatio: false,
  plugins: { legend: { display: false } },
  scales: {
    x: { grid: { color: '#1c2b4a' }, ticks: { color: '#6b7280', font: { family: 'JetBrains Mono', size: 10 } } },
    y: { grid: { color: '#1c2b4a' }, ticks: { color: '#6b7280', font: { family: 'JetBrains Mono', size: 10 } } },
  }
}

const doughnutOpts = {
  responsive: true, maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: { color: '#9ca3af', font: { family: 'JetBrains Mono', size: 10 }, padding: 12 }
    }
  }
}
</script>
