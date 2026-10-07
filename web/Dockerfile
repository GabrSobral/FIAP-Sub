# The build context is this folder, matching the other two services:
#   docker build -t fiapsub-web .
FROM node:24-alpine AS build
WORKDIR /app

# Install from the lockfile first, so editing source does not invalidate the install layer.
COPY package.json package-lock.json ./
RUN npm ci

COPY vite.config.js svelte.config.js index.html ./
COPY src/ src/
RUN npm run build

FROM nginx:1.29-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080

# 127.0.0.1, not localhost: nginx binds IPv4 only, and busybox wget resolves `localhost` to ::1
# first and gives up on connection refused instead of falling back.
HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget --quiet --spider http://127.0.0.1:8080/ || exit 1
