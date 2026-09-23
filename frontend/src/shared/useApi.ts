import { useCallback, useEffect, useState } from 'react'
import { api } from './api'

export function useApi<T>(path: string | null) {
  const [data, setData] = useState<T | null>(null)
  const [loading, setLoading] = useState(Boolean(path))
  const [error, setError] = useState<Error | null>(null)
  const [revision, setRevision] = useState(0)
  const reload = useCallback(() => setRevision(value => value + 1), [])

  useEffect(() => {
    if (path === null) return
    let active = true
    api<T>(path).then(value => {
      if (active) { setData(value); setError(null); setLoading(false) }
    }).catch(cause => {
      if (active) { setError(cause instanceof Error ? cause : new Error('Request failed.')); setLoading(false) }
    })
    return () => { active = false }
  }, [path, revision])

  return { data, loading, error, reload, setData }
}
