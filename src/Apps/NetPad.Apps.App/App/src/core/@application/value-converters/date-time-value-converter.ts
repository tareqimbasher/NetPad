export class DateTimeValueConverter {
    /**
     * Converts a Date instance to a string.
     * @param date The Date instance to convert.
     */
    public toView(date?: Date): string | null {
        if (!date || !(date instanceof Date)) {
            return null;
        }

        return date.toLocaleString();
    }
}
