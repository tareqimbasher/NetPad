// We must force tsc to interpret this file as a module, resolves the following:
// "Augmentations for the global scope can only be directly nested in external modules or ambient module declarations."
export {}

declare global {
    /**
     * Gets the name of a key on object.
     * @param obj The object.
     * @param key The key to get.
     */
    function nameof<TObject>(obj: TObject, key: keyof TObject): string;

    /**
     * Gets the name of a key on a type.
     * @param key The key to get.
     */
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    function nameof<TObject>(key: keyof TObject | (new(...args: any[]) => TObject)): string;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const _global = (window /* browser */ || global /* node */) as any;
_global.nameof = function<TObject>(key1: keyof TObject | (new() => TObject), key2?: keyof TObject) : string {
    if (key2) {
        return key2 as string;
    }

    if (typeof key1 === "string") {
        return key1;
    }

    if (typeof key1 === "function" && key1.name) {
        return key1.name;
    }

    throw new Error(`Could not determine nameof ${key1 as unknown}`);
}
