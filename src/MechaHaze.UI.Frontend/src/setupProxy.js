const { createProxyMiddleware } = require('http-proxy-middleware');

const target = process.env.API_URL || `http://mechahaze:8085/sync`;

module.exports = function (app) {
  app.use(
    createProxyMiddleware(
      target,
      {
        changeOrigin: true,
        ws: true,
      })
  );
};
