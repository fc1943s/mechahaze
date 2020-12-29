const path = require('path');
const fs = require('fs');
// const { addReactRefresh } = require('customize-cra-react-refresh');
const { override, babelInclude, removeModuleScopePlugin, useBabelRc } = require('customize-cra');

module.exports = (config, env) => Object.assign(
  config,
  override(
    useBabelRc(),
    babelInclude([
      path.resolve('./src'),
      fs.realpathSync('./public'),
    ]),
    // addReactRefresh(),
    removeModuleScopePlugin(),
  )(config, env),
);
