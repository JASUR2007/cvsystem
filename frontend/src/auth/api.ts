export type CurrentUser = {
  id: string
  email: string
  firstName: string
  lastName: string
  roles: string[]
}

type AuthResponse = {
  token: string
  user: CurrentUser
}

const tokenKey = 'talenthub_token'
export const apiBase = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? ''

export function getToken() {
  return sessionStorage.getItem(tokenKey)
}

export function saveToken(token: string) {
  sessionStorage.setItem(tokenKey, token)
}

export async function getCurrentUser(): Promise<CurrentUser | null> {
  const token = getToken()
  if (!token) return null

  const response = await fetch(`${apiBase}/api/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  })

  if (response.status === 401) {
    sessionStorage.removeItem(tokenKey)
    return null
  }

  if (!response.ok) {
    throw new Error('Could not load your account.')
  }

  return response.json() as Promise<CurrentUser>
}

export async function submitAuth(mode: 'login' | 'register', data: Record<string, string>): Promise<AuthResponse> {
  const response = await fetch(`${apiBase}/api/auth/${mode}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  })

  const result = await response.json()
  if (!response.ok) {
    const errors = result.errors
    const details = Array.isArray(errors) ? errors : errors && typeof errors === 'object' ? Object.values(errors).flat() : []
    const message = result.message ?? (details.length > 0 ? details.join(' ') : null) ?? result.title ?? 'Something went wrong. Please try again.'
    throw new Error(message)
  }

  return result as AuthResponse
}

export async function signOut() {
  const token = getToken()
  if (token) {
    try {
      await fetch(`${apiBase}/api/auth/logout`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
    } finally {
      sessionStorage.removeItem(tokenKey)
    }
  }
}
