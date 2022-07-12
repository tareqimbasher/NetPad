import {languages, editor, CancellationToken, Range} from "monaco-editor";
import {SemanticHighlightRequest, Range as ApiRange, Point, SemanticHighlightResponse} from "./api";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "./omnisharp-service";
import {Util} from "@common";

export class OmnisharpSemanticTokensProvider implements languages.DocumentSemanticTokensProvider, languages.DocumentRangeSemanticTokensProvider {
    private readonly legend: languages.SemanticTokensLegend;

    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
        this.legend = {
            tokenTypes: tokenTypes,
            tokenModifiers: tokenModifiers
        };
    }

    public getLegend(): languages.SemanticTokensLegend {
        return this.legend;
    }

    public provideDocumentSemanticTokens(model: editor.ITextModel, lastResultId: string | null, token: CancellationToken): languages.ProviderResult<languages.SemanticTokens | languages.SemanticTokensEdits> {
        return this.provideSemanticTokens(model, null, token);
    }

    public provideDocumentRangeSemanticTokens(model: editor.ITextModel, range: Range, token: CancellationToken): languages.ProviderResult<languages.SemanticTokens> {
        return this.provideSemanticTokens(model, range, token);
    }

    public releaseDocumentSemanticTokens(resultId: string | undefined): void {
    }

    private async provideSemanticTokens(model: editor.ITextModel, range: Range | null | undefined, cancellationToken: CancellationToken) {
        const scriptId = EditorUtil.getScriptId(model);

        const request = new SemanticHighlightRequest();
        if (range) {
            request.range = new ApiRange({
                start: new Point({line: range.startLineNumber, column: range.startColumn}),
                end: new Point({line: range.endLineNumber, column: range.endColumn})
            });
        }

        if (cancellationToken.isCancellationRequested) {
            return null;
        }

        const versionBeforeRequest = model.getVersionId();

        let response: SemanticHighlightResponse;
        let tries = 0;

        // Sometimes OmniSharp will return no semantic highlights immediately after it is started.
        // It seems that it needs a bit more time to initialize once it starts before it has the
        // semantic highlights to return.
        do {
            if (++tries > 1) {
                await Util.delay(1000);
            }

            response = await this.omnisharpService.getSemanticHighlights(scriptId, request);
        }
        while (
            (!response || !response.spans || !response.spans.length)
            && tries < 3
            && !cancellationToken.isCancellationRequested
            && versionBeforeRequest === model.getVersionId())

        const versionAfterRequest = model.getVersionId();

        if (versionBeforeRequest !== versionAfterRequest) {
            // Cannot convert result's offsets to (line;col) values correctly
            // a new request will come in soon...
            //
            // Here we cannot return null, because returning null would remove all semantic tokens.
            // We must throw to indicate that the semantic tokens should not be removed.
            throw new Error("busy");
        }

        if (cancellationToken.isCancellationRequested || !response || !response.spans) {
            return null;
        }

        return this.processResponse(response, model);
    }

    private processResponse(response: SemanticHighlightResponse, model: editor.ITextModel) {
        const data: number[] = [];

        let prevLine = 0;
        let prevChar = 0;

        const createDeltaEncodedTokenData = (lineNumber: number, colPosition: number, length: number, tokenTypeIndex: number, modifierIndex: number) => {
            const arr = [
                // Line number (0-indexed, and offset from the previous line)
                lineNumber - prevLine,
                // Column position (0-indexed, and offset from the previous column, unless this is the beginning of a new line)
                prevLine === lineNumber ? colPosition - prevChar : colPosition,
                // Token length
                length,
                tokenTypeIndex,
                modifierIndex
            ];

            prevLine = lineNumber
            prevChar = colPosition

            return arr;
        };

        for (const span of response.spans) {
            const tokenType = tokenTypeMap[SemanticHighlightClassification[span.type]];
            if (tokenType === undefined) {
                continue;
            }

            let tokenModifiers = span.modifiers.reduce((modifiers, modifier) => modifiers + tokenModifierMap[SemanticHighlightModifier[modifier]], 0);

            // We could add a separate classification for constants but they are
            // supported as a readonly variable. Until we start getting more complete
            // modifiers from the highlight service we can add the readonly modifier here.
            if (span.type === SemanticHighlightClassification[SemanticHighlightClassification.ConstantName]) {
                tokenModifiers += 2 ** DefaultTokenModifier.readonly;
            }

            const spanRange = new Range(span.startLine, span.startColumn, span.endLine, span.endColumn);
            const isMultiLineRange = spanRange.startLineNumber !== spanRange.endLineNumber;

            for (let iLine = spanRange.startLineNumber; iLine <= spanRange.endLineNumber; iLine++) {
                // If we are on the "range.StartLineNumber", use the start column, otherwise we are in a range that
                // spans multiple lines, and the start column should be the first char since its a continuation
                // of the previous line
                const startColumn = iLine === spanRange.startLineNumber ? spanRange.startColumn : 0;

                let length = 0;

                if (!isMultiLineRange) {
                    length = spanRange.endColumn - spanRange.startColumn;
                } else {
                    // First line
                    if (iLine === spanRange.startLineNumber) {
                        length = model.getLineLength(iLine + 1) - spanRange.startColumn;
                    }
                    // Line in the middle (not first or last line)
                    else if (iLine > spanRange.startLineNumber && iLine < spanRange.endLineNumber) {
                        length = model.getLineLength(iLine + 1);
                    }
                    // Last line
                    else {
                        length = spanRange.endColumn;
                    }
                }

                const arr = createDeltaEncodedTokenData(
                    iLine,
                    startColumn,
                    length,
                    tokenType,
                    tokenModifiers);

                data.push(...arr);

                // console.warn(
                //     spanRange,
                //     span.type,
                //     tokenType,
                //     tokenTypes[tokenType],
                //     model.getValueInRange(new Range(iLine + 1, startColumn + 1, iLine + 1, startColumn + length + 1)),
                //     length);
                // console.info(arr);
            }
        }

        return {
            data: new Uint32Array(data),
            resultId: null
        };
    }
}


// The default TokenTypes defined by VS Code https://github.com/microsoft/vscode/blob/master/src/vs/platform/theme/common/tokenClassificationRegistry.ts#L393
enum DefaultTokenType {
    comment,
    string,
    keyword,
    number,
    regexp,
    operator,
    namespace,
    type,
    struct,
    class,
    interface,
    enum,
    typeParameter,
    function,
    member,
    macro,
    variable,
    parameter,
    property,
    enumMember,
    event,
    label,
}

enum CustomTokenType {
    plainKeyword = DefaultTokenType.label + 1,
    controlKeyword,
    operatorOverloaded,
    preprocessorKeyword,
    preprocessorText,
    excludedCode,
    punctuation,
    stringVerbatim,
    stringEscapeCharacter,
    delegate,
    module,
    extensionMethod,
    field,
    local,
    xmlDocCommentAttributeName,
    xmlDocCommentAttributeQuotes,
    xmlDocCommentAttributeValue,
    xmlDocCommentCDataSection,
    xmlDocCommentComment,
    xmlDocCommentDelimiter,
    xmlDocCommentEntityReference,
    xmlDocCommentName,
    xmlDocCommentProcessingInstruction,
    xmlDocCommentText,
    regexComment,
    regexCharacterClass,
    regexAnchor,
    regexQuantifier,
    regexGrouping,
    regexAlternation,
    regexSelfEscapedCharacter,
    regexOtherEscape,
}

// The default TokenModifiers defined by VS Code https://github.com/microsoft/vscode/blob/master/src/vs/platform/theme/common/tokenClassificationRegistry.ts#L393
enum DefaultTokenModifier {
    declaration,
    static,
    abstract,
    deprecated,
    modification,
    async,
    readonly,
}

// All classifications from Roslyn's ClassificationTypeNames https://github.com/dotnet/roslyn/blob/master/src/Workspaces/Core/Portable/Classification/ClassificationTypeNames.cs
// Keep in sync with omnisharp-roslyn's SemanticHighlightClassification
enum SemanticHighlightClassification {
    Comment,
    ExcludedCode,
    Identifier,
    Keyword,
    ControlKeyword,
    NumericLiteral,
    Operator,
    OperatorOverloaded,
    PreprocessorKeyword,
    StringLiteral,
    WhiteSpace,
    Text,
    StaticSymbol,
    PreprocessorText,
    Punctuation,
    VerbatimStringLiteral,
    StringEscapeCharacter,
    ClassName,
    DelegateName,
    EnumName,
    InterfaceName,
    ModuleName,
    StructName,
    TypeParameterName,
    FieldName,
    EnumMemberName,
    ConstantName,
    LocalName,
    ParameterName,
    MethodName,
    ExtensionMethodName,
    PropertyName,
    EventName,
    NamespaceName,
    LabelName,
    XmlDocCommentAttributeName,
    XmlDocCommentAttributeQuotes,
    XmlDocCommentAttributeValue,
    XmlDocCommentCDataSection,
    XmlDocCommentComment,
    XmlDocCommentDelimiter,
    XmlDocCommentEntityReference,
    XmlDocCommentName,
    XmlDocCommentProcessingInstruction,
    XmlDocCommentText,
    XmlLiteralAttributeName,
    XmlLiteralAttributeQuotes,
    XmlLiteralAttributeValue,
    XmlLiteralCDataSection,
    XmlLiteralComment,
    XmlLiteralDelimiter,
    XmlLiteralEmbeddedExpression,
    XmlLiteralEntityReference,
    XmlLiteralName,
    XmlLiteralProcessingInstruction,
    XmlLiteralText,
    RegexComment,
    RegexCharacterClass,
    RegexAnchor,
    RegexQuantifier,
    RegexGrouping,
    RegexAlternation,
    RegexText,
    RegexSelfEscapedCharacter,
    RegexOtherEscape,
}

enum SemanticHighlightModifier {
    Static
}


const tokenTypes: string[] = [];
tokenTypes[DefaultTokenType.comment] = "comment";
tokenTypes[DefaultTokenType.string] = "string";
tokenTypes[DefaultTokenType.keyword] = "keyword";
tokenTypes[DefaultTokenType.number] = "number";
tokenTypes[DefaultTokenType.regexp] = "regexp";
tokenTypes[DefaultTokenType.operator] = "operator";
tokenTypes[DefaultTokenType.namespace] = "namespace";
tokenTypes[DefaultTokenType.type] = "type";
tokenTypes[DefaultTokenType.struct] = "struct";
tokenTypes[DefaultTokenType.class] = "class";
tokenTypes[DefaultTokenType.interface] = "interface";
tokenTypes[DefaultTokenType.enum] = "enum";
tokenTypes[DefaultTokenType.typeParameter] = "typeParameter";
tokenTypes[DefaultTokenType.function] = "function";
tokenTypes[DefaultTokenType.member] = 'member';
tokenTypes[DefaultTokenType.macro] = "macro";
tokenTypes[DefaultTokenType.variable] = "variable";
tokenTypes[DefaultTokenType.parameter] = "parameter";
tokenTypes[DefaultTokenType.property] = "property";
tokenTypes[DefaultTokenType.enumMember] = 'enumMember';
tokenTypes[DefaultTokenType.event] = 'event';
tokenTypes[DefaultTokenType.label] = 'label';
tokenTypes[CustomTokenType.plainKeyword] = "plainKeyword";
tokenTypes[CustomTokenType.controlKeyword] = "controlKeyword";
tokenTypes[CustomTokenType.operatorOverloaded] = "operatorOverloaded";
tokenTypes[CustomTokenType.preprocessorKeyword] = "preprocessorKeyword";
tokenTypes[CustomTokenType.preprocessorText] = "preprocessorText";
tokenTypes[CustomTokenType.excludedCode] = "excludedCode";
tokenTypes[CustomTokenType.punctuation] = "punctuation";
tokenTypes[CustomTokenType.stringVerbatim] = "stringVerbatim";
tokenTypes[CustomTokenType.stringEscapeCharacter] = "stringEscapeCharacter";
tokenTypes[CustomTokenType.delegate] = "delegate";
tokenTypes[CustomTokenType.module] = "module";
tokenTypes[CustomTokenType.extensionMethod] = "extensionMethod";
tokenTypes[CustomTokenType.field] = "field";
tokenTypes[CustomTokenType.local] = "local";
tokenTypes[CustomTokenType.xmlDocCommentAttributeName] = "xmlDocCommentAttributeName";
tokenTypes[CustomTokenType.xmlDocCommentAttributeQuotes] = "xmlDocCommentAttributeQuotes";
tokenTypes[CustomTokenType.xmlDocCommentAttributeValue] = "xmlDocCommentAttributeValue";
tokenTypes[CustomTokenType.xmlDocCommentCDataSection] = "xmlDocCommentCDataSection";
tokenTypes[CustomTokenType.xmlDocCommentComment] = "xmlDocCommentComment";
tokenTypes[CustomTokenType.xmlDocCommentDelimiter] = "xmlDocCommentDelimiter";
tokenTypes[CustomTokenType.xmlDocCommentEntityReference] = "xmlDocCommentEntityReference";
tokenTypes[CustomTokenType.xmlDocCommentName] = "xmlDocCommentName";
tokenTypes[CustomTokenType.xmlDocCommentProcessingInstruction] = "xmlDocCommentProcessingInstruction";
tokenTypes[CustomTokenType.xmlDocCommentText] = "xmlDocCommentText";
tokenTypes[CustomTokenType.regexComment] = "regexComment";
tokenTypes[CustomTokenType.regexCharacterClass] = "regexCharacterClass";
tokenTypes[CustomTokenType.regexAnchor] = "regexAnchor";
tokenTypes[CustomTokenType.regexQuantifier] = "regexQuantifier";
tokenTypes[CustomTokenType.regexGrouping] = "regexGrouping";
tokenTypes[CustomTokenType.regexAlternation] = "regexAlternation";
tokenTypes[CustomTokenType.regexSelfEscapedCharacter] = "regexSelfEscapedCharacter";
tokenTypes[CustomTokenType.regexOtherEscape] = "regexOtherEscape";

const tokenModifiers: string[] = [];
tokenModifiers[DefaultTokenModifier.declaration] = 'declaration';
tokenModifiers[DefaultTokenModifier.static] = 'static';
tokenModifiers[DefaultTokenModifier.abstract] = 'abstract';
tokenModifiers[DefaultTokenModifier.deprecated] = 'deprecated';
tokenModifiers[DefaultTokenModifier.modification] = 'modification';
tokenModifiers[DefaultTokenModifier.async] = 'async';
tokenModifiers[DefaultTokenModifier.readonly] = 'readonly';

const tokenTypeMap: number[] = [];
tokenTypeMap[SemanticHighlightClassification.Comment] = DefaultTokenType.comment;
tokenTypeMap[SemanticHighlightClassification.ExcludedCode] = CustomTokenType.excludedCode;
tokenTypeMap[SemanticHighlightClassification.Identifier] = DefaultTokenType.variable;
tokenTypeMap[SemanticHighlightClassification.Keyword] = CustomTokenType.plainKeyword;
tokenTypeMap[SemanticHighlightClassification.ControlKeyword] = CustomTokenType.controlKeyword;
tokenTypeMap[SemanticHighlightClassification.NumericLiteral] = DefaultTokenType.number;
tokenTypeMap[SemanticHighlightClassification.Operator] = DefaultTokenType.operator;
tokenTypeMap[SemanticHighlightClassification.OperatorOverloaded] = CustomTokenType.operatorOverloaded;
tokenTypeMap[SemanticHighlightClassification.PreprocessorKeyword] = CustomTokenType.preprocessorKeyword;
tokenTypeMap[SemanticHighlightClassification.StringLiteral] = DefaultTokenType.string;
tokenTypeMap[SemanticHighlightClassification.WhiteSpace] = undefined;
tokenTypeMap[SemanticHighlightClassification.Text] = undefined;
tokenTypeMap[SemanticHighlightClassification.StaticSymbol] = undefined;
tokenTypeMap[SemanticHighlightClassification.PreprocessorText] = CustomTokenType.preprocessorText;
tokenTypeMap[SemanticHighlightClassification.Punctuation] = CustomTokenType.punctuation;
tokenTypeMap[SemanticHighlightClassification.VerbatimStringLiteral] = CustomTokenType.stringVerbatim;
tokenTypeMap[SemanticHighlightClassification.StringEscapeCharacter] = CustomTokenType.stringEscapeCharacter;
tokenTypeMap[SemanticHighlightClassification.ClassName] = DefaultTokenType.class;
tokenTypeMap[SemanticHighlightClassification.DelegateName] = CustomTokenType.delegate;
tokenTypeMap[SemanticHighlightClassification.EnumName] = DefaultTokenType.enum;
tokenTypeMap[SemanticHighlightClassification.InterfaceName] = DefaultTokenType.interface;
tokenTypeMap[SemanticHighlightClassification.ModuleName] = CustomTokenType.module;
tokenTypeMap[SemanticHighlightClassification.StructName] = DefaultTokenType.struct;
tokenTypeMap[SemanticHighlightClassification.TypeParameterName] = DefaultTokenType.typeParameter;
tokenTypeMap[SemanticHighlightClassification.FieldName] = CustomTokenType.field;
tokenTypeMap[SemanticHighlightClassification.EnumMemberName] = DefaultTokenType.enumMember;
tokenTypeMap[SemanticHighlightClassification.ConstantName] = DefaultTokenType.variable;
tokenTypeMap[SemanticHighlightClassification.LocalName] = CustomTokenType.local;
tokenTypeMap[SemanticHighlightClassification.ParameterName] = DefaultTokenType.parameter;
tokenTypeMap[SemanticHighlightClassification.MethodName] = DefaultTokenType.member;
tokenTypeMap[SemanticHighlightClassification.ExtensionMethodName] = CustomTokenType.extensionMethod;
tokenTypeMap[SemanticHighlightClassification.PropertyName] = DefaultTokenType.property;
tokenTypeMap[SemanticHighlightClassification.EventName] = DefaultTokenType.event;
tokenTypeMap[SemanticHighlightClassification.NamespaceName] = DefaultTokenType.namespace;
tokenTypeMap[SemanticHighlightClassification.LabelName] = DefaultTokenType.label;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentAttributeName] = CustomTokenType.xmlDocCommentAttributeName;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentAttributeQuotes] = CustomTokenType.xmlDocCommentAttributeQuotes;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentAttributeValue] = CustomTokenType.xmlDocCommentAttributeValue;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentCDataSection] = CustomTokenType.xmlDocCommentCDataSection;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentComment] = CustomTokenType.xmlDocCommentComment;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentDelimiter] = CustomTokenType.xmlDocCommentDelimiter;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentEntityReference] = CustomTokenType.xmlDocCommentEntityReference;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentName] = CustomTokenType.xmlDocCommentName;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentProcessingInstruction] = CustomTokenType.xmlDocCommentProcessingInstruction;
tokenTypeMap[SemanticHighlightClassification.XmlDocCommentText] = CustomTokenType.xmlDocCommentText;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralAttributeName] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralAttributeQuotes] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralAttributeValue] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralCDataSection] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralComment] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralDelimiter] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralEmbeddedExpression] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralEntityReference] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralName] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralProcessingInstruction] = undefined;
tokenTypeMap[SemanticHighlightClassification.XmlLiteralText] = undefined;
tokenTypeMap[SemanticHighlightClassification.RegexComment] = CustomTokenType.regexComment;
tokenTypeMap[SemanticHighlightClassification.RegexCharacterClass] = CustomTokenType.regexCharacterClass;
tokenTypeMap[SemanticHighlightClassification.RegexAnchor] = CustomTokenType.regexAnchor;
tokenTypeMap[SemanticHighlightClassification.RegexQuantifier] = CustomTokenType.regexQuantifier;
tokenTypeMap[SemanticHighlightClassification.RegexGrouping] = CustomTokenType.regexGrouping;
tokenTypeMap[SemanticHighlightClassification.RegexAlternation] = CustomTokenType.regexAlternation;
tokenTypeMap[SemanticHighlightClassification.RegexText] = DefaultTokenType.regexp;
tokenTypeMap[SemanticHighlightClassification.RegexSelfEscapedCharacter] = CustomTokenType.regexSelfEscapedCharacter;
tokenTypeMap[SemanticHighlightClassification.RegexOtherEscape] = CustomTokenType.regexOtherEscape;

const tokenModifierMap: number[] = [];
tokenModifierMap[SemanticHighlightModifier.Static] = 2 ** DefaultTokenModifier.static;

