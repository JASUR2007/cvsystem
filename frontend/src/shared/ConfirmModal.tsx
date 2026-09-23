import { useEffect } from 'react'
import { t } from './i18n'

type ConfirmModalProps = {
  isOpen: boolean
  title: string
  message: string
  confirmText?: string
  cancelText?: string
  confirmVariant?: 'danger' | 'primary'
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmModal({
  isOpen,
  title,
  message,
  confirmText,
  cancelText,
  confirmVariant = 'danger',
  onConfirm,
  onCancel,
}: ConfirmModalProps) {
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && isOpen) {
        onCancel()
      }
    }
    if (isOpen) {
      document.body.style.overflow = 'hidden'
      window.addEventListener('keydown', handleKeyDown)
    }
    return () => {
      document.body.style.overflow = ''
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen, onCancel])

  if (!isOpen) return null

  return (
    <div className="custom-modal-backdrop" onClick={onCancel}>
      <div
        className="custom-modal-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-modal-title"
        onClick={e => e.stopPropagation()}
      >
        <div className="custom-modal-header">
          <h3 id="confirm-modal-title" className="custom-modal-title">
            {t(title)}
          </h3>
          <button
            type="button"
            className="custom-modal-close"
            onClick={onCancel}
            aria-label={t('Cancel')}
          >
            ×
          </button>
        </div>
        <div className="custom-modal-body">
          <p className="mb-0">{message}</p>
        </div>
        <div className="custom-modal-footer">
          <button
            type="button"
            className="btn btn-outline-secondary btn-sm"
            onClick={onCancel}
          >
            {t(cancelText || 'Cancel')}
          </button>
          <button
            type="button"
            className={`btn btn-${confirmVariant} btn-sm`}
            onClick={() => {
              onConfirm()
              onCancel()
            }}
          >
            {t(confirmText || 'Confirm')}
          </button>
        </div>
      </div>
    </div>
  )
}
