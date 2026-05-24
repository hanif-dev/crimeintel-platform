<template>
  <div class="min-h-screen bg-ink-950 flex font-body">
    <!-- Sidebar -->
    <aside class="w-60 bg-ink-900 border-r border-ink-600 flex flex-col flex-shrink-0">
      <!-- Logo -->
      <div class="p-5 border-b border-ink-600">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-signal/10 border border-signal/30 flex items-center justify-center">
            <span class="text-signal text-sm">⬡</span>
          </div>
          <div>
            <p class="font-display font-semibold text-white text-sm tracking-wide">CrimeIntel</p>
            <p class="text-gray-500 text-xs font-mono">v1.0.0</p>
          </div>
        </div>
      </div>

      <!-- Nav -->
      <nav class="flex-1 p-3 space-y-0.5">
        <RouterLink v-for="item in navItems" :key="item.path" :to="item.path"
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-display transition-all group"
          :class="isActive(item.path)
            ? 'bg-signal/10 text-signal border border-signal/20'
            : 'text-gray-400 hover:text-gray-200 hover:bg-ink-700'">
          <span class="text-base">{{ item.icon }}</span>
          <span>{{ item.label }}</span>
          <span v-if="item.badge" class="ml-auto text-xs bg-threat-critical/20 text-threat-critical px-1.5 py-0.5 rounded font-mono">
            {{ item.badge }}
          </span>
        </RouterLink>
      </nav>

      <!-- Status bar -->
      <div class="p-4 border-t border-ink-600 space-y-2">
        <div v-for="svc in services" :key="svc.name" class="flex items-center gap-2 text-xs font-mono">
          <div class="w-1.5 h-1.5 rounded-full" :class="svc.ok ? 'bg-threat-low animate-pulse' : 'bg-threat-critical'"></div>
          <span class="text-gray-500">{{ svc.name }}</span>
          <span class="ml-auto" :class="svc.ok ? 'text-threat-low' : 'text-threat-critical'">{{ svc.ok ? 'UP' : 'DOWN' }}</span>
        </div>
      </div>
    </aside>

    <!-- Main -->
    <main class="flex-1 flex flex-col min-h-screen overflow-hidden">
      <!-- Top bar -->
      <header class="h-14 bg-ink-900 border-b border-ink-600 flex items-center px-6 flex-shrink-0">
        <h1 class="font-display font-semibold text-white">{{ currentPageTitle }}</h1>
        <div class="ml-auto flex items-center gap-3">
          <span class="text-xs font-mono text-gray-500">{{ now }}</span>
          <div class="w-7 h-7 rounded-full bg-signal/20 border border-signal/30 flex items-center justify-center text-xs font-mono text-signal">
            A1
          </div>
        </div>
      </header>

      <!-- Page content -->
      <div class="flex-1 overflow-y-auto p-6">
        <RouterView />
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'

const route = useRoute()

const navItems = [
  { path: '/',        icon: '◈', label: 'Dashboard' },
  { path: '/cases',   icon: '⊡', label: 'Cases' },
  { path: '/search',  icon: '⊕', label: 'Search' },
  { path: '/ingest',  icon: '⊞', label: 'Data Ingest' },
]

const pageTitles: Record<string, string> = {
  '/':        'Intelligence Dashboard',
  '/cases':   'Case Management',
  '/search':  'Search & Discovery',
  '/ingest':  'Data Ingestion',
}

const isActive = (path: string) =>
  path === '/' ? route.path === '/' : route.path.startsWith(path)

const currentPageTitle = computed(() =>
  route.path.match(/^\/cases\/\d+$/)
    ? 'Case Detail'
    : pageTitles[route.path] || 'CrimeIntel'
)

// Live clock
const now = ref('')
let clockTimer: number
onMounted(() => {
  clockTimer = setInterval(() => {
    now.value = new Date().toUTCString().split(' ').slice(1, 5).join(' ') + ' UTC'
  }, 1000) as unknown as number
})
onUnmounted(() => clearInterval(clockTimer))

// Service health
const services = ref([
  { name: 'API', ok: false },
  { name: 'ML', ok: false },
  { name: 'Search', ok: false },
])

async function checkHealth() {
  const apiUrl = ''
  const mlUrl  = '/ml'
  await Promise.allSettled([
    axios.get(`/api/health`).then(() => { services.value[0].ok = true }).catch(() => { services.value[0].ok = false }),
    axios.get(`/ml/health`).then(() => { services.value[1].ok = true }).catch(() => { services.value[1].ok = false }),
    axios.get(`${apiUrl}/api/search?q=test`).then(() => { services.value[2].ok = true }).catch(() => { services.value[2].ok = false }),
  ])
}

onMounted(checkHealth)
</script>
