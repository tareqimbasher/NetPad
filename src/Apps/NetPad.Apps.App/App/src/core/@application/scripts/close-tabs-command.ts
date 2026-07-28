/**
 * Instructs app to close open tabs.
 */
export class CloseTabsCommand {
    /**
     * @param scope "all" closes every tab; "others" closes every tab but one.
     * @param keepId The tab to keep when scope is "others". Defaults to the active tab.
     */
    constructor(public readonly scope: "all" | "others", public readonly keepId?: string) {
    }
}
