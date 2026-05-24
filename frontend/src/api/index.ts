import axios from 'axios'
import type { Case, DashboardStats, NetworkGraph } from '@/types'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 10_000,
})

// Cases
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

// Search
export const searchCases = (q: string, riskLevel?: string, category?: string) =>
  api.get('/api/search', { params: { q, riskLevel, category } }).then(r => r.data)

// Analytics
export const getDashboardStats = () =>
  api.get<DashboardStats>('/api/analytics/dashboard').then(r => r.data)

export const getRiskDistribution = () =>
  api.get('/api/analytics/risk-distribution').then(r => r.data)

export const getTimeline = (days = 30) =>
  api.get('/api/analytics/timeline', { params: { days } }).then(r => r.data)

// ML
const mlApi = axios.create({
  baseURL: import.meta.env.VITE_ML_URL || 'http://localhost:8001',
  timeout: 10_000,
})

export const scoreTransaction = (tx: Record<string, unknown>) =>
  mlApi.post('/score', tx).then(r => r.data)

export const getModelInfo = () =>
  mlApi.get('/model/info').then(r => r.data)

export default api
