import axios from 'axios'
import type { Case, DashboardStats, NetworkGraph } from '@/types'

// Pakai relative URL — semua lewat Vite proxy di port 3000
// Tidak perlu VITE_API_URL, tidak ada CORS
const api = axios.create({ timeout: 10_000 })

const mlApi = axios.create({
  baseURL: '/ml',
  timeout: 10_000,
})

export const getCases = (params?: Record<string, unknown>) =>
  api.get<Case[]>('/api/cases', { params }).then(r => r.data)

export const getCase = (id: number) =>
  api.get<Case>(`/api/cases/${id}`).then(r => r.data)

export const createCase = (data: Partial<Case>) =>
  api.post<Case>('/api/cases', data).then(r => r.data)

export const updateCase = (id: number, data: Partial<Case>) =>
  api.put<Case>(`/api/cases/${id}`, data).then(r => r.data)

export const deleteCase = (id: number) =>
  api.delete(`/api/cases/${id}`)

export const addNote = (id: number, content: string, author: string) =>
  api.post(`/api/cases/${id}/notes`, { content, author }).then(r => r.data)

export const getNetwork = (id: number) =>
  api.get<NetworkGraph>(`/api/cases/${id}/network`).then(r => r.data)

export const searchCases = (q: string, riskLevel?: string, category?: string) =>
  api.get('/api/search', { params: { q, riskLevel, category } }).then(r => r.data)

export const getDashboardStats = () =>
  api.get<DashboardStats>('/api/analytics/dashboard').then(r => r.data)

export const getRiskDistribution = () =>
  api.get('/api/analytics/risk-distribution').then(r => r.data)

export const getTimeline = (days = 30) =>
  api.get('/api/analytics/timeline', { params: { days } }).then(r => r.data)

export const scoreTransaction = (tx: Record<string, unknown>) =>
  mlApi.post('/score', tx).then(r => r.data)

export const getModelInfo = () =>
  mlApi.get('/model/info').then(r => r.data)

export default api
