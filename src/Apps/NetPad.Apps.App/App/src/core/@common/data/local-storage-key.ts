/**
 * localStorage keys follow `netpad.<area>.<name>` in kebab-case, e.g. `netpad.tab-bar.main.viewables-order`.
 */

/**
 * Moves a value from a key an earlier release used to the key that replaced it, so renaming a key doesn't reset the
 * state users already have. Call it before the first read of `newKey`.
 */
export function migrateLocalStorageKey(legacyKey: string, newKey: string): void {
    const legacyValue = localStorage.getItem(legacyKey);
    if (legacyValue === null) {
        return;
    }

    if (localStorage.getItem(newKey) === null) {
        localStorage.setItem(newKey, legacyValue);
    }

    localStorage.removeItem(legacyKey);
}
