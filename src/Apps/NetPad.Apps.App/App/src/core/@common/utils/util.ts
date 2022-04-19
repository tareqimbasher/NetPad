import {PLATFORM} from "aurelia";

export class Util {
    public static newGuid(): string {
        return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
            const r = (Math.random() * 16) | 0,
                v = c == "x" ? r : (r & 0x3) | 0x8;
            return v.toString(16);
        });
    }

    /**
     * Gets the difference of 2 dates in number of days
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
     * @param str string
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
    public static truncate(str: string, maxLength: number) {
        if (!str || maxLength < 0 || str.length <= maxLength) return str;

        return str.substr(0, maxLength - 3) + "...";
    }

    /**
     * Removes the specified character from the beginning and end of a string.
     * @param str The string to trim.
     * @param character The character to remove.
     */
    public static trim(str: string | null | undefined, character: string) {
        if (!str)
            return str;

        let start = 0,
            end = str.length;

        while(start < end && str[start] === character)
            ++start;

        while(end > start && str[end - 1] === character)
            --end;

        return (start > 0 || end < str.length) ? str.substring(start, end) : str;
    }

    /**
     * Removes the specified set of characters from the beginning and end of a string.
     * @param str The string to trim.
     * @param characters The characters to remove.
     */
    public static trimAny(str: string | null | undefined, ...characters: string[]) {
        if (!str)
            return str;

        let start = 0,
            end = str.length;

        while(start < end && characters.indexOf(str[start]) >= 0)
            ++start;

        while(end > start && characters.indexOf(str[end - 1]) >= 0)
            --end;

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
     * Creates a debounced function that delays invoking func until after wait milliseconds have elapsed since the last time the
     * debounced function was invoked.
     * @param thisArg The value to use as this when calling func.
     * @param func The function to debounce.
     * @param waitMs The number of milliseconds to debounce.
     * @param immediate If true, will execute func immediately and then waits for the interval before calling func.
     */
    public static debounce(thisArg: unknown, func: (...args: any[]) => void, waitMs: number, immediate?: boolean) : (...args:any[]) => void {
        let timeout: number;
        let isImmediateCall = false;

        return (...args: any[]) => {
            const later = () => {
                timeout = null;
                if (!isImmediateCall) func.call(thisArg, ...args);
            };

            isImmediateCall = immediate && !timeout;

            const callNow = immediate && isImmediateCall;

            if (timeout) PLATFORM.clearTimeout(timeout);

            timeout = PLATFORM.setTimeout(later, waitMs);

            if (callNow) func.call(thisArg, ...args);
        };
    }

    /**
     * Creates a promise that resolves after the specified number of milliseconds.
     * @param ms The delay in milliseconds.
     */
    public delay = (ms: number) => new Promise((resolve) => PLATFORM.setTimeout(resolve, ms));
}
