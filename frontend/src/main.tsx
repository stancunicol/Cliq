import React from 'react'
import ReactDOM from 'react-dom/client'
import { App } from './App'
import './styles.css'
import './accessibility.css'
import './layout.css'
import './brand.css'
import { installMockBridge } from './mockBridge'

if (import.meta.env.DEV && new URLSearchParams(window.location.search).has('mock')) {
  installMockBridge()
}
ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><App /></React.StrictMode>)
