import {ISystemService} from "@application/system/isystem-service";

export class BrowserSystemService implements ISystemService {
    public openUrlInBrowser(url: string): void {
        window.open(url, "_blank");
    }
}
