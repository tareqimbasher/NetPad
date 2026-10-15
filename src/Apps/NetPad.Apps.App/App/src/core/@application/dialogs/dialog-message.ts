import {SafeHtml} from "@common";

/**
 * The body of an alert or ask dialog. `message` is shown as text. `messageHtml` is markup and is typed SafeHtml,
 * which only the html template tag returns, so every value interpolated into it is escaped.
 */
export type DialogMessage =
    | { message: string; messageHtml?: never }
    | { messageHtml: SafeHtml; message?: never };
