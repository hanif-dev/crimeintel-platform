// src/types/index.ts
export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical'
export type CaseStatus = 'Open' | 'Active' | 'UnderReview' | 'Closed' | 'Escalated'
export type CrimeCategory =
  | 'FinancialFraud' | 'Cybercrime' | 'Trafficking'
  | 'TerrorismFinancing' | 'MoneyLaundering' | 'OrganisedCrime'

export interface Entity {
  id: number
  name: string
  type: string
  identifier?: string
  country?: string
  riskScore?: number
  caseId: number
}

export interface Transaction {
  id: number
  transactionId: string
  timestamp: string
  amount: number
  currency: string
  fromAccount?: string
  toAccount?: string
  fromCountry?: string
  toCountry?: string
  fraudScore: number
  isFlagged: boolean
  caseId: number
}

export interface CaseNote {
  id: number
  content: string
  author: string
  createdAt: string
}

export interface Case {
  id: number
  caseNumber: string
  title: string
  description: string
  category: CrimeCategory
  status: CaseStatus
  riskLevel: RiskLevel
  riskScore: number
  createdAt: string
  updatedAt: string
  assignedAnalyst?: string
  countryCode?: string
  latitude?: number
  longitude?: number
  entities: Entity[]
  transactions: Transaction[]
  notes: CaseNote[]
}

export interface NetworkNode {
  id: string
  label: string
  type: string
  risk?: number
  country?: string
}

export interface NetworkEdge {
  source: string
  target: string
  label: string
  weight?: number
  evidence?: string
}

export interface NetworkGraph {
  nodes: NetworkNode[]
  edges: NetworkEdge[]
}

export interface DashboardStats {
  total: number
  active: number
  critical: number
  flaggedTransactions: number
  flaggedValue: number
}
