export type CurrentUser = {
  id: string
  email: string
  firstName: string
  lastName: string
  roles: string[]
  photoObjectKey?: string | null
}

type AuthResponse = {
  token: string
  user: CurrentUser
}

const tokenKey = 'talenthub_token'
const userKey = 'talenthub_user'
export const apiBase = import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? ''

export function getToken(): string | null {
  return localStorage.getItem(tokenKey) || sessionStorage.getItem(tokenKey)
}

export function getCachedUser(): CurrentUser | null {
  try {
    const raw = localStorage.getItem(userKey) || sessionStorage.getItem(userKey)
    return raw ? (JSON.parse(raw) as CurrentUser) : null
  } catch {
    return null
  }
}

export function saveAuth(token: string, user: CurrentUser, remember = true) {
  if (remember) {
    localStorage.setItem(tokenKey, token)
    localStorage.setItem(userKey, JSON.stringify(user))
    sessionStorage.removeItem(tokenKey)
    sessionStorage.removeItem(userKey)
  } else {
    sessionStorage.setItem(tokenKey, token)
    sessionStorage.setItem(userKey, JSON.stringify(user))
    localStorage.removeItem(tokenKey)
    localStorage.removeItem(userKey)
  }
}

export function saveToken(token: string, remember = true) {
  if (remember) {
    localStorage.setItem(tokenKey, token)
    sessionStorage.removeItem(tokenKey)
  } else {
    sessionStorage.setItem(tokenKey, token)
    localStorage.removeItem(tokenKey)
  }
}

export function clearAuth() {
  localStorage.removeItem(tokenKey)
  localStorage.removeItem(userKey)
  sessionStorage.removeItem(tokenKey)
  sessionStorage.removeItem(userKey)
}

export async function getCurrentUser(): Promise<CurrentUser | null> {
  const token = getToken()
  if (!token) {
    clearAuth()
    return null
  }

  try {
    const response = await fetch(`${apiBase}/api/auth/me`, {
      credentials: 'include',
      headers: { Authorization: `Bearer ${token}` },
    })

    if (response.status === 401) {
      clearAuth()
      window.dispatchEvent(new CustomEvent('talenthub_unauthorized'))
      return null
    }

    if (!response.ok) {
      return getCachedUser()
    }

    const user = (await response.json()) as CurrentUser
    if (localStorage.getItem(tokenKey)) {
      localStorage.setItem(userKey, JSON.stringify(user))
    }
    if (sessionStorage.getItem(tokenKey)) {
      sessionStorage.setItem(userKey, JSON.stringify(user))
    }
    return user
  } catch {
    return getCachedUser()
  }
}

export async function submitAuth(mode: 'login' | 'register', data: Record<string, any>): Promise<AuthResponse> {
  const response = await fetch(`${apiBase}/api/auth/${mode}`, {
    method: 'POST',
    credentials: 'include',
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
        credentials: 'include',
        headers: { Authorization: `Bearer ${token}` },
      })
    } finally {
      clearAuth()
    }
  } else {
    clearAuth()
  }
}
