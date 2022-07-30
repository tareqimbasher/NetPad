export class TextToHtmlValueConverter {
    /**
     * Converts text to an HTML escaped string.
     * @param text The text to convert.
     */
    public toView(text?: string): string | null {
        if (text === "")
            return text;

        if (!text || typeof text !== "string")
            return null;

        return text
            .replaceAll(" ", "&nbsp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\n", "<br/>");
    }
}
