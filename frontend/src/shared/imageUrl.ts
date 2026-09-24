export function imageUrl(key: string | null) {
  if (!key) return null
  let normalized = key.trim()

  // Rewrite legacy storage.acdn.uz URLs to the active Cloudflare R2 CDN
  if (normalized.includes('storage.acdn.uz/cvmanagement/')) {
    normalized = normalized.replace(/.*storage\.acdn\.uz\/cvmanagement\//, '')
  }

  if (normalized.startsWith('blob:') || normalized.startsWith('data:')) return normalized
  if (normalized.startsWith('http://') || normalized.startsWith('https://')) return normalized

  if (normalized.startsWith('/uploads/') || normalized.startsWith('uploads/')) {
    const apiBase = (import.meta.env.VITE_API_BASE_URL || 'https://cvsystem-jtnh.onrender.com')
      .replace(/\/api\/?$/, '')
      .replace(/\/$/, '')
    return `${apiBase}/${normalized.replace(/^\//, '')}`
  }

  const base = import.meta.env.VITE_S3_PUBLIC_BASE_URL?.replace(/\/$/, '') || 'https://pub-1834497bb7c24e8cb36a4a06dd7e7ffd.r2.dev'
  return `${base}/${normalized.replace(/^\//, '')}`
}
