/**
 * Applies the user's own CSS to the document.
 *
 * The style element goes in `body`, not `head`, so user rules keep winning the cascade.
 * Appending to `head` would only win until the next chunk loads: `style-loader` injects a module's
 * styles at the end of `head` when it is first imported.
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
