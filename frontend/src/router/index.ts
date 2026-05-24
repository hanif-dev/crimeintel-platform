import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'
import CasesView from '@/views/CasesView.vue'
import CaseDetailView from '@/views/CaseDetailView.vue'
import SearchView from '@/views/SearchView.vue'
import IngestionView from '@/views/IngestionView.vue'

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/',          name: 'dashboard',   component: DashboardView },
    { path: '/cases',     name: 'cases',       component: CasesView },
    { path: '/cases/:id', name: 'case-detail', component: CaseDetailView },
    { path: '/search',    name: 'search',      component: SearchView },
    { path: '/ingest',    name: 'ingest',      component: IngestionView },
  ]
})
