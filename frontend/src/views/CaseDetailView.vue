<template>
  <div v-if="loading" class="flex items-center justify-center h-64 text-gray-500 font-mono">Loading case...</div>
  <div v-else-if="!caseData" class="flex items-center justify-center h-64 text-gray-500 font-mono">Case not found</div>

  <div v-else class="space-y-6">
    <!-- Header -->
    <div class="flex items-start justify-between gap-4">
      <div>
        <div class="flex items-center gap-3 mb-2">
          <RouterLink to="/cases" class="text-gray-500 hover:text-signal font-mono text-xs transition-colors">← Cases</RouterLink>
          <span class="text-gray-600 font-mono text-xs">{{ caseData.caseNumber }}</span>
          <span :class="`badge-${caseData.riskLevel.toLowerCase()}`">{{ caseData.riskLevel }}</span>
          <span class="text-xs font-mono text-gray-500 border border-ink-600 px-2 py-0.5 rounded">{{ caseData.status }}</span>
        </div>
        <h1 class="font-display text-2xl font-bold text-white">{{ caseData.title }}</h1>
        <p class="text-gray-400 text-sm mt-1 font-mono">{{ caseData.category }} · {{ caseData.countryCode }} · Analyst: {{ caseData.assignedAnalyst }}</p>
      </div>
      <div class="flex-shrink-0 text-center">
        <div class="w-16 h-16 rounded-full border-2 flex items-center justify-center"
          :class="{
            'border-threat-critical': caseData.riskLevel === 'Critical',
            'border-threat-high': caseData.riskLevel === 'High',
            'border-threat-medium': caseData.riskLevel === 'Medium',
            'border-threat-low': caseData.riskLevel === 'Low',
          }">
          <span class="font-mono font-bold text-xl text-white">{{ Math.round(caseData.riskScore) }}</span>
        </div>
        <p class="text-gray-500 text-xs font-mono mt-1">risk score</p>
      </div>
    </div>

    <!-- Description -->
    <div class="card">
      <h3 class="font-display font-semibold text-white text-sm mb-2">Summary</h3>
      <p class="text-gray-300 text-sm leading-relaxed">{{ caseData.description }}</p>
    </div>

    <!-- Network Graph -->
    <div class="card">
      <h3 class="font-display font-semibold text-white text-sm mb-4 flex items-center gap-2">
        <span class="text-signal">⬡</span> Entity Network
        <span class="text-gray-500 font-mono text-xs ml-2">{{ network?.nodes?.length ?? 0 }} nodes · {{ network?.edges?.length ?? 0 }} links</span>
      </h3>
      <div class="h-80 bg-ink-900 rounded-lg border border-ink-600 overflow-hidden relative">
        <NetworkGraph
          v-if="network"
          :nodes="network.nodes"
          :edges="network.edges"
        />
        <div v-else class="absolute inset-0 flex items-center justify-center text-gray-600 font-mono text-sm">Loading graph...</div>
      </div>
    </div>

    <!-- Transactions -->
    <div class="card">
      <h3 class="font-display font-semibold text-white text-sm mb-4 flex items-center gap-2">
        <span>⊘</span> Flagged Transactions
        <span class="text-gray-500 font-mono text-xs ml-2">{{ flaggedTxns.length }} of {{ caseData.transactions.length }}</span>
      </h3>
      <div v-if="!caseData.transactions.length" class="text-gray-600 text-sm font-mono">No transactions linked</div>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-xs font-mono">
          <thead>
            <tr class="border-b border-ink-600 text-gray-500">
              <th class="text-left pb-2 pr-4">Date</th>
              <th class="text-right pb-2 pr-4">Amount</th>
              <th class="text-left pb-2 pr-4">From</th>
              <th class="text-left pb-2 pr-4">To</th>
              <th class="text-right pb-2 pr-4">Fraud Score</th>
              <th class="text-left pb-2">Flag</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-ink-700">
            <tr v-for="tx in caseData.transactions" :key="tx.id" class="hover:bg-ink-700/50 transition-colors">
              <td class="py-2 pr-4 text-gray-400">{{ formatDate(tx.timestamp) }}</td>
              <td class="py-2 pr-4 text-right font-semibold" :class="tx.isFlagged ? 'text-threat-high' : 'text-white'">
                {{ formatCurrency(tx.amount, tx.currency) }}
              </td>
              <td class="py-2 pr-4 text-gray-400">{{ tx.fromCountry ?? '?' }} · {{ truncate(tx.fromAccount, 12) }}</td>
              <td class="py-2 pr-4 text-gray-400">{{ tx.toCountry ?? '?' }} · {{ truncate(tx.toAccount, 12) }}</td>
              <td class="py-2 pr-4 text-right">
                <span :class="tx.fraudScore >= 80 ? 'text-threat-critical' : tx.fraudScore >= 60 ? 'text-threat-high' : 'text-gray-400'">
                  {{ tx.fraudScore.toFixed(0) }}
                </span>
              </td>
              <td class="py-2">
                <span v-if="tx.isFlagged" class="badge-high">FLAGGED</span>
                <span v-else class="text-gray-600">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Notes -->
    <div class="card">
      <h3 class="font-display font-semibold text-white text-sm mb-4">Analyst Notes</h3>
      <div class="space-y-3 mb-4">
        <div v-if="!caseData.notes.length" class="text-gray-600 text-sm font-mono">No notes yet</div>
        <div v-for="note in caseData.notes" :key="note.id"
          class="bg-ink-900 border border-ink-600 rounded-lg p-3">
          <p class="text-gray-300 text-sm">{{ note.content }}</p>
          <p class="text-gray-600 text-xs font-mono mt-1">{{ note.author }} · {{ formatDate(note.createdAt) }}</p>
        </div>
      </div>
      <div class="flex gap-3">
        <input v-model="newNote" class="input-field flex-1" placeholder="Add analyst note..." @keyup.enter="submitNote" />
        <button class="btn-primary" @click="submitNote" :disabled="!newNote.trim()">Add</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getCase, getNetwork, addNote } from '@/api'
import NetworkGraph from '@/components/NetworkGraph.vue'
import type { Case, NetworkGraph as NetworkGraphType } from '@/types'

const route = useRoute()
const caseData = ref<Case | null>(null)
const network = ref<NetworkGraphType | null>(null)
const loading = ref(true)
const newNote = ref('')

onMounted(async () => {
  const id = Number(route.params.id)
  try {
    const [c, g] = await Promise.all([getCase(id), getNetwork(id)])
    caseData.value = c
    network.value = g
  } finally {
    loading.value = false
  }
})

const flaggedTxns = computed(() => caseData.value?.transactions.filter(t => t.isFlagged) ?? [])

async function submitNote() {
  if (!newNote.value.trim() || !caseData.value) return
  const note = await addNote(caseData.value.id, newNote.value, 'analyst.01')
  caseData.value.notes.push(note)
  newNote.value = ''
}

const formatDate = (d: string) => new Date(d).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'short' })
const formatCurrency = (n: number, c: string) => `${new Intl.NumberFormat('de-DE').format(n)} ${c}`
const truncate = (s: string | undefined, n: number) => s ? (s.length > n ? s.slice(0, n) + '…' : s) : '—'
</script>
