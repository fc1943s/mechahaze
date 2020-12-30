const { createProxyMiddleware } = require('http-proxy-middleware');

const target = process.env.API_URL || `http://${process.env.HOST}:8085/sync`;

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
