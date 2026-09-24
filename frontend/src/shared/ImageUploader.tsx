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
        objectKey = uploaded.objectKey || uploaded.publicUrl || ''
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
        <div className="my-3 d-flex flex-column align-items-center justify-content-center text-center">
          <div className="position-relative d-inline-block">
            <img
              src={source}
              alt="Uploaded preview"
              className="upload-preview border rounded shadow-sm"
              style={{
                maxWidth: 180,
                maxHeight: 180,
                objectFit: 'contain',
                backgroundColor: 'var(--bs-tertiary-bg)',
                padding: '4px',
              }}
            />
            {value && (
              <button
                className="btn btn-danger btn-sm rounded-circle position-absolute"
                type="button"
                style={{
                  top: -10,
                  right: -10,
                  width: 26,
                  height: 26,
                  padding: 0,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  boxShadow: '0 2px 6px rgba(0,0,0,0.3)',
                }}
                title={t('Remove image')}
                aria-label={t('Remove image')}
                onClick={() => {
                  setPreview(null)
                  onChange(null)
                }}
              >
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                  <line x1="18" y1="6" x2="6" y2="18" />
                  <line x1="6" y1="6" x2="18" y2="18" />
                </svg>
              </button>
            )}
          </div>
          {value && (
            <button
              className="btn btn-sm btn-outline-danger mt-2 d-inline-flex align-items-center gap-1"
              type="button"
              onClick={() => {
                setPreview(null)
                onChange(null)
              }}
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
              <span>{t('Remove image')}</span>
            </button>
          )}
        </div>
      )}

      {uploading && <small className="text-muted d-block">{t('Uploading...')}</small>}
      {error && <small className="text-danger d-block" role="alert">{error}</small>}
    </div>
  )
}
