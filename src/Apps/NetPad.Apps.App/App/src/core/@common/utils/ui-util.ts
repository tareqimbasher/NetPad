import {normalizeLogicalKey} from "@common/utils/logical-key";
import {SubscriptionToken} from "@common/events/subscription-token";

/**
 * A set of utility functions related to UI functionality.
 */
export class UiUtil {
    /**
     * Adds a keyboard event handler to the element that catches select all (CTRL or META + A).
     * When the key combination is detected it selects all content of this element and calls
     * preventDefault() on the event.
     * @param element The element to attach the select all keyboard event handler to.
     */
    public static confineSelectAllToElement(element: Element) {
        if (!element.hasAttribute("tabindex")) {
            console.warn("element does not have a tabindex attribute");
        }

        const selectAllKeyHandler = (ev: KeyboardEvent) => {
            if (normalizeLogicalKey(ev.key) === "A" && (ev.ctrlKey || ev.metaKey)) {
                const range = document.createRange();
                range.selectNode(element);
                window.getSelection()?.removeAllRanges();
                window.getSelection()?.addRange(range);

                ev.preventDefault();
            }
        };

        element.addEventListener("keydown", selectAllKeyHandler as EventListenerOrEventListenerObject);
        return new SubscriptionToken(() => element.removeEventListener("keydown", selectAllKeyHandler as EventListenerOrEventListenerObject));
    }
}
