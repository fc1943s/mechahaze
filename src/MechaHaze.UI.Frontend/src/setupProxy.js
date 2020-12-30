const { createProxyMiddleware } = require('http-proxy-middleware');

const target = process.env.API_URL || `https://mechahaze:8085/sync`;

module.exports = function (app) {
  app.use(
    createProxyMiddleware(
      target,
      {
        secure: false,
        ws: true,
      })
  );
};
