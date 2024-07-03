import {PLATFORM} from "aurelia";

export class Util {
    /**
     * Generates a new GUID.
     */
    public static newGuid(): string {
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
            const r = (Math.random() * 16) | 0,
                v = c == "x" ? r : (r & 0x3) | 0x8;
            return v.toString(16);
        });
    }

    public static dateToFormattedString(date: Date, format: string): string {
        if (!date || !format) return "";

        return format.replaceAll("yyyy", date.getFullYear().toString())
            .replaceAll("MM", ("0" + (date.getMonth() + 1)).slice(-2))
            .replaceAll("M", (date.getMonth() + 1).toString())
            .replaceAll("dd", ("0" + date.getDate()).slice(-2))
            .replaceAll("d", date.getDate().toString())
            .replaceAll("HH", ("0" + date.getHours()).slice(-2))
            .replaceAll("H", date.getHours().toString())
            .replaceAll("mm", ("0" + date.getMinutes()).slice(-2))
            .replaceAll("m", date.getMinutes().toString())
            .replaceAll("ss", ("0" + date.getSeconds()).slice(-2))
            .replaceAll("s", date.getSeconds().toString())
            .replaceAll("fff", ("000" + date.getMilliseconds()).slice(-3))
            .replaceAll("ff", ("00" + date.getMilliseconds().toString().slice(0, 2)).slice(-2))
            .replaceAll("f", date.getMilliseconds().toString().slice(0, 1));
    }

    /**
     * Gets the difference of 2 dates in number of days.
     * @param a
     * @param b
     */
    public static dateDiffInDays(a: Date, b: Date): number {
        // Discard the time and time-zone information.
        const utc1 = Date.UTC(a.getFullYear(), a.getMonth(), a.getDate());
        const utc2 = Date.UTC(b.getFullYear(), b.getMonth(), b.getDate());

        // 8.64e+7 milliseconds = 1 day
        return Math.floor(Math.abs(utc2 - utc1) / 8.64e7);
    }

    /**
     * Converts a string to title case.
     * @param str string.
     */
    public static toTitleCase(str: string) {
        return str.replace(/\w\S*/g, function (txt) {
            return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();
        });
    }

    /**
     * Truncates a string.
     * @param str The string to truncate.
     * @param maxLength The length after which the target string will be truncated.
     */
    public static truncate(str: string, maxLength: number): string {
        if (typeof str !== "string" || !str || maxLength <= 0 || str.length <= maxLength) return str;

        return str.substring(0, maxLength) + "...";
    }

    /**
     * Removes the specified character from the beginning and end of a string.
     * @param str The string to trim.
     * @param character The character to remove.
     */
    public static trim(str: string, character: string): string {
        if (!str)
            return str;

        let start = 0,
            end = str.length;

        while (start < end && str[start] === character)
            ++start;

        while (end > start && str[end - 1] === character)
            --end;

        return (start > 0 || end < str.length) ? str.substring(start, end) : str;
    }

    /**
     * Removes the specified set of characters from the beginning and end of a string.
     * @param str The string to trim.
     * @param characters The characters to remove.
     */
    public static trimAny(str: string, ...characters: string[]): string {
        if (!str)
            return str;

        let start = 0,
            end = str.length;

        while (start < end && characters.indexOf(str[start]) >= 0)
            ++start;

        while (end > start && characters.indexOf(str[end - 1]) >= 0)
            --end;

        return (start > 0 || end < str.length) ? str.substring(start, end) : str;
    }

    /**
     * Removes the specified character from the start of a string.
     * @param str The string to trim.
     * @param character The character to remove.
     */
    public static trimStart(str: string, character: string): string {
        if (!str)
            return str;

        let start = 0;
        const end = str.length;

        while (start < end && str[start] === character)
            ++start;

        return (start > 0 && start < str.length) ? str.substring(start, end) : str;
    }

    /**
     * Removes the specified character from the end of a string.
     * @param str The string to trim.
     * @param character The character to remove.
     */
    public static trimEnd(str: string, character: string): string {
        if (!str)
            return str;

        const start = 0;
        let end = str.length;

        while (end > start && str[end - 1] === character)
            --end;

        return (end < str.length) ? str.substring(start, end) : str;
    }

    /**
     * Trims a word from the start and the end of a string.
     * @param str The string to trim.
     * @param word The word to remove.
     */
    public static trimWord(str: string, word: string): string {
        const len = word.length;
        let start = 0,
            end = str.length;

        while (start < end && this.hasSubstringAt(str, word, start))
            start += word.length;

        while (end > start && this.hasSubstringAt(str, word, end - len))
            end -= word.length

        return (start > 0 || end < str.length) ? str.substring(start, end) : str;
    }

    /**
     * Checks if a string is a letter.
     * @param str The string to check.
     */
    public static isLetter(str: string): boolean {
        return str.length === 1 && !!str.match(/[a-z]/i);
    }

    /**
     * Groups a collection by the selected key.
     * @param collection The collection to group.
     * @param keyGetter A function that selects the key to group by.
     */
    public static groupBy<TItem, TKey>(collection: Array<TItem>, keyGetter: (item: TItem) => TKey): Map<TKey, Array<TItem>> {
        const map = new Map();

        for (const item of collection) {
            const key = keyGetter(item);

            const collection = map.get(key);

            if (!collection) {
                map.set(key, [item]);
            } else {
                collection.push(item);
            }
        }

        return map;
    }

    /**
     * Returns a new array with the unique items from the provided array.
     * @param collection The array to filter.
     */
    public static distinct<TItem>(collection: Array<TItem>): Array<TItem> {
        return [...new Set(collection)];
    }

    /**
     * Creates an awaitable promise that resolves after the specified number of milliseconds.
     * @param ms Milliseconds after which to resolve the promise.
     */
    public static delay(ms: number): Promise<void> {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Creates a debounced function that delays invoking func until after wait milliseconds have elapsed since the last time the
     * debounced function was invoked.
     * @param thisArg The value to use as this when calling func.
     * @param func The function to debounce.
     * @param waitMs The number of milliseconds to debounce.
     * @param immediate If true, will execute func immediately and then waits for the interval before func can be executed again.
     */
    public static debounce(thisArg: unknown, func: (...args: unknown[]) => void, waitMs: number, immediate?: boolean): (...args: unknown[]) => void {
        let timeout: NodeJS.Timeout | undefined;
        let isImmediateCall: boolean | undefined = false;

        return (...args: unknown[]) => {
            const later = () => {
                timeout = undefined;
                if (!isImmediateCall) func.call(thisArg, ...args);
            };

            isImmediateCall = immediate && !timeout;

            const callNow = immediate && isImmediateCall;

            if (timeout) clearTimeout(timeout);

            timeout = setTimeout(later, waitMs);

            if (callNow) func.call(thisArg, ...args);
        };
    }

    /**
     * Creates a promise that resolves after the specified number of milliseconds.
     * @param ms The delay in milliseconds.
     */
    public delay = (ms: number) => new Promise((resolve) => PLATFORM.setTimeout(resolve, ms));

    private static hasSubstringAt(str: string, substr: string, pos: number) {
        const len = substr.length;
        let idx = 0;

        for (const max = str.length; idx < len; ++idx) {
            if ((pos + idx) >= max || str[pos + idx] != substr[idx])
                break;
        }

        return idx === len;
    }
}
