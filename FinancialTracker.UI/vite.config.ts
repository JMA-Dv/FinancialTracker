import { readFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'
import vue from '@vitejs/plugin-vue'
import { defineConfig, loadEnv } from 'vite'

const defaultApiTarget = 'https://localhost:7204'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')

  // Google sends the browser back to the origin the API saw, which is this dev server, over https.
  const https =
    env.VITE_HTTPS_CERT && env.VITE_HTTPS_KEY
      ? { cert: readFileSync(env.VITE_HTTPS_CERT), key: readFileSync(env.VITE_HTTPS_KEY) }
      : undefined

  const api = {
    target: env.VITE_API_TARGET || defaultApiTarget,
    // The API's development certificate is self-signed.
    secure: false,
  }

  return {
    plugins: [vue()],
    resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
    server: {
      https,
      proxy: {
        // Same origin as the app, so the API's Lax cookie and the Google redirect work without CORS.
        '/api': api,
        // Where Google sends the browser after sign-in; the API's Google handler answers it.
        '/signin-google': api,
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      server: { deps: { inline: ['element-plus'] } },
    },
  }
})
