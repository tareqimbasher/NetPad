export class TextToHtmlValueConverter {
    public toView(text?: string): string | undefined | null {
        if (!text)
            return text;

        return text.replaceAll("\n", "<br/>");
    }
}
