const express = require('express')
const axios = require('axios')

const PORT = process.env.PORT || 3000
const PARSE_URL = process.env.PARSE_URL || 'http://parse-api:1337/parse'
const APP_ID = process.env.PARSE_APP_ID || 'poc-app'

const parse = axios.create({
  baseURL: PARSE_URL,
  timeout: 10000,
  headers: { 'X-Parse-Application-Id': APP_ID, 'Content-Type': 'application/json' }
})

const app = express()

app.get('/health', (req, res) => res.json({ ok: true, node: process.version }))

app.get('/checkout', async (req, res) => {
  const sku = req.query.sku || `SKU-${Math.ceil(Math.random() * 5)}`
  try {
    const order = await parse.post('/classes/Order', { sku, qty: 1, status: 'new' })
    const enriched = await parse.post('/functions/enrich', { orderId: order.data.objectId, sku })
    res.json({ orderId: order.data.objectId, result: enriched.data.result })
  } catch (err) {
    console.error('checkout failed', err.message)
    res.status(502).json({ error: err.message })
  }
})

app.get('/error', async (req, res) => {
  try {
    await parse.post('/functions/enrich', { sku: 'FAIL', fail: true })
    res.json({ ok: true })
  } catch (err) {
    console.error('error route failed as expected', err.message)
    res.status(500).json({ error: err.message })
  }
})

app.listen(PORT, () => console.log(`gateway listening on ${PORT} (node ${process.version})`))
