type SpinnerProps = {
  size?: 'sm' | 'md' | 'lg'
  className?: string
  label?: string
}

export default function Spinner({ size = 'md', className = '', label }: SpinnerProps) {
  const sizePx = size === 'sm' ? 18 : size === 'lg' ? 44 : 28
  const strokeWidth = size === 'sm' ? 3 : size === 'lg' ? 3.5 : 3

  return (
    <div
      className={`talenthub-spinner-wrapper ${className}`}
      role="status"
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '10px',
      }}
    >
      <svg
        width={sizePx}
        height={sizePx}
        viewBox="0 0 38 38"
        xmlns="http://www.w3.org/2000/svg"
        style={{
          animation: 'talenthub-spin 0.8s linear infinite',
          flexShrink: 0,
        }}
        aria-hidden="true"
      >
        <defs>
          <linearGradient x1="8.042%" y1="0%" x2="65.682%" y2="23.865%" id="spinnerGrad">
            <stop stopColor="#2563eb" stopOpacity="0" offset="0%" />
            <stop stopColor="#2563eb" stopOpacity=".3" offset="63.146%" />
            <stop stopColor="#2563eb" offset="100%" />
          </linearGradient>
        </defs>
        <g fill="none" fillRule="evenodd">
          <g transform="translate(1 1)">
            <path
              d="M36 18c0-9.94-8.06-18-18-18"
              id="spinnerOval"
              stroke="url(#spinnerGrad)"
              strokeWidth={strokeWidth}
              strokeLinecap="round"
            />
            <circle fill="#2563eb" cx="36" cy="18" r="1.5" />
          </g>
        </g>
      </svg>
      {label && <span style={{ fontSize: size === 'sm' ? '13px' : '14px', color: 'var(--bs-secondary-color)' }}>{label}</span>}
    </div>
  )
}
