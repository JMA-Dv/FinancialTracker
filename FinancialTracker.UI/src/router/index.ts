import type { Pinia } from 'pinia'
import { createRouter, type RouteLocationRaw, type RouteRecordRaw, type Router, type RouterHistory } from 'vue-router'
import { useSessionStore } from '@/stores/session'

declare module 'vue-router' {
  interface RouteMeta {
    /** Routes without this are reachable while signed out. */
    requiresAuth?: boolean
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

/** Where to send someone who must sign in first; Google brings them back to `returnUrl`. */
export function signInLocation(returnUrl: string): RouteLocationRaw {
  return { name: 'sign-in', query: { returnUrl } }
}

const routes: RouteRecordRaw[] = [
  { path: '/sign-in', name: 'sign-in', component: () => import('@/views/SignInView.vue'), meta: { title: 'Sign in' } },
  ...navItems.map(
    (item): RouteRecordRaw => ({
      path: item.to,
      name: item.label.toLowerCase(),
      component: () => import('@/views/ComingSoonView.vue'),
      meta: { requiresAuth: true, title: item.label },
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
    return true
  })

  return router
}
