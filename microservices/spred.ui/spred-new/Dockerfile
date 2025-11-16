FROM node:22-alpine

# Set working directory
WORKDIR /app

# Add node_modules/.bin to PATH
ENV PATH /app/node_modules/.bin:$PATH

# Copy package files and install dependencies
COPY package.json package-lock.json ./
RUN npm ci

# Install TypeScript globally
RUN npm install -g typescript
RUN npm install --save-dev vite

# Copy application source code
COPY . .

# Build the app
RUN npm run build

# Install serve for production
RUN npm install -g serve

EXPOSE 443

CMD [ "serve", "-s", "dist", "-l", "443" ]
