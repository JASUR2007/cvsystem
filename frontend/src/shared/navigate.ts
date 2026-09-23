export function navigate(path: string) {
  if (path === window.location.pathname + window.location.search) return
  window.history.pushState(null, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

