import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { getCases, getDashboardStats, getRiskDistribution, getTimeline } from '@/api'
import type { Case, DashboardStats } from '@/types'

export const useCasesStore = defineStore('cases', () => {
  const cases = ref<Case[]>([])
  const stats = ref<DashboardStats | null>(null)
  const riskDist = ref<any>(null)
  const timeline = ref<any>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const selectedStatus = ref<string>('')
  const selectedCategory = ref<string>('')

  const filteredCases = computed(() => cases.value)

  async function fetchCases() {
    loading.value = true
    error.value = null
    try {
      cases.value = await getCases({
        status: selectedStatus.value || undefined,
        category: selectedCategory.value || undefined,
      })
    } catch (e: any) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchDashboard() {
    try {
      const [s, r, t] = await Promise.all([
        getDashboardStats(),
        getRiskDistribution(),
        getTimeline(30),
      ])
      stats.value = s
      riskDist.value = r
      timeline.value = t
    } catch (e: any) {
      error.value = e.message
    }
  }

  return {
    cases, stats, riskDist, timeline,
    loading, error,
    selectedStatus, selectedCategory,
    filteredCases,
    fetchCases, fetchDashboard,
  }
})
