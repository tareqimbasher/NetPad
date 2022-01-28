export class DateTimeValueConverter {
    public toView(dateTime?: Date): string {
        if (!dateTime)
            return "";

        return dateTime.toUTCString();
    }
}
