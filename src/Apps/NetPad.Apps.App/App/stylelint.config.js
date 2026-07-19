module.exports = {
    "extends": "stylelint-config-standard-scss",
    "rules": {
        "indentation": 4,
        "at-rule-empty-line-before": null,
        "declaration-empty-line-before": null,
        "property-no-vendor-prefix": null,
        "value-no-vendor-prefix": null,
        "color-hex-length": null,
        "selector-type-no-unknown": null,
        // Exempts `rgba(var(--token-rgb), <alpha>)`, which the theme's channel tokens require —
        // see `to-rgb()` in styles/themes/common/_functions.scss.
        "color-function-notation": ["modern", {"ignore": ["with-var-inside"]}],
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
