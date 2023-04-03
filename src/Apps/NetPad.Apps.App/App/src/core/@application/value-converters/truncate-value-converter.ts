export class TruncateValueConverter {
    public toView(text: string, max: number): string | null {
        if (text === "" || !max)
            return text;

        if (!text || typeof text !== "string")
            return null;

        return text.length > max ? text.substring(0, max) + "..." : text;
    }
}
