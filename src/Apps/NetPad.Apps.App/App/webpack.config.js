/* eslint-disable @typescript-eslint/no-var-requires */
const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const TerserPlugin = require('terser-webpack-plugin');
const {BundleAnalyzerPlugin} = require('webpack-bundle-analyzer');
const MonacoWebpackPlugin = require('monaco-editor-webpack-plugin');
const Dotenv = require('dotenv-webpack');
const tsNameof = require("ts-nameof");

const cssLoader = 'css-loader';

const sassLoader = {
    loader: 'sass-loader',
    options: {
        sassOptions: {
            includePaths: ['node_modules']
        }
    }
};

const postcssLoader = {
    loader: 'postcss-loader',
    options: {
        postcssOptions: {
            plugins: ['autoprefixer']
        }
    }
};

module.exports = function (env, {analyze}) {
    const production = env.production || process.env.NODE_ENV === 'production';

    if (!process.env.NODE_ENV) {
        process.env.NODE_ENV = production ? "production" : "development";
    }

    return {
        //target: 'web',
        target: 'electron-renderer',
        mode: production ? 'production' : 'development',
        devtool: production ? undefined : 'eval-cheap-source-map',
        optimization: {
            minimize: true,
            minimizer: [
                new TerserPlugin({
                    // Added these options so that passing class types as parameters (ex. EventBus) works.
                    // Without this webpack will mangle class names, breaking any calls to Function.name.
                    terserOptions: {
                        keep_classnames: true,
                        keep_fnames: true
                    }
                })
            ]
        },
        entry: {
            entry: './src/main.ts'
        },
        output: {
            path: path.resolve(__dirname, 'dist'),
            filename: production ? '[name].[contenthash].bundle.js' : '[name].bundle.js'
        },
        resolve: {
            extensions: ['.ts', '.js'],
            modules: [
                path.resolve(__dirname, 'src'),
                path.resolve(__dirname, 'src/core'),
                path.resolve(__dirname, 'dev-app'),
                path.resolve(__dirname, 'node_modules'),
            ],
            fallback: {
                "fs": false,
                "path": require.resolve("path-browserify"),
            }
        },
        devServer: {
            historyApiFallback: true,
            //open: !process.env.CI,
            port: 9000
        },
        module: {
            rules: [
                {test: /\.(png|svg|jpg|jpeg|gif)$/i, type: 'asset'},
                {test: /\.(woff|woff2|ttf|eot|svg|otf)(\?v=[0-9]\.[0-9]\.[0-9])?$/i, type: 'asset'},
                {test: /\.css$/i, use: ['style-loader', cssLoader, postcssLoader]},
                {test: /\.scss$/i, use: ['style-loader', cssLoader, postcssLoader, sassLoader]},
                {
                    test: /\.ts$/i,
                    use: [
                        {
                            loader: 'ts-loader',
                            options: {
                                getCustomTransformers: () => ({before: [tsNameof]}),
                            },
                        },
                        '@aurelia/webpack-loader'
                    ],
                    exclude: /node_modules/
                },
                {
                    test: /[/\\]src[/\\].+\.html$/i,
                    use: '@aurelia/webpack-loader',
                    exclude: /node_modules/
                }
            ]
        },
        plugins: [
            new HtmlWebpackPlugin({template: 'index.html', favicon: '../wwwroot/favicon.ico'}),
            new Dotenv({
                path: `./.env${production ? '' : '.' + process.env.NODE_ENV}`,
            }),
            new MonacoWebpackPlugin({
                languages: [
                    "csharp"
                ]
            }),
            analyze && new BundleAnalyzerPlugin()
        ].filter(p => p),
        experiments: {
            topLevelAwait: true // Enables us to use await in src/main.ts
        }
    }
}
