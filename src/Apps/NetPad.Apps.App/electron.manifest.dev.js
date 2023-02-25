const electronNetConfig = require("./electron.manifest.js");

// Development environment specific configuration
electronNetConfig.environment = "Development";

module.exports = electronNetConfig;
