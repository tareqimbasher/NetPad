import {Toast} from "bootstrap";
import {Util} from "@common";

/**
 * Shows toast notifications.
 */
export class ToastUtil {
    private static readonly noHeaderHtmlTemplate =
        `<div class="toast" style="width: fit-content">
                <div class="toast-body d-flex justify-content-between">
                    {0}
                    <button type="button" class="btn-close ms-5" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>`;

    private static readonly withHeaderHtmlTemplate =
        `<div class="toast">
            <div class="toast-header">
                <strong class="me-auto">{1}</strong>
                <small class="text-body-secondary">just now</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                {0}
            </div>
        </div>`;

    public static showToast(message: string, header?: string | null, duration = 5000): void {
        const container = document.querySelector(".toast-container > div") as HTMLElement;

        const template = header ? this.withHeaderHtmlTemplate : this.noHeaderHtmlTemplate;

        let el: Element = document.createElement("div");
        el.innerHTML = Util.formatString(template, message, header);
        el = el.firstElementChild as Element;
        container?.appendChild(el);

        const toast = new Toast(el, {
            delay: duration,
        });
        el.addEventListener('hidden.bs.toast', () => {
            try {
                el.remove();
                toast.dispose();
            } finally {
                if (container!.children.length === 0) {
                    container.parentElement!.style.display = "none";
                }
            }
        });
        container.parentElement!.style.display = "block";
        toast.show();

        setTimeout(() => container!.scrollTop = container!.scrollHeight, 100);
    }
}
