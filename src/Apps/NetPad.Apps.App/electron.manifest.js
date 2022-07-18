const config = {
    name: "netpad",
    description: "A cross-platform C# editor and playground.",
    executable: "NetPad.Apps.App",
    author: {
        name: "Tareq Imbasher",
        email: "tareq@meccasoft.com"
    },
    license: "MIT",
    repository: {
        type: "git",
        url: "https://github.com/tareqimbasher/NetPad"
    },
    homepage: "https://github.com/tareqimbasher/NetPad#readme",
    splashscreen: {
        imageFile: "./wwwroot/256x256.png"
    },
    environment: "Production",
    singleInstance: false,
    aspCoreBackendPort: 50900,
    electronPort: 50901,
    build: {
        appId: "com.netpad.app",
        productName: "NetPad",
        copyright: "Copyright © 2020 Tareq Imbasher",
        buildVersion: "0.1.0",
        compression: "maximum",
        directories: {
            output: "../../../bin/Desktop"
        },
        extraResources: [
            {
                from: "./bin",
                to: "bin",
                filter: [
                    "**/*"
                ]
            },
        ],
        files: [
            {
                from: "./ElectronHostHook/node_modules",
                to: "ElectronHostHook/node_modules",
                filter: [
                    "**/*"
                ]
            },
            "**/*"
        ],
        linux: {
            category: "Development",
            icon: "../../../wwwroot/256x256.png",
            target: [
                {
                    target: "dir"
                },
                // {
                //     target: "pacman"
                // }
            ]
        },
        win: {
            target: "portable",
            icon: "../../../wwwroot/favicon.ico"
        },
        mac: {
            category: "public.app-category.developer-tools",
            darkModeSupport: true,
            target: [
                {
                    target: "dir"
                },
            ]
        }
    }
};

module.exports = config;
