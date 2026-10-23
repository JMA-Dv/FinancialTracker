import { createPinia } from 'pinia'
import { createApp, type App as VueApp } from 'vue'
import type { Router, RouterHistory } from 'vue-router'
import App from './App.vue'
import { onUnauthorized } from './api/client'
import { createAppRouter, signInLocation } from './router'
import { useSessionStore } from './stores/session'

export function createFinanceApp(history: RouterHistory): { app: VueApp; router: Router } {
  const app = createApp(App)
  const pinia = createPinia()
  app.use(pinia)
  const router = createAppRouter(history, pinia)
  app.use(router)

  const session = useSessionStore(pinia)
  onUnauthorized(() => {
    // Asking who is signed in answers 401 for a visitor; the route guard handles that one.
    if (!session.isSignedIn) {
      return
    }
    session.clear()
    void router.replace(signInLocation(router.currentRoute.value.fullPath))
  })

  return { app, router }
}
