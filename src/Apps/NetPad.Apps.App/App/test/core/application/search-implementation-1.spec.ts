﻿import {SearchImplementation1} from "@application/find-text-box/search-implementations/search-implementation-1";

const runTest = (initialHtml: string, searchForText: string, expectedHtml: string) => {
    const fragment = document.createDocumentFragment();
    const p = document.createElement("p");
    p.innerHTML = initialHtml;
    fragment.appendChild(p);

    const searchImplementation = new SearchImplementation1();

    const results = searchImplementation.search(fragment as unknown as HTMLElement, searchForText, "p");

    if (initialHtml === expectedHtml)
        expect(results.length).toBe(0);
    else
        expect(results.length).toBeGreaterThan(0);

    expect(p.innerHTML).toBe(expectedHtml);
};

// Used to simulate how text is rendered in HTML as generated by NetPad
const toHtml = (text: string) => {
    let html = "";
    let insideXml = false;

    for (let char of text) {
        if (char === "<") {
            html += char;
            insideXml = true;
            continue;
        }

        if (char === ">") {
            html += char;
            insideXml = false;
            continue;
        }

        if (insideXml) {
            html += char;
            continue;
        }

        if (char === " ") char = "&nbsp;";
        else if (char === "&") char = "&amp;";
        else if (char === "<") char = "&lt;";
        else if (char === ">") char = "&gt;";
        else if (char === "\n") char = "<br>";

        html += char;
    }

    return html;
};

describe.each([
    ["Should not match anything", "none", `Should not match anything`],
    ["My name is Joe", "name", `My <span class="text-search-result">name</span> is Joe`],
    ["Names are important", "name", `<span class="text-search-result">Name</span>s are important`],
    ["I like my namE", "nAme", `I like my <span class="text-search-result">namE</span>`],
])("Find Text Search implementation 1 - Simple Search", (initialHtml: string, searchForText: string, expectedHtml: string) => {
    test(`searching "${initialHtml}" for "${searchForText}" results in "${expectedHtml}"`, () => {
        runTest(toHtml(initialHtml), searchForText, toHtml(expectedHtml));
    });
});

describe.each([
    ["My name is Joe", "name is", `My <span class="text-search-result">name is</span> Joe`],
    [" My name is Joe", " my", `<span class="text-search-result"> My</span> name is Joe`],
    ["My name is", " ", `My<span class="text-search-result"> </span>name<span class="text-search-result"> </span>is`],
    ["My name is Beck", "b", `My name is <span class="text-search-result">B</span>eck`],
    ["My \n name is Beck", "name", `My \n <span class="text-search-result">name</span> is Beck`],
])("Find Text Search implementation 1 - Spaces & Breaks", (initialHtml: string, searchForText: string, expectedHtml: string) => {
    test(`searching "${initialHtml}" for "${searchForText}" results in "${expectedHtml}"`, () => {
        runTest(toHtml(initialHtml), searchForText, toHtml(expectedHtml));
    });
});

describe.each([
    ["I like apples & oranges", "apples & oranges", `I like <span class="text-search-result">apples & oranges</span>`],
    ["I like apples & oranges", "apples &amp; oranges", `I like apples & oranges`],
    [`I like "apples" & 'oranges'`, `"apples" & 'oranges'`, `I like <span class="text-search-result">"apples" & 'oranges'</span>`],
    [`I like "apples" & 'oranges'`, `"`, `I like <span class="text-search-result">"</span>apples<span class="text-search-result">"</span> & 'oranges'`],
    [`I like "apples" & 'oranges'`, `'`, `I like "apples" & <span class="text-search-result">'</span>oranges<span class="text-search-result">'</span>`],
    ["1&nbsp;is&nbsp;&lt;&nbsp;than&nbsp;10", "is < than", `1&nbsp;<span class="text-search-result">is&nbsp;&lt;&nbsp;than</span>&nbsp;10`],
    ["10&nbsp;is&nbsp;&gt;&nbsp;than&nbsp;1", "is > than", `10&nbsp;<span class="text-search-result">is&nbsp;&gt;&nbsp;than</span>&nbsp;1`],
])("Find Text Search implementation 1 - Special Characters/Strings", (initialHtml: string, searchForText: string, expectedHtml: string) => {
    test(`searching "${initialHtml}" for "${searchForText}" results in "${expectedHtml}"`, () => {
        const skipLessAndGreaterThanHtmlConversion = initialHtml.indexOf("&lt;") >= 0 || initialHtml.indexOf("&gt;") >= 0;
        runTest(
            skipLessAndGreaterThanHtmlConversion ? initialHtml : toHtml(initialHtml),
            searchForText,
            skipLessAndGreaterThanHtmlConversion ? expectedHtml : toHtml(expectedHtml));
    });
});

describe.each([
    ["My name is Joe <div>some text</div>", "name", "My name is Joe <div>some text</div>"],
    ["My name <div>some text</div> is Joe", "name", "My name <div>some text</div> is Joe"],
])("Find Text Search implementation 1 - Text alongside other elements cannot be searched",
    (initialHtml: string, searchForText: string, expectedHtml: string) => {
        test(`searching "${initialHtml}" for "${searchForText}" results in "${expectedHtml}"`, () => {
            runTest(toHtml(initialHtml), searchForText, toHtml(expectedHtml));
        });
    });
