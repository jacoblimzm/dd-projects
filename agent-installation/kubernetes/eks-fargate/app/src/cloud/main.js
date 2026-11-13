/* global Parse */
const tracer = require('dd-trace')
const axios = require('axios')

const INVENTORY_URL = process.env.INVENTORY_URL || 'http://inventory:4000'

Parse.Cloud.define('enrich', async (request) => {
  const { orderId, sku, fail } = request.params

  // All Cloud functions share one route, so tag the span with the function name.
  const span = tracer.scope().active()
  if (span) span.setTag('parse.function', request.functionName || 'enrich')

  // Parse Server 6 removed Parse.Cloud.httpRequest; any HTTP client works and is traced via the http module.
  const stock = await axios.get(
    fail ? `${INVENTORY_URL}/fail` : `${INVENTORY_URL}/stock/${encodeURIComponent(sku)}`,
    { timeout: 5000 }
  )

  if (!orderId) return { sku, stock: stock.data }

  const order = await new Parse.Query('Order').get(orderId, { useMasterKey: true })
  order.set('status', stock.data.available > 0 ? 'reserved' : 'backordered')
  order.set('available', stock.data.available)
  await order.save(null, { useMasterKey: true })

  return { orderId, sku, status: order.get('status'), available: stock.data.available }
})
