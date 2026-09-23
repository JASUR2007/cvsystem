import { useState } from 'react'
import { api } from './api'
import { imageUrl } from './imageUrl'
import { t } from './i18n'

export default function ImageUploader({
  value,
  onChange,
  userId,
}: {
  value: string | null
  onChange: (key: string | null) => void
  userId?: string
}) {
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState('')
  const [preview, setPreview] = useState<string | null>(null)

  async function upload(file: File) {
    setError('')
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      setError(t('Choose a JPEG, PNG or WebP image smaller than 5 MB.'))
      return
    }
    setUploading(true)
    try {
      let objectKey = ''
      try {
        // Direct upload to backend API (handles ACDN S3 with server credentials and robust fallback)
        const formData = new FormData()
        formData.append('file', file)
        const uploaded = await api<{ objectKey: string; publicUrl: string | null }>(
          `/files/upload${userId ? `?userId=${userId}` : ''}`,
          {
            method: 'POST',
            body: formData,
          }
        )
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
      setPreview(URL.createObjectURL(file))
      onChange(objectKey)
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Image upload failed.')
    } finally {
      setUploading(false)
    }
  }

  const source = preview ?? imageUrl(value)
  return (
    <div
      className="image-uploader"
      onDragOver={event => event.preventDefault()}
      onDrop={event => {
        event.preventDefault()
        const file = event.dataTransfer.files[0]
        if (file && !uploading) void upload(file)
      }}
    >
      <div className="mb-2">
        <label className="form-label small fw-semibold text-muted d-block mb-1">
          {t('Upload image')} <span className="fw-normal">(JPEG, PNG, WebP ≤ 5 MB)</span>
        </label>
        <input
          className="form-control"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          disabled={uploading}
          onChange={event => {
            const file = event.target.files?.[0]
            if (file) void upload(file)
          }}
        />
      </div>

      {source && (
        <div className="mb-2 d-flex align-items-center gap-2">
          <img
            src={source}
            alt="Uploaded preview"
            className="upload-preview border rounded"
            style={{ maxWidth: 140, maxHeight: 140, objectFit: 'cover' }}
          />
          {value && (
            <button
              className="btn btn-sm btn-outline-danger"
              type="button"
              onClick={() => {
                setPreview(null)
                onChange(null)
              }}
            >
              {t('Remove image')}
            </button>
          )}
        </div>
      )}

      {uploading && <small className="text-muted d-block">{t('Uploading...')}</small>}
      {error && <small className="text-danger d-block" role="alert">{error}</small>}
    </div>
  )
}
