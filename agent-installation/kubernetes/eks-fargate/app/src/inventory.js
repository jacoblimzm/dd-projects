const express = require('express')

const PORT = process.env.PORT || 4000
const app = express()

app.get('/health', (req, res) => res.json({ ok: true, node: process.version }))

app.get('/stock/:sku', (req, res) => {
  const delay = 20 + Math.floor(Math.random() * 150)
  setTimeout(() => {
    res.json({ sku: req.params.sku, available: Math.floor(Math.random() * 10), warehouse: 'sg-1' })
  }, delay)
})

app.get('/fail', (req, res, next) => {
  next(new Error('inventory backend unavailable (simulated)'))
})

app.use((err, req, res, next) => {
  console.error(err.stack)
  res.status(500).json({ error: err.message })
})

app.listen(PORT, () => console.log(`inventory listening on ${PORT} (node ${process.version})`))
