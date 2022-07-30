export class DateTimeValueConverter {
    /**
     * Converts a Date instance to a string.
     * @param dateTime The Date instance to convert.
     * @param timeZone The timezone to convert to.
     */
    public toView(dateTime?: Date, timeZone: "UTC" | "Local" = "UTC"): string | null {
        if (!dateTime || !(dateTime instanceof Date))
            return null;

        if (!timeZone)
            timeZone = "UTC";

        return timeZone.toLowerCase() === "local" ? dateTime.toLocaleString() : dateTime.toUTCString();
    }
}
