import { useState, useRef, useEffect, type FormEvent } from 'react'
import type { CurrentUser } from '../auth/api'
import { imageUrl } from './imageUrl'
import { t } from './i18n'

type HeaderProps = {
  currentPath?: string
  user: CurrentUser | null
  theme: string
  onThemeChange: (theme: string) => void
  language: string
  onLanguageChange: (language: string) => void
  onSignOut: () => void
}

const languages = [
  { code: 'en', label: 'English', short: 'EN' },
  { code: 'ru', label: 'Русский', short: 'RU' },
  { code: 'uz', label: "O'zbek", short: 'UZ' },
]

export default function Header({
  currentPath,
  user,
  theme,
  onThemeChange,
  language,
  onLanguageChange,
  onSignOut,
}: HeaderProps) {
  const [search, setSearch] = useState('')
  const [langOpen, setLangOpen] = useState(false)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const langRef = useRef<HTMLDivElement>(null)
  const path = currentPath || window.location.pathname

  const canManage = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') ?? false
  const isAdmin = user?.roles.includes('Administrator') ?? false

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (langRef.current && !langRef.current.contains(event.target as Node)) {
        setLangOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function submitSearch(event: FormEvent) {
    event.preventDefault()
    if (!search.trim()) return
    window.location.assign(`/search?q=${encodeURIComponent(search.trim())}`)
  }

  const currentLang = languages.find(l => l.code === language) || languages[0]

  return (
    <>
      <header className="site-header">
        <div className="header-container">
        {/* Left: Brand + Nav */}
        <div className="header-left">
          <a className="brand" href="/" aria-label="TalentHub Home">
            <span className="brand-mark">
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="M5 4h10a4 4 0 0 1 4 4v8a4 4 0 0 1-4 4H5V4Zm3 4v8m4-8v8m4-6v4" />
              </svg>
            </span>
            <span className="brand-text">TalentHub</span>
          </a>

          <nav className="desktop-nav" aria-label={t('Main navigation')}>
            <a className={path.startsWith('/positions') ? 'nav-link active' : 'nav-link'} href="/positions">
              {t('Positions')}
            </a>
            {canManage && (
              <a className={path.startsWith('/attributes') ? 'nav-link active' : 'nav-link'} href="/attributes">
                {t('Attributes')}
              </a>
            )}
            {user && (
              <a className={path.startsWith('/profile') ? 'nav-link active' : 'nav-link'} href="/profile">
                {t('Profile')}
              </a>
            )}
            {isAdmin && (
              <a className={path.startsWith('/admin') ? 'nav-link active' : 'nav-link'} href="/admin">
                {t('Admin')}
              </a>
            )}
          </nav>
        </div>

        {/* Center: Search */}
        <div className="header-center">
          <form className="header-search-form" onSubmit={submitSearch}>
            <span className="search-icon">
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <circle cx="11" cy="11" r="7" />
                <path d="m16 16 4 4" />
              </svg>
            </span>
            <input
              className="search-input"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('Search...')}
              aria-label={t('Search')}
            />
          </form>
        </div>

        {/* Right: Actions */}
        <div className="header-right">
          {/* Custom Language Dropdown */}
          <div className="lang-dropdown-wrapper" ref={langRef}>
            <button
              type="button"
              className="header-control-button lang-trigger"
              onClick={() => setLangOpen(!langOpen)}
              aria-expanded={langOpen}
              aria-label={t('Language')}
            >
              <svg className="control-icon" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2">
                <circle cx="12" cy="12" r="10" />
                <path d="M2 12h20M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
              </svg>
              <span className="lang-code">{currentLang.short}</span>
              <svg className={`caret-icon ${langOpen ? 'rotated' : ''}`} viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="6 9 12 15 18 9" />
              </svg>
            </button>

            {langOpen && (
              <div className="dropdown-menu-card">
                {languages.map(lang => (
                  <button
                    key={lang.code}
                    type="button"
                    className={`dropdown-item ${language === lang.code ? 'active' : ''}`}
                    onClick={() => {
                      onLanguageChange(lang.code)
                      setLangOpen(false)
                    }}
                  >
                    <span className="dropdown-item-short">{lang.short}</span>
                    <span className="dropdown-item-label">{lang.label}</span>
                    {language === lang.code && (
                      <svg className="check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
                        <polyline points="20 6 9 17 4 12" />
                      </svg>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Theme Toggle Button with Clean Sun/Moon */}
          <button
            type="button"
            className="header-control-button theme-toggle-btn"
            aria-label={t('Toggle theme')}
            title={t('Toggle theme')}
            onClick={() => onThemeChange(theme === 'dark' ? 'light' : 'dark')}
          >
            {theme === 'dark' ? (
              <svg className="control-icon sun-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <circle cx="12" cy="12" r="4" />
                <path d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32 1.41 1.41M2 12h2m16 0h2M4.93 19.07l1.41-1.41m11.32-11.32 1.41-1.41" />
              </svg>
            ) : (
              <svg className="control-icon moon-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
              </svg>
            )}
          </button>

          {/* User Profile / Auth Button */}
          {user ? (
            <div className="user-profile-actions">
              <a className="user-badge" href="/profile" title={user.firstName + ' ' + (user.lastName || '')}>
                {user.photoObjectKey ? (
                  <img
                    className="user-avatar-img"
                    src={imageUrl(user.photoObjectKey)!}
                    alt={user.firstName}
                  />
                ) : (
                  <span className="user-avatar-initials">
                    {(user.firstName[0] || 'U').toUpperCase()}
                  </span>
                )}
                <span className="user-name-label">{user.firstName}</span>
              </a>
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm sign-out-btn d-inline-flex align-items-center gap-1"
                onClick={onSignOut}
                title={t('Sign out')}
                aria-label={t('Sign out')}
              >
                <svg className="sign-out-icon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                  <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                  <polyline points="16 17 21 12 16 7" />
                  <line x1="21" y1="12" x2="9" y2="12" />
                </svg>
                <span className="sign-out-text">{t('Sign out')}</span>
              </button>
            </div>
          ) : (
            <a className="btn btn-primary btn-sm header-signin-btn" href="/login">
              {t('Sign in')}
            </a>
          )}

          {/* Mobile Hamburger Button */}
          <button
            type="button"
            className="mobile-hamburger-btn"
            aria-label="Toggle navigation"
            aria-expanded={mobileMenuOpen}
            onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
          >
            {mobileMenuOpen ? (
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            ) : (
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                <line x1="3" y1="12" x2="21" y2="12" />
                <line x1="3" y1="6" x2="21" y2="6" />
                <line x1="3" y1="18" x2="21" y2="18" />
              </svg>
            )}
          </button>
        </div>
      </div>

      {/* Mobile Drawer / Expanded Menu */}
      {mobileMenuOpen && (
        <div className="mobile-menu-drawer">
          <form className="mobile-search-form" onSubmit={e => { submitSearch(e); setMobileMenuOpen(false); }}>
            <span className="search-icon">
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <circle cx="11" cy="11" r="7" />
                <path d="m16 16 4 4" />
              </svg>
            </span>
            <input
              className="search-input"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('Search...')}
              aria-label={t('Search')}
            />
          </form>

          <nav className="mobile-nav-links">
            <a
              className={path.startsWith('/positions') ? 'mobile-nav-item active' : 'mobile-nav-item'}
              href="/positions"
              onClick={() => setMobileMenuOpen(false)}
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><rect x="2" y="7" width="20" height="14" rx="2" ry="2" /><path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" /></svg>
              <span>{t('Positions')}</span>
            </a>

            {canManage && (
              <a
                className={path.startsWith('/attributes') ? 'mobile-nav-item active' : 'mobile-nav-item'}
                href="/attributes"
                onClick={() => setMobileMenuOpen(false)}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><polygon points="12 2 2 7 12 12 22 7 12 2" /><polyline points="2 17 12 22 22 17" /><polyline points="2 12 12 17 22 12" /></svg>
                <span>{t('Attributes')}</span>
              </a>
            )}

            {user && (
              <a
                className={path.startsWith('/profile') ? 'mobile-nav-item active' : 'mobile-nav-item'}
                href="/profile"
                onClick={() => setMobileMenuOpen(false)}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" /></svg>
                <span>{t('Profile')}</span>
              </a>
            )}

            {isAdmin && (
              <a
                className={path.startsWith('/admin') ? 'mobile-nav-item active' : 'mobile-nav-item'}
                href="/admin"
                onClick={() => setMobileMenuOpen(false)}
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z" /></svg>
                <span>{t('Admin')}</span>
              </a>
            )}
          </nav>

          <div className="mobile-auth-footer">
            {user ? (
              <div className="mobile-user-row">
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginBottom: '8px' }}>
                  {user.photoObjectKey ? (
                    <img
                      className="user-avatar-img"
                      src={imageUrl(user.photoObjectKey)!}
                      alt={user.firstName}
                    />
                  ) : (
                    <span className="user-avatar-initials">
                      {(user.firstName[0] || 'U').toUpperCase()}
                    </span>
                  )}
                  <span className="user-name-label">{user.firstName} {user.lastName}</span>
                </div>
                <button
                  type="button"
                  className="btn btn-outline-danger btn-sm w-100"
                  onClick={() => { onSignOut(); setMobileMenuOpen(false); }}
                >
                  {t('Sign out')}
                </button>
              </div>
            ) : (
              <a
                className="btn btn-primary btn-sm w-100"
                href="/login"
                onClick={() => setMobileMenuOpen(false)}
              >
                {t('Sign in')}
              </a>
            )}
          </div>
        </div>
      )}
    </header>

    {/* Mobile Bottom Navigation Bar */}
    <nav className="mobile-bottom-nav" aria-label="Mobile Navigation">
      <a className={path.startsWith('/positions') ? 'bottom-nav-item active' : 'bottom-nav-item'} href="/positions">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
          <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
        </svg>
        <span>{t('Positions')}</span>
      </a>

      {canManage ? (
        <a className={path.startsWith('/attributes') ? 'bottom-nav-item active' : 'bottom-nav-item'} href="/attributes">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polygon points="12 2 2 7 12 12 22 7 12 2" />
            <polyline points="2 17 12 22 22 17" />
            <polyline points="2 12 12 17 22 12" />
          </svg>
          <span>{t('Attributes')}</span>
        </a>
      ) : (
        <a className={path.startsWith('/search') ? 'bottom-nav-item active' : 'bottom-nav-item'} href="/search">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="7" />
            <path d="m16 16 4 4" />
          </svg>
          <span>{t('Search')}</span>
        </a>
      )}

      <a className={path.startsWith('/profile') || path === '/login' ? 'bottom-nav-item active' : 'bottom-nav-item'} href={user ? '/profile' : '/login'}>
        {user?.photoObjectKey ? (
          <img
            className="nav-avatar-img"
            src={imageUrl(user.photoObjectKey)!}
            alt={user.firstName}
          />
        ) : (
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
            <circle cx="12" cy="7" r="4" />
          </svg>
        )}
        <span>{t('Profile')}</span>
      </a>

      {isAdmin ? (
        <a className={path.startsWith('/admin') ? 'bottom-nav-item active' : 'bottom-nav-item'} href="/admin">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
          </svg>
          <span>{t('Admin')}</span>
        </a>
      ) : canManage ? (
        <a className={path.startsWith('/search') ? 'bottom-nav-item active' : 'bottom-nav-item'} href="/search">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="7" />
            <path d="m16 16 4 4" />
          </svg>
          <span>{t('Search')}</span>
        </a>
      ) : (
        <a className={path === '/' ? 'bottom-nav-item active' : 'bottom-nav-item'} href="/">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
            <polyline points="9 22 9 12 15 12 15 22" />
          </svg>
          <span>{t('Overview')}</span>
        </a>
      )}
    </nav>
  </>
  )
}
