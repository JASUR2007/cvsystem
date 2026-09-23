export function imageUrl(key: string | null) {
  if (!key) return null
  if (key.startsWith('http://') || key.startsWith('https://') || key.startsWith('blob:')) return key
  const base = import.meta.env.VITE_S3_PUBLIC_BASE_URL?.replace(/\/$/, '') || 'https://storage.acdn.uz/cvmanagement'
  return `${base}/${key.replace(/^\//, '')}`
}
