const config = {
    executable: "NetPad.Web",
    splashscreen: {
        imageFile: ""
    },
    name: "netpad",
    author: {
        name: "Tareq Imbasher",
        email: "tareq@meccasoft.com"
    },
    singleInstance: false,
    environment: "Production",
    aspCoreBackendPort: 8001,
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
        win: {
            target: "portable"
        },
        linux: {
            category: "Development",
            target: [
                {
                    target: "dir"
                },
                // {
                //     target: "pacman"
                // }
            ]
        }
    }
};

module.exports = config;
