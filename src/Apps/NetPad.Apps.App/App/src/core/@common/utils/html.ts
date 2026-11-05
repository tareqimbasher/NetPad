/**
 * Markup made by the {@link html} template tag, so every value interpolated into it has been escaped.
 */
export type SafeHtml = string & { readonly __brand: "SafeHtml" };

const entities: Record<string, string> = {
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    "\"": "&quot;",
    "'": "&#39;",
};

/**
 * A template tag that keeps the template's own markup and HTML-escapes every interpolated value.
 * `null` and `undefined` become empty text. A nested `html` result is escaped like any other value.
 *
 * @example html`Delete <code>${secret.key}</code>?`
 */
export function html(strings: TemplateStringsArray, ...values: unknown[]): SafeHtml {
    let markup = strings[0];

    for (let i = 0; i < values.length; i++) {
        markup += escape(values[i]) + strings[i + 1];
    }

    return markup as SafeHtml;
}

function escape(value: unknown): string {
    if (value === null || value === undefined) {
        return "";
    }

    return String(value).replace(/[&<>"']/g, char => entities[char]);
}
