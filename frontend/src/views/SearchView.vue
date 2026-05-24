<template>
  <div class="space-y-6">
    <!-- Search bar -->
    <div class="card">
      <div class="flex gap-3">
        <div class="relative flex-1">
          <span class="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 font-mono text-sm">⊕</span>
          <input
            v-model="query"
            class="input-field pl-8"
            placeholder="Search cases, entities, descriptions... (powered by Elasticsearch)"
            @keyup.enter="doSearch"
            autofocus
          />
        </div>
        <select v-model="filterRisk" class="input-field w-36">
          <option value="">All Risks</option>
          <option v-for="r in risks" :key="r" :value="r">{{ r }}</option>
        </select>
        <select v-model="filterCategory" class="input-field w-44">
          <option value="">All Categories</option>
          <option v-for="c in categories" :key="c" :value="c">{{ c }}</option>
        </select>
        <button class="btn-primary" @click="doSearch" :disabled="!query.trim() || searching">
          {{ searching ? 'Searching...' : 'Search' }}
        </button>
      </div>

      <!-- Search tips -->
      <div v-if="!searched" class="mt-4 flex flex-wrap gap-2">
        <span class="text-gray-500 text-xs font-mono mr-1">Try:</span>
        <button v-for="tip in tips" :key="tip"
          class="text-xs font-mono text-signal/70 hover:text-signal border border-signal/20 hover:border-signal/40 px-2 py-0.5 rounded transition-colors"
          @click="query = tip; doSearch()">
          {{ tip }}
        </button>
      </div>
    </div>

    <!-- Results -->
    <div v-if="searched">
      <div v-if="searching" class="text-center py-12 text-gray-500 font-mono text-sm">Querying Elasticsearch...</div>
      <div v-else-if="!results.length" class="card text-center py-12 text-gray-500 font-mono text-sm">
        No results for "{{ lastQuery }}"
      </div>
      <div v-else>
        <p class="text-gray-500 text-xs font-mono mb-3">{{ results.length }} results for "{{ lastQuery }}"</p>
        <div class="space-y-3">
          <RouterLink v-for="r in results" :key="r.id" :to="`/cases/${r.id}`"
            class="card block hover:border-signal/30 transition-all group">
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span :class="`badge-${r.riskLevel.toLowerCase()}`">{{ r.riskLevel }}</span>
                  <span class="text-gray-500 text-xs font-mono">{{ r.caseNumber }}</span>
                  <span class="text-gray-500 text-xs font-mono">{{ r.category }}</span>
                </div>
                <h3 class="font-display font-semibold text-white group-hover:text-signal transition-colors">{{ r.title }}</h3>
                <p class="text-gray-400 text-sm mt-1 line-clamp-2">{{ r.description }}</p>
                <div v-if="r.entityNames?.length" class="flex flex-wrap gap-1 mt-2">
                  <span v-for="e in r.entityNames.slice(0, 4)" :key="e"
                    class="text-xs font-mono bg-ink-700 text-gray-400 px-2 py-0.5 rounded border border-ink-600">
                    {{ e }}
                  </span>
                  <span v-if="r.entityNames.length > 4" class="text-xs font-mono text-gray-600">+{{ r.entityNames.length - 4 }} more</span>
                </div>
              </div>
              <div class="text-right flex-shrink-0">
                <p class="font-mono font-bold text-2xl text-white">{{ Math.round(r.riskScore) }}</p>
                <p class="text-gray-500 text-xs font-mono">risk</p>
              </div>
            </div>
          </RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { searchCases } from '@/api'

const query = ref('')
const filterRisk = ref('')
const filterCategory = ref('')
const results = ref<any[]>([])
const searching = ref(false)
const searched = ref(false)
const lastQuery = ref('')

const risks = ['Low', 'Medium', 'High', 'Critical']
const categories = ['FinancialFraud', 'Cybercrime', 'Trafficking', 'TerrorismFinancing', 'MoneyLaundering', 'OrganisedCrime']
const tips = ['Baltic Holdings', 'shell company', 'crypto layering', 'ransomware', 'money laundering']

async function doSearch() {
  if (!query.value.trim()) return
  searching.value = true
  searched.value = true
  lastQuery.value = query.value
  try {
    const data = await searchCases(query.value, filterRisk.value || undefined, filterCategory.value || undefined)
    results.value = data.results
  } catch {
    results.value = []
  } finally {
    searching.value = false
  }
}
</script>
