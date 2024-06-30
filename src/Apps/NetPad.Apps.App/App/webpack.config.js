/* eslint-disable @typescript-eslint/no-var-requires */
const path = require("path");
const Dotenv = require("dotenv-webpack");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const TerserPlugin = require("terser-webpack-plugin");
const {BundleAnalyzerPlugin} = require("webpack-bundle-analyzer");
const MonacoWebpackPlugin = require("monaco-editor-webpack-plugin");
/* eslint-enable @typescript-eslint/no-var-requires */

const cssLoader = "css-loader";

const sassLoader = {
    loader: "sass-loader",
    options: {
        sassOptions: {
            includePaths: ["node_modules"]
        }
    }
};

const postcssLoader = {
    loader: "postcss-loader",
    options: {
        postcssOptions: {
            plugins: ["autoprefixer"]
        }
    }
};

module.exports = function (env, {analyze}) {
    const production = env.production || process.env.NODE_ENV === "production";
    const environment = production ? "production" : "development";
    const target = env.target || "electron";

    console.log("Build info:");
    console.log(`   Environment: ${environment}`);
    console.log(`   Target: ${target}`);

    /** @type {import("webpack").Configuration} */
    return {
        target: target === "web" ? "web" : "electron-renderer",
        mode: production ? "production" : "development",
        devtool: production ? undefined : "eval-cheap-source-map",
        entry: {
            entry: "./src/main.ts"
        },
        output: {
            path: path.resolve(__dirname, "dist"),
            filename: production ? "[name].[contenthash].bundle.js" : "[name].bundle.js"
        },
        resolve: {
            extensions: [".ts", ".js"],
            modules: [
                path.resolve(__dirname, "src"),
                path.resolve(__dirname, "src/core"),
                path.resolve(__dirname, "dev-app"),
                path.resolve(__dirname, "node_modules"),
            ],
            fallback: {
                "fs": false,
                "process": false,
                "path": require.resolve("path-browserify"),
            },
            alias: {
                // Re-route all "import from "monaco-editor" to this so that MonacoWebpackPlugin
                // can do its job and only bundle the parts of monaco editor that we need and not
                // all of it. The "$" here is so we allow more specific imports like:
                // import {IQuickInputService} from "monaco-editor/esm/vs/platform/quickinput/common/quickInput"
                "monaco-editor$": "monaco-editor/esm/vs/editor/editor.api.js",
                ...getAureliaAliases(production)
            }
        },
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
        devServer: {
            historyApiFallback: true,
            //open: !process.env.CI,
            port: 9000,
            client: {
                progress: true,
                overlay: false
            },
        },
        module: {
            rules: [
                {test: /\.(png|svg|jpg|jpeg|gif)$/i, type: "asset"},
                {test: /\.(woff|woff2|ttf|eot|svg|otf)(\?v=[0-9]\.[0-9]\.[0-9])?$/i, type: "asset"},
                {test: /\.css$/i, use: ["style-loader", cssLoader, postcssLoader]},
                {test: /\.scss$/i, use: ["style-loader", cssLoader, postcssLoader, sassLoader]},
                {
                    test: /\.ts$/i,
                    use: [
                        {
                            loader: "ts-loader"
                        },
                        "@aurelia/webpack-loader"
                    ],
                    exclude: /node_modules/
                },
                {
                    test: /[/\\]src[/\\].+\.html$/i,
                    use: "@aurelia/webpack-loader",
                    exclude: /node_modules/
                }
            ]
        },
        plugins: [
            new HtmlWebpackPlugin({template: "index.html", favicon: "../wwwroot/favicon.ico"}),
            new Dotenv({
                path: `./.env${production ? '' : "." + environment}`,
            }),
            new MonacoWebpackPlugin({
                languages: [
                    "csharp",
                    "sql",
                    "json",
                    "css"
                ]
            }),
            analyze && new BundleAnalyzerPlugin()
        ].filter(p => p),
        experiments: {
            topLevelAwait: true // Enables us to use await in src/main.ts
        }
    }
}

function getAureliaAliases(isProduction) {
    if (isProduction) {
        return {};
    }

    return [
        'aurelia',
        'fetch-client',
        'kernel',
        'metadata',
        'platform',
        'platform-browser',
        'route-recognizer',
        'router',
        'router-lite',
        'runtime',
        'runtime-html',
        'testing',
        'state',
        'ui-virtualization'
    ].reduce((map, pkg) => {
        const name = pkg === 'aurelia' ? pkg : `@aurelia/${pkg}`;
        try {
            const packageLocation = require.resolve(name);
            map[name] = path.resolve(packageLocation, `../../esm/index.dev.mjs`);
        } catch {/**/}
        return map;
    }, {});
}
