// Template for webpack.config.js in Fable projects
// Find latest version in https://github.com/fable-compiler/webpack-config-template

// In most cases, you'll only need to edit the CONFIG object
// See below if you need better fine-tuning of Webpack options

let port = process.env.PORT || "8085";

let CONFIG = {
    indexHtmlTemplate: './public/index.html',
    fsharpEntry: './MechaHaze.UI.Frontend.fsproj',
    outputDir: '../dist',
    assetsDir: './public',
    devServerPort: 8082,
    // When using webpack-dev-server, you may need to redirect some calls
    // to a external API server. See https://webpack.js.org/configuration/dev-server/#devserver-proxy
    devServerProxy: {
        '/api/*': {
            target: 'http://localhost:' + port,
            changeOrigin: true
        },
        '/socket': {
            target: 'http://localhost:' + port,
            ws: true
        },
    },
    // Use babel-preset-env to generate JS compatible with most-used browsers.
    // More info at https://github.com/babel/babel/blob/master/packages/babel-preset-env/README.md
    babel: {
        presets: [
            ["@babel/preset-env", {
                "targets": {"node": "12"},
                "modules": false,
                "useBuiltIns": "usage",
                "corejs": 3,
                // This saves around 4KB in minified bundle (not gzipped)
                // "loose": true,
            }],
            ["@babel/preset-typescript", {}],
            ["@babel/preset-react", {}]
        ],
    }
};

// If we're running the webpack-dev-server, assume we're in development mode
let isProduction = !process.argv.find(v => v.indexOf('webpack-dev-server') !== -1);
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

let path = require("path");
let webpack = require("webpack");
let HtmlWebpackPlugin = require('html-webpack-plugin');
let CopyWebpackPlugin = require('copy-webpack-plugin');
let MiniCssExtractPlugin = require("mini-css-extract-plugin");

// The HtmlWebpackPlugin allows us to use a template for the index.html page
// and automatically injects <script> or <link> tags for generated bundles.
let commonPlugins = [
    new HtmlWebpackPlugin({
        filename: 'index.html',
        template: CONFIG.indexHtmlTemplate
    })
];

module.exports = {
    // In development, have two different entries to speed up hot reloading.
    // In production, have a single entry but use mini-css-extract-plugin to move the styles to a separate file.
    entry: {
        app: [CONFIG.fsharpEntry]
    } ,
    // Add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: {
        path: path.join(__dirname, CONFIG.outputDir),
        filename: isProduction ? '[name].[hash].js' : '[name].js',
        devtoolModuleFilenameTemplate: info =>
          path.resolve(info.absoluteResourcePath).replace(/\\/g, '/'),
    },
    mode: isProduction ? "production" : "development",
    devtool: isProduction ? "nosources-source-map" : "eval-source-map",
    optimization: {
        // Split the code coming from npm packages into a different file.
        // 3rd party dependencies change less often, let the browser cache them.
        splitChunks: {
                    chunks: "all"
                }
    },
    // Besides the HtmlPlugin, we use the following plugins:
    // PRODUCTION
    //      - MiniCssExtractPlugin: Extracts CSS from bundle to a different file
    //      - CopyWebpackPlugin: Copies static assets to output directory
    // DEVELOPMENT
    //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
    plugins: isProduction ?
        commonPlugins.concat([
            new MiniCssExtractPlugin({ filename: 'style.css' }),
            new CopyWebpackPlugin([{ from: CONFIG.assetsDir }]),
        ])
        : commonPlugins.concat([
            new webpack.HotModuleReplacementPlugin(),
        ]),
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false
    },
    // Configuration for webpack-dev-server
    devServer: {
        publicPath: "/",
        contentBase: CONFIG.assetsDir,
        port: CONFIG.devServerPort,
        proxy: CONFIG.devServerProxy,
        hot: true,
        inline: true
    },
    // - fable-loader: transforms F# into JS
    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    // - sass-loaders: transforms SASS/SCSS into JS
    // - file-loader: Moves files referenced in the code (fonts, images) into output folder
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: "fable-loader",
                    options: {
                        babel: CONFIG.babel
                    }
                },
            },
            {
                test: /\.[tj]sx?$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: CONFIG.babel
                },
            },
            {
                test: /\.(sass|scss|css)$/,
                use: [
                    isProduction
                        ? MiniCssExtractPlugin.loader
                        : 'style-loader',
                    'css-loader',
                    'sass-loader',
                ],
            },
            {
                test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                use: ["file-loader"]
            }
        ]
    }
};
