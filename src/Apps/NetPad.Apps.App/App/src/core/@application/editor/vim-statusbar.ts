import {IStatusbarItemNew} from "../../../windows/main/statusbar/istatusbar-item";
import {initVimMode} from "monaco-vim";
import {ITextEditorService} from "@application/editor/itext-editor-service";
import {editor} from "monaco-editor";

export class VimStatusbar implements IStatusbarItemNew {
    public readonly position = "left";

    constructor(public readonly element: HTMLElement, @ITextEditorService private readonly textEditorService: ITextEditorService) {
    }

    public attached() {
        setTimeout(() => {
            const activeEditor = this.textEditorService.active;
            if (activeEditor) {
                initVimMode(activeEditor.monaco, this.element, VimLibStatusBar);
            } else {
                console.warn("No editor initialized");
            }
        }, 500);
    }
}

interface IVimStatusbarInputOptions {
    selectValueOnOpen?: boolean,
    closeOnBlur?: boolean,
    closeOnEnter?: boolean,
    value?: string,
    onKeyUp?: (e: KeyboardEvent, v: any, f: () => void) => boolean
    onKeyDown?: (e: KeyboardEvent, v: any, f: () => void) => boolean
    onKeyInput?: (p: any) => boolean
}

interface IVimStatusbarInput {
    node: HTMLInputElement
    options?: IVimStatusbarInputOptions,
    callback?: (v: any) => void,
}

interface IVimModeEvent {
    mode: string;
    subMode?: string;
}

class VimLibStatusBar {
    private readonly modeInfoNode: HTMLElement;
    private readonly secInfoNode: HTMLElement;
    private readonly keyInfoNode: HTMLElement;
    private readonly notifNode: HTMLElement;
    private notifTimeout: NodeJS.Timeout | null | undefined;
    private input: IVimStatusbarInput | null | undefined;

    constructor(
        private readonly node: HTMLElement,
        private readonly editor: editor.IStandaloneCodeEditor,
        private readonly sanitizer: ((html: string) => string) | null = null
    ) {
        this.modeInfoNode = document.createElement("span");
        this.modeInfoNode.className = "vim-mode-info";

        this.secInfoNode = document.createElement("span");
        this.secInfoNode.className = "vim-sec-info";

        this.notifNode = document.createElement("span");
        this.notifNode.className = "vim-notification";

        this.keyInfoNode = document.createElement("span");
        this.keyInfoNode.className = "vim-key-info";
        // this.keyInfoNode.setAttribute("style", "float: right");

        this.node.appendChild(this.modeInfoNode);
        this.node.appendChild(this.secInfoNode);
        this.node.appendChild(this.notifNode);
        this.node.appendChild(this.keyInfoNode);
        this.toggleVisibility(false);
    }

    setMode(ev: IVimModeEvent) {
        if (ev.mode === "visual") {
            if (ev.subMode === "linewise") {
                this.setText("--VISUAL LINE--");
            } else if (ev.subMode === "blockwise") {
                this.setText("--VISUAL BLOCK--");
            } else {
                this.setText("--VISUAL--");
            }
            return;
        }

        this.setText(`--${ev.mode.toUpperCase()}--`);
    }

    setKeyBuffer(key: string) {
        this.keyInfoNode.textContent = key;
    }

    setSec(text?: string, callback?: (value: any) => void, options?: { selectValueOnOpen: boolean, value: string }) {
        this.notifNode.textContent = "";
        if (text === undefined) {
            return this.closeInput;
        }

        this.setInnerHtml_(this.secInfoNode, text);
        const input = this.secInfoNode.querySelector("input");

        if (input) {
            input.focus();
            this.input = {
                callback,
                options,
                node: input,
            };

            if (options) {
                if (options.selectValueOnOpen) {
                    input.select();
                }

                if (options.value) {
                    input.value = options.value;
                }
            }

            this.addInputListeners();
        }

        return this.closeInput;
    }

    setText(text: string) {
        this.modeInfoNode.textContent = text;
    }

    toggleVisibility(show: boolean) {
        if (show) {
            this.node.classList.remove("d-none");
        } else {
            this.node.classList.add("d-none");
        }

        if (this.input) {
            this.removeInputListeners();
        }

        if (this.notifTimeout) {
            clearInterval(this.notifTimeout);
            this.notifTimeout = null;
        }
    }

    closeInput = () => {
        this.removeInputListeners();
        this.input = null;
        this.setSec("");

        if (this.editor) {
            this.editor.focus();
        }
    };

    clear = () => {
        this.setInnerHtml_(this.node, "");
    };

    inputKeyUp = (e: KeyboardEvent) => {
        const options = this.input?.options;
        if (options && options.onKeyUp) {
            options.onKeyUp(e, (e.target as HTMLInputElement)?.value, this.closeInput);
        }
    };

    inputKeyInput = (e: Event) => {
        const options = this.input?.options;
        if (options && options.onKeyInput && options.onKeyUp) {
            options.onKeyUp(e as KeyboardEvent, (e.target as HTMLInputElement)?.value, this.closeInput);
        }
    };

    inputBlur = () => {
        const options = this.input?.options;

        if (options?.closeOnBlur) {
            this.closeInput();
        }
    };

    inputKeyDown = (e: KeyboardEvent) => {
        const options = this.input?.options;
        const callback = this.input?.callback;

        if (
            options &&
            options.onKeyDown &&
            options.onKeyDown(e, (e.target as HTMLInputElement)?.value, this.closeInput)
        ) {
            return;
        }

        if (
            e.keyCode === 27 ||
            (options && options.closeOnEnter !== false && e.keyCode == 13)
        ) {
            this.input?.node.blur();
            e.stopPropagation();
            this.closeInput();
        }

        if (e.keyCode === 13 && callback) {
            e.stopPropagation();
            e.preventDefault();
            callback((e.target as HTMLInputElement)?.value);
        }
    };

    addInputListeners() {
        const node = this.input?.node;
        if (node) {
            node.addEventListener("keyup", this.inputKeyUp);
            node.addEventListener("keydown", this.inputKeyDown);
            node.addEventListener("input", this.inputKeyInput);
            node.addEventListener("blur", this.inputBlur);
        }
    }

    removeInputListeners() {
        if (!this.input || !this.input.node) {
            return;
        }

        const {node} = this.input;
        node.removeEventListener("keyup", this.inputKeyUp);
        node.removeEventListener("keydown", this.inputKeyDown);
        node.removeEventListener("input", this.inputKeyInput);
        node.removeEventListener("blur", this.inputBlur);
    }

    showNotification(text: string) {
        const sp = document.createElement("span");
        this.setInnerHtml_(sp, text);
        this.notifNode.textContent = sp.textContent;
        this.notifTimeout = setTimeout(() => {
            this.notifNode.textContent = "";
        }, 5000);
    }

    setInnerHtml_(element: HTMLElement, htmlContents: any) {
        // Clear out previous contents first.
        while (element.childNodes.length) {
            element.removeChild(element.childNodes[0]);
        }
        if (!htmlContents) {
            return;
        }
        if (this.sanitizer) {
            element.appendChild(this.sanitizer(htmlContents) as any);
        } else {
            element.appendChild(htmlContents);
        }
    }
}
