# Build context Web/ klasörü olmalı (docker-compose.yml'deki
# "context: ./Web") - npm workspaces yapısı (package-lock.json, packages/ui)
# tek başına apps/platform'un içinde değil, Web/ kökünde yaşıyor.
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
COPY apps/platform/package.json apps/platform/
COPY packages/ui/package.json packages/ui/
RUN npm ci

COPY . .
RUN npm run build --workspace=web

# Statik dosyaları (dist/) sunmak için tam bir Node runtime'a gerek yok -
# nginx hem daha küçük hem de React Router'ın client-side route'ları
# (ör. /audit-log) için "her yolu index.html'e düşür" ayarını nginx.conf'ta yapıyoruz.
FROM nginx:alpine AS runtime
COPY --from=build /app/apps/platform/dist /usr/share/nginx/html
COPY apps/platform/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
