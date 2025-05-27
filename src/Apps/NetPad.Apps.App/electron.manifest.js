/**
 * @type {import('./App/node_modules/electron-builder').Configuration}
 * @see https://www.electron.build/configuration/configuration
 */
const electronBuilderConfig = {
    appId: "com.netpad.app",
    productName: "NetPad",
    copyright: "Copyright © 2021 Tareq Imbasher",
    buildVersion: "0.9.1",
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
        }
    ],
    files: [
        "**/*"
    ],
    linux: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
        icon: "../../../wwwroot/logo/circle/",
        category: "Development;IDE;Building;Database;TextEditor;",
        desktop: {
            Type: "Application",
            Name: "NetPad",
            Categories: "Development;IDE;Building;Database;TextEditor;",
            StartupNotify: "true",
            StartupWMClass: "netpad",
            Encoding: "UTF-8",
            MimeType: "application/x-netpad"
        },
        target: [
            {
                target: "pacman",
            },
            {
                target: "AppImage",
            },
            {
                target: "deb",
            },
            {
                target: "rpm",
            },
            // {
            //     target: "flatpak",
            // },
            {
                target: "snap",
            },
            {
                target: "zip",
            },
        ]
    },
    mac: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
        category: "public.app-category.developer-tools",
        icon: "../../../wwwroot/logo/circle/logo.icns",
        darkModeSupport: true,
        target: [
            {
                target: "zip"
            }
        ]
    },
    win: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
        icon: "../../../wwwroot/favicon-256x256.ico",
        target: [
            {
                target: "nsis"
            },
            {
                target: "zip"
            }
        ]
    },
    pacman: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
    },
    appImage: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
    },
    deb: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
        depends: ["libnotify4", "libxtst6", "libnss3"]
    },
    rpm: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
    },
    flatpak: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
    },
    snap: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
    },
    nsis: {
        artifactName: "${name}-${version}-${os}-${arch}.${ext}",
        oneClick: false,
        allowToChangeInstallationDirectory: true,
    }
}

const electronNetConfig = {
    environment: "Production",
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
        imageFile: "./wwwroot/logo/square/128x128.png"
    },
    singleInstance: true,
    electronPort: 57931,
    build: electronBuilderConfig
};

module.exports = electronNetConfig;
