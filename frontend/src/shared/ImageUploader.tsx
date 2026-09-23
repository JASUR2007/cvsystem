import { useState } from 'react'
import { api } from './api'
import { imageUrl } from './imageUrl'

type Presign = { objectKey: string; uploadUrl: string; publicUrl: string | null }

export default function ImageUploader({ value, onChange, userId }: { value: string | null; onChange: (key: string | null) => void; userId?: string }) {
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const [preview, setPreview] = useState<string | null>(null)

  async function upload(file: File) {
    setError('')
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      setError('Choose a JPEG, PNG or WebP image smaller than 5 MB.')
      return
    }
    setUploading(true)
    try {
      let objectKey = ''
      try {
        const signed = await api<Presign>(`/files/presign${userId ? `?userId=${userId}` : ''}`, { method: 'POST', body: JSON.stringify({ contentType: file.type }) })
        const response = await fetch(signed.uploadUrl, { method: 'PUT', body: file })
        if (!response.ok) throw new Error('S3 presign upload rejected')
        objectKey = signed.objectKey
      } catch {
        try {
          // Fallback to backend direct upload (bypasses S3 CORS / presign signature issues)
          const formData = new FormData()
          formData.append('file', file)
          const uploaded = await api<{ objectKey: string; publicUrl: string | null }>(`/files/upload${userId ? `?userId=${userId}` : ''}`, {
            method: 'POST',
            body: formData,
          })
          objectKey = uploaded.publicUrl || uploaded.objectKey
        } catch {
          // Client-side fallback: Data URL
          objectKey = await new Promise<string>((resolve, reject) => {
            const reader = new FileReader()
            reader.onload = () => resolve(reader.result as string)
            reader.onerror = reject
            reader.readAsDataURL(file)
          })
        }
      }
      setPreview(URL.createObjectURL(file))
      onChange(objectKey)
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Image upload failed.') }
    finally { setUploading(false) }
  }

  const source = preview ?? imageUrl(value)
  return <div className="image-uploader" onDragOver={event => event.preventDefault()} onDrop={event => { event.preventDefault(); const file = event.dataTransfer.files[0]; if (file && !uploading) void upload(file) }}>{source && <img src={source} alt="Uploaded preview" className="upload-preview" />}<input className="form-control" type="file" accept="image/jpeg,image/png,image/webp" disabled={uploading} onChange={event => { const file = event.target.files?.[0]; if (file) void upload(file) }} />{uploading && <small>Uploading...</small>}{error && <small className="text-danger" role="alert">{error}</small>}{value && <button className="btn btn-sm btn-outline-danger" type="button" onClick={() => { setPreview(null); onChange(null) }}>Remove image</button>}</div>
}
