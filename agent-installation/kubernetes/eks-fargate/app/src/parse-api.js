const path = require('path')
const express = require('express')
const { ParseServer } = require('parse-server')

const PORT = process.env.PORT || 1337

const server = new ParseServer({
  databaseURI: process.env.PARSE_DATABASE_URI || 'mongodb://mongo:27017/parse',
  cloud: path.join(__dirname, 'cloud', 'main.js'),
  appId: process.env.PARSE_APP_ID || 'poc-app',
  masterKey: process.env.PARSE_MASTER_KEY || 'poc-master-key',
  serverURL: `http://localhost:${PORT}/parse`,
  allowClientClassCreation: true,
  enforcePrivateUsers: false
})

async function main () {
  await server.start()
  const app = express()
  app.get('/health', (req, res) => res.json({ ok: true, node: process.version }))
  app.use('/parse', server.app)
  app.listen(PORT, () => console.log(`parse-api listening on ${PORT} (node ${process.version})`))
}

main().catch(err => {
  console.error('parse-api failed to start', err)
  process.exit(1)
})
