import { getToken } from '../auth/api'

const base = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? ''

export class ApiError extends Error {
  status: number
  details?: unknown
  constructor(status: number, message: string, details?: unknown) {
    super(message)
    this.status = status
    this.details = details
  }
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken()
  const headers = new Headers(options.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  let response: Response
  try {
    response = await fetch(`${base}/api${path}`, { ...options, headers })
  } catch {
    throw new ApiError(0, 'Cannot connect to the server.')
  }
  if (response.status === 204) return undefined as T
  const data = await response.json().catch(() => null)
  if (!response.ok) {
    const errors = data?.errors
    const detail = Array.isArray(errors) ? errors.join(' ') : errors && typeof errors === 'object' ? Object.values(errors).flat().join(' ') : ''
    throw new ApiError(response.status, data?.message ?? (detail || data?.title || `Request failed (${response.status}).`), data)
  }
  return data as T
}

export function json(method: string, body: unknown): RequestInit {
  return { method, body: JSON.stringify(body) }
}

export type Page<T> = { items: T[]; page: number; pageSize: number; totalItems: number; totalPages: number }
export type AttributeType = 'String' | 'Text' | 'Image' | 'Numeric' | 'Date' | 'Period' | 'Boolean' | 'Dropdown'
export type PositionLevel = 'Junior' | 'Middle' | 'Senior' | 'Lead'
export type AttributeListItem = { id: string; name: string; category: string; type: AttributeType; isBuiltIn: boolean; version: number; usageCount: number }
export type AttributeDetail = AttributeListItem & { description: string; options: { id: string; value: string }[] }
export type AttributeValue = { attributeId: string; name: string; category: string; type: AttributeType; textValue: string | null; numberValue: number | null; dateValue: string | null; periodStart: string | null; periodEnd: string | null; booleanValue: boolean | null; selectedOptionId: string | null; imageObjectKey: string | null; version: number }
export type PositionListItem = { id: string; title: string; company: string | null; level: PositionLevel | null; updatedAt: string; isPublic: boolean; cvCount: number }
export type PositionAttribute = { attributeId: string; name: string; type: AttributeType; sortOrder: number; isRequired: boolean }
export type AccessRule = { id: string; attributeId: string; attributeName: string; operator: string; comparisonValue: string }
export type PositionDetail = PositionListItem & { shortDescription: string; maxProjects: number; version: number; createdAt: string; attributes: PositionAttribute[]; accessRules: AccessRule[]; projectTags: string[] }
export type Project = { id: string; name: string; startedOn: string; endedOn: string | null; description: string; tags: string[]; version: number }
export type Profile = { id: string; firstName: string; lastName: string; email: string; location: string | null; photoObjectKey: string | null; language: string; theme: string; version: number }
export type CvListItem = { id: string; positionId: string; position: string; status: 'Draft' | 'Published'; updatedAt: string; likes: number }
export type CvAttribute = { value: AttributeValue; isRequired: boolean; isFilled: boolean; options: { id: string; value: string }[] }
export type CvDetail = { id: string; positionId: string; position: string; candidateId: string; firstName: string; lastName: string; location: string | null; photoObjectKey: string | null; status: 'Draft' | 'Published'; updatedAt: string; likes: number; canEdit: boolean; attributes: CvAttribute[]; projects: Project[] }
export type PositionCvPage = { columns: PositionAttribute[]; items: { id: string; candidateId: string; candidate: string; updatedAt: string; likes: number; values: { attributeId: string; value: string | null }[] }[]; page: number; pageSize: number; totalItems: number }

export function dateText(value: string | null | undefined): string {
  if (!value) return ''
  const d = new Date(value)
  if (isNaN(d.getTime())) return String(value)
  const day = String(d.getDate()).padStart(2, '0')
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const year = d.getFullYear()
  return `${day}/${month}/${year}`
}
