export function imageUrl(key: string | null) {
  if (!key) return null
  if (key.startsWith('http://') || key.startsWith('https://') || key.startsWith('blob:') || key.startsWith('data:')) return key
  if (key.startsWith('/uploads/') || key.startsWith('uploads/')) {
    const apiBase = (import.meta.env.VITE_API_BASE_URL || 'https://cvsystem-jtnh.onrender.com')
      .replace(/\/api\/?$/, '')
      .replace(/\/$/, '')
    return `${apiBase}/${key.replace(/^\//, '')}`
  }
  const base = import.meta.env.VITE_S3_PUBLIC_BASE_URL?.replace(/\/$/, '') || 'https://pub-1834497bb7c24e8cb36a4a06dd7e7ffd.r2.dev'
  return `${base}/${key.replace(/^\//, '')}`
}
