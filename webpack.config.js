const path = require("path");
const webpack = require("webpack");
const { VueLoaderPlugin } = require("vue-loader");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const fs = require("fs");

module.exports = (_, options) => {
    const isProduction = options.mode === "production";

    return {
        entry: {
            main: "./vue_client/src/main.ts",
            styles: "./vue_client/src/styles.scss"
        },
        output: {
            filename: "js/[name].[contenthash].js",
            chunkFilename: "js/[name].chunk.[contenthash].js",
            path: path.resolve(__dirname, "vue_client/dist"),
            publicPath: "/"
        },
        devtool: isProduction ? false : "source-map",
        resolve: {
            extensions: [".ts", ".js", ".vue", ".json"],
            alias: {
                "@": path.resolve(__dirname, "vue_client/src"),
                vue: "vue/dist/vue.esm-bundler.js"
            }
        },
        module: {
            rules: [
                {
                    test: /\.vue$/,
                    loader: "vue-loader"
                },
                {
                    test: /\.ts$/,
                    loader: "ts-loader",
                    options: { appendTsSuffixTo: [/\.vue$/] }
                },
                {
                    test: /\.(sc|c)ss$/,
                    use: [
                        MiniCssExtractPlugin.loader,
                        "css-loader",
                        {
                            loader: "postcss-loader",
                            options: {
                                postcssOptions: {
                                    plugins: [require("@tailwindcss/postcss"), require("autoprefixer")]
                                }
                            }
                        },
                        "sass-loader"
                    ]
                }
            ]
        },
        plugins: [
            new webpack.DefinePlugin({
                __VUE_OPTIONS_API__: JSON.stringify(true),
                __VUE_PROD_DEVTOOLS__: JSON.stringify(false),
                __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: JSON.stringify(false)
            }),
            new VueLoaderPlugin(),
            new MiniCssExtractPlugin({
                filename: "css/[name].[contenthash].css",
                chunkFilename: "css/[id].chunk.[contenthash].css"
            }),
            new HtmlWebpackPlugin({
                template: "vue_client/src/index.html",
                inject: true,
                chunks: ["main", "styles"]
            }),
            new CopyWebpackPlugin({
                patterns: [{ from: "vue_client/public", to: "./" }]
            })
        ],
        devServer: {
            static: path.join(__dirname, "vue_client/dist"),
            compress: true,
            port: process.env.VUE_APP_PORT,
            hot: true,
            historyApiFallback: true,
            server: {
                type: "https",
                options: {
                    key: fs.readFileSync(path.resolve(__dirname, "cert/localhost.key")),
                    cert: fs.readFileSync(path.resolve(__dirname, "cert/localhost.crt"))
                }
            },
            proxy: [
                {
                    context: ["/any2any.Demo/"],
                    target: `https://localhost:${process.env.API_PORT}`,
                    changeOrigin: true,
                    secure: false
                }
            ]
        }
    };
};
