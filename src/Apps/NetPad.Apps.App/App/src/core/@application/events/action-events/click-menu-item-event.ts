/**
 * An action event instructing app to click a specific menu item.
 */
export class ClickMenuItemEvent {
    constructor(public readonly menuItemId: string) {
    }
}
