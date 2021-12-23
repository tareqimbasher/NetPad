import { IDialogDom } from "@aurelia/runtime-html";

export abstract class DialogBase {
    protected constructor(
        @IDialogDom protected readonly dialogDom: IDialogDom
    ) {
        dialogDom.contentHost.classList.add("dialog");
        dialogDom.overlay.style.backgroundColor = "rgba(0, 0, 0, 0.5)";
    }

    public attaching() {
        const animation = this.dialogDom.contentHost.animate([{ opacity: "0" }, { opacity: "1" }], {
            duration: 100,
        });
        return animation.finished;
    }

    public detaching() {
        const animation = this.dialogDom.contentHost.animate([{ opacity: "1" }, { opacity: "0" }], {
            duration: 100,
        });
        return animation.finished;
    }
}
