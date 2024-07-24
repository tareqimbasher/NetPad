/**
 * Requests app to click a specific menu item.
 */
export class ClickMenuItemCommand {
    constructor(public readonly menuItemId: string) {
    }
}
