/**
 * Ids of the commands NetPad ships with. An id is `<area>.<action>`.
 */
export enum CommandIds {
    // File
    newScript = "file.new",
    openFile = "file.open",
    goToScript = "file.goToScript",
    saveScript = "file.save",
    saveScriptAs = "file.saveAs",
    saveAllScripts = "file.saveAll",
    openScriptProperties = "file.properties",
    closeScript = "file.close",
    openSettings = "file.settings",
    exit = "file.exit",

    // Settings pages
    settingsGeneral = "settings.general",
    settingsEditor = "settings.editor",
    settingsResults = "settings.results",
    settingsCustomCss = "settings.customCss",
    settingsShortcuts = "settings.shortcuts",
    settingsOmniSharp = "settings.omniSharp",

    // Edit
    undo = "edit.undo",
    redo = "edit.redo",
    selectAll = "edit.selectAll",
    find = "edit.find",
    replace = "edit.replace",
    transformToUpperOrLowerCase = "edit.transformToUpperOrLowerCase",
    transformToUpperCase = "edit.transformToUpperCase",
    transformToLowerCase = "edit.transformToLowerCase",
    transformToTitleCase = "edit.transformToTitleCase",
    transformToCamelCase = "edit.transformToCamelCase",
    transformToKebabCase = "edit.transformToKebabCase",
    transformToSnakeCase = "edit.transformToSnakeCase",
    toggleLineComment = "edit.toggleLineComment",
    toggleBlockComment = "edit.toggleBlockComment",

    // View
    toggleExplorerPane = "view.explorer",
    toggleOutputPane = "view.output",
    toggleCodePane = "view.code",
    toggleNamespacesPane = "view.namespaces",
    reloadWindow = "view.reload",
    toggleDeveloperTools = "view.toggleDeveloperTools",
    zoomIn = "view.zoomIn",
    zoomOut = "view.zoomOut",
    zoomReset = "view.resetZoom",
    toggleFullScreen = "view.toggleFullScreen",
    toggleVimMode = "view.toggleVimMode",
    openCommandPalette = "view.commandPalette",

    // Work area
    runScript = "script.run",
    stopScript = "script.stop",
    openScriptReferences = "script.configReferences",
    openScriptPackages = "script.configPackages",
    openScriptNamespaces = "script.configNamespaces",
    switchToLastActiveScript = "script.switchToLastActive",
    closeOtherTabs = "script.closeOtherTabs",
    closeAllTabs = "script.closeAllTabs",

    // Tools
    checkAppDependencies = "tools.dependencyCheck",
    stopRunningScripts = "tools.stopRunningScripts",
    stopScriptsAndRunners = "tools.stopScriptHosts",

    // Help
    openWiki = "help.wiki",
    openGitHub = "help.github",
    searchIssues = "help.searchIssues",
    checkForUpdates = "help.checkForUpdates",
    about = "help.about",
}
