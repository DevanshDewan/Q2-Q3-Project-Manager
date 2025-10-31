export default {
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7011',
        changeOrigin: true,
        secure: false
      }
    }
  }
}
