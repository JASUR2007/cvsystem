export function imageUrl(key: string | null) {
  if (!key) return null
  if (key.startsWith('http://') || key.startsWith('https://') || key.startsWith('blob:') || key.startsWith('data:')) return key
  if (key.startsWith('/uploads/') || key.startsWith('uploads/')) {
    const apiBase = (import.meta.env.VITE_API_BASE_URL || 'https://cvsystem-jtnh.onrender.com')
      .replace(/\/api\/?$/, '')
      .replace(/\/$/, '')
    return `${apiBase}/${key.replace(/^\//, '')}`
  }
  const base = import.meta.env.VITE_S3_PUBLIC_BASE_URL?.replace(/\/$/, '') || 'https://storage.acdn.uz/cvmanagement'
  return `${base}/${key.replace(/^\//, '')}`
}
