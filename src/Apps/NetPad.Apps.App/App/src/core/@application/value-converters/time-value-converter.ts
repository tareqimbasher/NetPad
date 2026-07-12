export class TimeValueConverter {
    /**
     * Converts a Date instance to a locale time string (hour and minute).
     * @param date The Date instance to convert.
     */
    public toView(date?: Date): string | null {
        if (!date || !(date instanceof Date)) {
            return null;
        }

        return date.toLocaleTimeString([], {hour: "numeric", minute: "2-digit"});
    }
}
