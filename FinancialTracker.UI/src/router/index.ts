import type { Pinia } from 'pinia'
import { createRouter, type RouteLocationRaw, type RouteRecordRaw, type Router, type RouterHistory } from 'vue-router'
import { useSessionStore } from '@/stores/session'

declare module 'vue-router' {
  interface RouteMeta {
    /** Routes without this are reachable while signed out. */
    requiresAuth?: boolean
    /** Ledger screens: a User with no Currency is sent to choose one first. */
    needsCurrency?: boolean
    title?: string
  }
}

export interface NavItem {
  label: string
  to: string
}

/** The screens the following tickets add; each links to a "coming soon" page until it exists. */
export const navItems: NavItem[] = [
  { label: 'Dashboard', to: '/' },
  { label: 'Accounts', to: '/accounts' },
  { label: 'Transactions', to: '/transactions' },
  { label: 'Transfers', to: '/transfers' },
  { label: 'Categories', to: '/categories' },
  { label: 'Payees', to: '/payees' },
]

/** Screens that exist; the rest of `navItems` show "coming soon". */
const builtScreens: Record<string, RouteRecordRaw['component']> = {
  '/categories': () => import('@/views/CategoriesView.vue'),
}

/** Where to send someone who must sign in first; Google brings them back to `returnUrl`. */
export function signInLocation(returnUrl: string): RouteLocationRaw {
  return { name: 'sign-in', query: { returnUrl } }
}

const routes: RouteRecordRaw[] = [
  {
    path: '/settings',
    name: 'settings',
    component: () => import('@/views/SettingsView.vue'),
    meta: { requiresAuth: true, title: 'Settings' },
  },
  {
    path: '/setup/currency',
    name: 'currency-setup',
    component: () => import('@/views/CurrencySetupView.vue'),
    meta: { requiresAuth: true, title: 'Choose your Currency' },
  },
  { path: '/sign-in', name: 'sign-in', component: () => import('@/views/SignInView.vue'), meta: { title: 'Sign in' } },
  ...navItems.map(
    (item): RouteRecordRaw => ({
      path: item.to,
      name: item.label.toLowerCase(),
      component: builtScreens[item.to] ?? (() => import('@/views/ComingSoonView.vue')),
      meta: { requiresAuth: true, needsCurrency: true, title: item.label },
    }),
  ),
  // Requires auth so a signed-out visitor is asked to sign in before learning which paths exist.
  {
    path: '/:path(.*)*',
    name: 'not-found',
    component: () => import('@/views/NotFoundView.vue'),
    meta: { requiresAuth: true, title: 'Page not found' },
  },
]

export function createAppRouter(history: RouterHistory, pinia: Pinia): Router {
  const router = createRouter({ history, routes })

  router.beforeEach(async (to) => {
    if (to.meta.requiresAuth !== true) {
      return true
    }
    const session = useSessionStore(pinia)
    if (session.status === 'unknown') {
      await session.load()
    }
    if (session.status === 'signedOut') {
      return signInLocation(to.fullPath)
    }
    // A failed session load leaves settings unknown; that is the retry screen's job, not a missing Currency.
    if (to.meta.needsCurrency === true && session.settings?.currency === null) {
      return { name: 'currency-setup', query: { returnUrl: to.fullPath } }
    }
    return true
  })

  return router
}
