import * as sanitizeHtml from 'sanitize-html';

export class SanitizeHtmlValueConverter {
    public toView(html?: string): string | null | undefined {
        if (!html)
            return html;

        return sanitizeHtml.default(html);
    }
}
