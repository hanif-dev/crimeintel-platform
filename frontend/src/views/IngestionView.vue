<template>
  <div class="space-y-6">
    <!-- Pipeline status -->
    <div class="grid grid-cols-3 gap-4">
      <div v-for="svc in services" :key="svc.name" class="card flex items-center gap-3">
        <div class="w-2.5 h-2.5 rounded-full flex-shrink-0"
          :class="svc.status === 'ok' ? 'bg-threat-low animate-pulse' : svc.status === 'checking' ? 'bg-threat-medium animate-pulse' : 'bg-threat-critical'"></div>
        <div>
          <p class="font-display text-sm font-semibold text-white">{{ svc.name }}</p>
          <p class="text-gray-500 text-xs font-mono">{{ svc.url }}</p>
        </div>
        <span class="ml-auto text-xs font-mono"
          :class="svc.status === 'ok' ? 'text-threat-low' : svc.status === 'checking' ? 'text-threat-medium' : 'text-threat-critical'">
          {{ svc.status.toUpperCase() }}
        </span>
      </div>
    </div>

    <div class="grid grid-cols-1 xl:grid-cols-2 gap-6">
      <!-- CSV Upload -->
      <div class="card space-y-4">
        <h3 class="font-display font-semibold text-white text-sm">Upload Transaction CSV</h3>
        <div class="border-2 border-dashed border-ink-600 rounded-xl p-8 text-center"
          :class="dragOver ? 'border-signal bg-signal/5' : 'hover:border-ink-500'"
          @dragover.prevent="dragOver = true"
          @dragleave="dragOver = false"
          @drop.prevent="onDrop">
          <p class="text-gray-500 text-sm font-mono mb-3">⊞ Drop CSV here or click to browse</p>
          <p class="text-gray-600 text-xs font-mono mb-4">Required columns: amount, timestamp, from_account, to_account</p>
          <input ref="fileInput" type="file" accept=".csv" class="hidden" @change="onFile" />
          <button class="btn-ghost" @click="fileInput?.click()">Choose File</button>
          <p v-if="selectedFile" class="text-signal text-xs font-mono mt-3">✓ {{ selectedFile.name }}</p>
        </div>

        <div class="flex gap-3">
          <input v-model="caseId" type="number" class="input-field w-32" placeholder="Case ID" />
          <button class="btn-primary flex-1" :disabled="!selectedFile || !caseId || uploading" @click="uploadCSV">
            {{ uploading ? 'Processing...' : 'Ingest & Score' }}
          </button>
        </div>

        <!-- Result -->
        <div v-if="ingestResult" class="bg-ink-900 border border-ink-600 rounded-lg p-4 font-mono text-sm space-y-1">
          <p class="text-threat-low">✓ Processed: {{ ingestResult.processed }}</p>
          <p class="text-threat-high">⚠ Flagged: {{ ingestResult.flagged }}</p>
          <p v-if="ingestResult.failed" class="text-threat-critical">✗ Failed: {{ ingestResult.failed }}</p>
        </div>
      </div>

      <!-- Live ML Scorer -->
      <div class="card space-y-4">
        <h3 class="font-display font-semibold text-white text-sm">Live Transaction Scorer</h3>
        <div class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-gray-500 text-xs font-mono mb-1 block">Amount (EUR)</label>
              <input v-model.number="tx.amount" type="number" class="input-field" placeholder="50000" />
            </div>
            <div>
              <label class="text-gray-500 text-xs font-mono mb-1 block">Currency</label>
              <select v-model="tx.currency" class="input-field">
                <option>EUR</option><option>USD</option><option>GBP</option><option>RUB</option><option>IRR</option>
              </select>
            </div>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-gray-500 text-xs font-mono mb-1 block">From Country</label>
              <input v-model="tx.from_country" class="input-field" placeholder="DE" maxlength="2" />
            </div>
            <div>
              <label class="text-gray-500 text-xs font-mono mb-1 block">To Country</label>
              <input v-model="tx.to_country" class="input-field" placeholder="RU" maxlength="2" />
            </div>
          </div>
          <button class="btn-primary w-full" @click="scoreTransaction" :disabled="scoring">
            {{ scoring ? 'Scoring...' : 'Run ML Scoring' }}
          </button>
        </div>

        <!-- Score result -->
        <div v-if="scoreResult" class="bg-ink-900 border rounded-lg p-4 space-y-3"
          :class="{
            'border-threat-critical': scoreResult.riskLevel === 'Critical',
            'border-threat-high': scoreResult.riskLevel === 'High',
            'border-threat-medium': scoreResult.riskLevel === 'Medium',
            'border-threat-low': scoreResult.riskLevel === 'Low',
          }">
          <div class="flex items-center justify-between">
            <div>
              <span :class="`badge-${scoreResult.riskLevel.toLowerCase()}`">{{ scoreResult.riskLevel }}</span>
              <p class="text-gray-400 text-xs font-mono mt-1">{{ scoreResult.isAnomaly ? '⚠ ANOMALY DETECTED' : '✓ Normal pattern' }}</p>
            </div>
            <div class="text-right">
              <p class="font-mono font-bold text-3xl text-white">{{ scoreResult.score.toFixed(0) }}</p>
              <p class="text-gray-500 text-xs font-mono">fraud score</p>
            </div>
          </div>
          <div>
            <p class="text-gray-500 text-xs font-mono mb-2 uppercase tracking-wider">Feature Contributions</p>
            <div class="space-y-1.5">
              <div v-for="(val, key) in scoreResult.explanation" :key="key" class="flex items-center gap-2">
                <span class="text-gray-500 text-xs font-mono w-40 truncate">{{ key }}</span>
                <div class="flex-1 bg-ink-700 rounded-full h-1.5">
                  <div class="h-1.5 rounded-full bg-signal transition-all" :style="{ width: Math.min(Math.abs(val), 100) + '%' }"></div>
                </div>
                <span class="text-gray-400 text-xs font-mono w-8 text-right">{{ val.toFixed(0) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import axios from 'axios'
import { scoreTransaction as mlScore } from '@/api'

const fileInput = ref<HTMLInputElement>()
const selectedFile = ref<File | null>(null)
const dragOver = ref(false)
const caseId = ref<number | null>(null)
const uploading = ref(false)
const ingestResult = ref<any>(null)

const scoring = ref(false)
const scoreResult = ref<any>(null)

const tx = reactive({
  amount: 50000,
  currency: 'EUR',
  from_country: 'DE',
  to_country: 'RU',
  timestamp: new Date().toISOString(),
})

const services = reactive([
  { name: 'Core API (.NET)', url: 'localhost:5000', status: 'checking' },
  { name: 'Ingestion (Python)', url: 'localhost:8000', status: 'checking' },
  { name: 'ML Service (Python)', url: 'localhost:8001', status: 'checking' },
])

onMounted(async () => {
  const apiUrl = ''
  const ingUrl = '/ingest'
  const mlUrl  = '/ml'
  await Promise.allSettled([
    axios.get(`${apiUrl}/api/health`).then(() => services[0].status = 'ok').catch(() => services[0].status = 'error'),
    axios.get(`${ingUrl}/health`).then(() => services[1].status = 'ok').catch(() => services[1].status = 'error'),
    axios.get(`${mlUrl}/health`).then(() => services[2].status = 'ok').catch(() => services[2].status = 'error'),
  ])
})

function onDrop(e: DragEvent) {
  dragOver.value = false
  selectedFile.value = e.dataTransfer?.files[0] ?? null
}
function onFile(e: Event) {
  selectedFile.value = (e.target as HTMLInputElement).files?.[0] ?? null
}

async function uploadCSV() {
  if (!selectedFile.value || !caseId.value) return
  uploading.value = true
  ingestResult.value = null
  try {
    const form = new FormData()
    form.append('file', selectedFile.value)
    const ingUrl = '/ingest'
    const { data } = await axios.post(`${ingUrl}/ingest/transactions/csv?case_id=${caseId.value}`, form)
    ingestResult.value = data
  } catch (e: any) {
    ingestResult.value = { processed: 0, flagged: 0, failed: 1, errors: [e.message] }
  } finally {
    uploading.value = false
  }
}

async function scoreTransaction() {
  scoring.value = true
  scoreResult.value = null
  try {
    scoreResult.value = await mlScore({ ...tx })
  } finally {
    scoring.value = false
  }
}
</script>
