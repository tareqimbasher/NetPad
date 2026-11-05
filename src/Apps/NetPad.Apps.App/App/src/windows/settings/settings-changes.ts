const isGroup = (value: unknown): value is Record<string, unknown> =>
    typeof value === "object" && value !== null && !Array.isArray(value);

const sameValue = (a: unknown, b: unknown) => JSON.stringify(a) === JSON.stringify(b);

/**
 * Records the path (like `appearance.mode`) of every setting whose edited value differs from the saved one.
 */
export function collectChanges(saved: unknown, edited: unknown, path: string, changed: Set<string>) {
    if (isGroup(saved) && isGroup(edited)) {
        const keys = new Set([...Object.keys(saved), ...Object.keys(edited)]);

        for (const key of keys) {
            collectChanges(saved[key], edited[key], path ? `${path}.${key}` : key, changed);
        }

        return;
    }

    if (!sameValue(saved, edited)) {
        changed.add(path);
    }
}

/**
 * Applies settings saved elsewhere to settings the user is still editing, without losing the user's edits.
 *
 * @param base The saved settings the user started editing from.
 * @param edited The settings being edited. Changed directly, so the form stays bound to it.
 * @param saved The newly saved settings. Values the user hasn't changed (still equal to `base`) are taken from here.
 */
export function mergeSavedSettings(base: unknown, edited: Record<string, unknown>, saved: Record<string, unknown>) {
    const baseObject = isGroup(base) ? base : {};
    const keys = new Set([...Object.keys(edited), ...Object.keys(saved)]);

    for (const key of keys) {
        const editedValue = edited[key];
        const savedValue = saved[key];

        if (isGroup(editedValue) && isGroup(savedValue)) {
            mergeSavedSettings(baseObject[key], editedValue, savedValue);
        } else if (sameValue(editedValue, baseObject[key])) {
            edited[key] = savedValue;
        }
    }
}
