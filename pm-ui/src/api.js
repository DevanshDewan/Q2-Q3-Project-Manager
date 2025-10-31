// src/api.js
import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:5000/api', // Direct connection to backend
  headers: { 'Content-Type': 'application/json' },
})

// Request interceptor: attach token from localStorage
api.interceptors.request.use(cfg => {
  try {
    const token = localStorage.getItem('pm_token')
    if (token) cfg.headers.Authorization = `Bearer ${token}`
  } catch (e) {}
  return cfg
}, e => Promise.reject(e))

export default api
