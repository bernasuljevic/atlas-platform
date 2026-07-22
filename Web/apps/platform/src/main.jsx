import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'

// shadcn bileşenleri (Card, Input, Button...) koyu renklerini ".dark" class'ı
// üzerinden alıyor - sitenin kendi mor/koyu temasında olduğu gibi prefers-color-scheme
// media query'sini değil. İkisini senkronlamazsak shadcn bileşenleri OS koyu modda
// bile açık renkli kalırdı.
if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
  document.documentElement.classList.add('dark');
}

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
