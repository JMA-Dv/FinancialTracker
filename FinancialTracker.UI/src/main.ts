import 'element-plus/dist/index.css'
import { createWebHistory } from 'vue-router'
import { createFinanceApp } from './app'

const { app, router } = createFinanceApp(createWebHistory())
void router.isReady().then(() => app.mount('#app'))
