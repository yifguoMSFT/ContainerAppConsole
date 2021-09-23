FROM node:14-alpine as build-step
WORKDIR /app
COPY package.json ./
RUN npm install 
COPY . .
RUN npm run build

FROM nginx:1.20.1-alpine as prod-staing
COPY --from=build-step /app/dist/container-app-console-ui /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]