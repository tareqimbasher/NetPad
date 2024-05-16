module.exports = {
    "extends": "stylelint-config-standard-scss",
    "rules": {
        "indentation": 4,
        "at-rule-empty-line-before": null,
        "declaration-empty-line-before": null,
        "value-no-vendor-prefix": null,
        "color-hex-length": null,
        "selector-type-no-unknown": null,
        "scss/at-extend-no-missing-placeholder": [
            true,
            {
                "severity": "warning"
            }
        ],
        "no-descending-specificity": [
            true,
            {
                "severity": "warning"
            }
        ]
    }
}
