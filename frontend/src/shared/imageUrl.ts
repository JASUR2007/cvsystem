export function imageUrl(key: string | null) {
  const base = import.meta.env.VITE_S3_PUBLIC_BASE_URL?.replace(/\/$/, '')
  return key && base ? `${base}/${key}` : null
}
