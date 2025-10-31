import React from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import './styles.css' // optional; file can be empty or contain tiny tweaks

createRoot(document.getElementById('root')).render(<App />)
