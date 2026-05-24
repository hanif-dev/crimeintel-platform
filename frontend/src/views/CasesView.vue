<template>
  <div class="space-y-4">
    <!-- Filters -->
    <div class="flex flex-wrap gap-3 items-center">
      <select v-model="store.selectedStatus" @change="store.fetchCases()" class="input-field w-40">
        <option value="">All Statuses</option>
        <option v-for="s in statuses" :key="s" :value="s">{{ s }}</option>
      </select>
      <select v-model="store.selectedCategory" @change="store.fetchCases()" class="input-field w-48">
        <option value="">All Categories</option>
        <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
      </select>
      <span class="text-gray-500 text-xs font-mono ml-auto">{{ store.cases.length }} results</span>
    </div>

    <!-- Table -->
    <div class="card p-0 overflow-hidden">
      <div v-if="store.loading" class="p-8 text-center text-gray-500 font-mono text-sm">Loading cases...</div>
      <div v-else-if="!store.cases.length" class="p-8 text-center text-gray-600 font-mono text-sm">No cases match filters</div>
      <table v-else class="w-full">
        <thead class="border-b border-ink-600">
          <tr class="text-gray-500 text-xs font-mono uppercase tracking-wider">
            <th class="text-left px-5 py-3">Risk</th>
            <th class="text-left px-5 py-3">Case</th>
            <th class="text-left px-5 py-3">Category</th>
            <th class="text-left px-5 py-3">Status</th>
            <th class="text-right px-5 py-3">Score</th>
            <th class="text-left px-5 py-3">Country</th>
            <th class="text-left px-5 py-3">Updated</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-ink-700">
          <tr v-for="c in store.cases" :key="c.id"
            @click="router.push(`/cases/${c.id}`)"
            class="hover:bg-ink-700/40 cursor-pointer transition-colors group">
            <td class="px-5 py-3">
              <span :class="`badge-${c.riskLevel.toLowerCase()}`">{{ c.riskLevel }}</span>
            </td>
            <td class="px-5 py-3">
              <p class="text-white text-sm font-display font-medium group-hover:text-signal transition-colors">{{ c.title }}</p>
              <p class="text-gray-500 text-xs font-mono">{{ c.caseNumber }}</p>
            </td>
            <td class="px-5 py-3 text-gray-400 text-xs font-mono">{{ c.category }}</td>
            <td class="px-5 py-3">
              <span class="text-xs font-mono border border-ink-600 px-2 py-0.5 rounded text-gray-400">{{ c.status }}</span>
            </td>
            <td class="px-5 py-3 text-right font-mono font-bold text-sm" :class="{
              'text-threat-critical': c.riskLevel === 'Critical',
              'text-threat-high': c.riskLevel === 'High',
              'text-threat-medium': c.riskLevel === 'Medium',
              'text-threat-low': c.riskLevel === 'Low',
            }">{{ c.riskScore.toFixed(1) }}</td>
            <td class="px-5 py-3 text-gray-400 text-xs font-mono">{{ c.countryCode ?? '—' }}</td>
            <td class="px-5 py-3 text-gray-500 text-xs font-mono">{{ formatDate(c.updatedAt) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useCasesStore } from '@/stores/cases'

const store = useCasesStore()
const router = useRouter()
onMounted(() => store.fetchCases())

const statuses = ['Open', 'Active', 'UnderReview', 'Closed', 'Escalated']
const categories = ['FinancialFraud', 'Cybercrime', 'Trafficking', 'TerrorismFinancing', 'MoneyLaundering', 'OrganisedCrime']

const formatDate = (d: string) => new Date(d).toLocaleDateString('en-GB')
</script>
