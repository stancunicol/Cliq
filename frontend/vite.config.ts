import ts from 'typescript'
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [
    {
      name: 'typescript-cts',
      enforce: 'pre',
      transform: (code, id) => id.endsWith('.cts')
        ? {
            code: ts.transpileModule(code, {
              compilerOptions: {
                module: ts.ModuleKind.ESNext,
                target: ts.ScriptTarget.ES2022,
              },
              fileName: id,
            }).outputText,
            map: null,
          }
        : undefined,
    },
    react(),
  ],
  server: { host: '127.0.0.1', port: 5173 },
  test: {
    environment: 'jsdom',
  },
})
