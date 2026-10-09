/**
 * Applies the user's own CSS to the document.
 *
 * The app's stylesheets are built into `@layer app` (see webpack.config.js) and this CSS is not, so
 * it wins at any specificity. Against the unlayered sheets (Monaco's, and the app's rules that restyle
 * Monaco) only order counts, so the element goes in `body`, after everything `style-loader` appends to `head`.
 */
export class CustomCss {
    private static readonly styleElementId = "user-custom-styles";

    /**
     * The applied style element, or null when the user has none. Its presence already means the
     * CSS is enabled and non-empty, so callers that need to carry the user's styling elsewhere
     * (ex: HTML export) do not have to consult settings again.
     */
    public static get element(): HTMLElement | null {
        return document.getElementById(CustomCss.styleElementId);
    }

    /** Replaces whatever was applied before. A null or empty value removes it. */
    public static apply(css: string | null | undefined) {
        let styleElement = document.getElementById(CustomCss.styleElementId);

        if (!css) {
            styleElement?.remove();
            return;
        }

        const cssTextNode = document.createTextNode(css);

        if (styleElement) {
            styleElement.replaceChildren(cssTextNode);
            return;
        }

        styleElement = document.createElement("style");
        styleElement.id = CustomCss.styleElementId;
        styleElement.setAttribute("type", "text/css");
        styleElement.appendChild(cssTextNode);

        document.body.prepend(styleElement);
    }
}
