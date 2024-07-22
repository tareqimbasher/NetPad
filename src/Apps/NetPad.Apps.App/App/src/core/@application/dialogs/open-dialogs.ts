import {DialogOpenResult} from "@aurelia/dialog";

export class OpenDialogs {
    private static instances = new Map<string, DialogOpenResult>();

    public static get(name: string) {
        return this.instances.get(name);
    }

    public static set(name: string, openResult: DialogOpenResult) {
        this.instances.set(name, openResult);
    }

    public static delete(name: string) {
        return this.instances.delete(name);
    }

    public static clear() {
        this.instances.clear();
    }
}
