# APP-10 OnboardR — service documents (NestJS). Image multi-stage.
FROM node:20-bookworm-slim AS build
WORKDIR /build
COPY package.json ./
RUN npm install
COPY tsconfig.json nest-cli.json ./
COPY src ./src
RUN npm run build

FROM node:20-bookworm-slim
WORKDIR /app
ENV NODE_ENV=production
COPY package.json ./
RUN npm install --omit=dev
COPY --from=build /build/dist ./dist
EXPOSE 3000
ENV PORT=3000
CMD ["node", "dist/main.js"]
