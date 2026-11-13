FROM node:14.21.3-bullseye-slim

WORKDIR /app
ENV NODE_ENV=production

COPY package.json ./
RUN npm install --production --no-audit --no-fund && npm cache clean --force

COPY src ./src

ENV ROLE=gateway
CMD ["sh", "-c", "exec node -r dd-trace/init src/${ROLE}.js"]
