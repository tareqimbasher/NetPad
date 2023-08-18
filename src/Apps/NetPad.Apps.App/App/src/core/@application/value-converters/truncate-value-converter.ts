import {Util} from "@common";

export class TruncateValueConverter {
    public toView(text: string, maxLength: number): string | null {
        return Util.truncate(text, maxLength);
    }
}
