import type { Pinia } from 'pinia'
import { createRouter, type RouteRecordRaw, type Router, type RouterHistory } from 'vue-router'
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
      return { name: 'sign-in', query: { returnUrl: to.fullPath } }
    }
    return true
  })

  return router
}
