const { createProxyMiddleware } = require('http-proxy-middleware');

const target = process.env.API_URL || "https://mechahaze:8085";

module.exports = function (app) {
  app.use(
    '/sync',
    createProxyMiddleware({
      target,
      changeOrigin: true,
      ws: true,
    })
  );
};
