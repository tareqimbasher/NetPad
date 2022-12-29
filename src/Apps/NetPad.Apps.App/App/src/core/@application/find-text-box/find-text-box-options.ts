export class FindTextBoxOptions {
    /**
     * Initializes a new instance of FindTextBoxOptions.
     * @param rootElement The element that contains the text/HTML that will be searched.
     * @param searchableElementsQuerySelector A selector for all searchable elements within the rootElement.
     */
    constructor(
        public rootElement: HTMLElement,
        public searchableElementsQuerySelector: string) {
    }
}
