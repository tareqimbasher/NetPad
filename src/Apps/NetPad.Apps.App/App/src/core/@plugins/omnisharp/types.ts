import {languages} from "monaco-editor";

export module Symbols {
    export module OmniSharpKinds {
        // types
        export const Class = 'class';
        export const Delegate = 'delegate';
        export const Enum = 'enum';
        export const Interface = 'interface';
        export const Struct = 'struct';

        // members
        export const Constant = 'constant';
        export const Constructor = 'constructor';
        export const Destructor = 'destructor';
        export const EnumMember = 'enummember';
        export const Event = 'event';
        export const Field = 'field';
        export const Indexer = 'indexer';
        export const Method = 'method';
        export const Operator = 'operator';
        export const Property = 'property';

        // other
        export const Namespace = 'namespace';
        export const Unknown = 'unknown';
    }


    export module RangeNames {
        export const Attributes = 'attributes';
        export const Full = 'full';
        export const Name = 'name';
    }

    export const kindsMap: { [kind: string]: languages.SymbolKind; } = {};

    kindsMap[OmniSharpKinds.Class] = languages.SymbolKind.Class;
    kindsMap[OmniSharpKinds.Delegate] = languages.SymbolKind.Class;
    kindsMap[OmniSharpKinds.Enum] = languages.SymbolKind.Enum;
    kindsMap[OmniSharpKinds.Interface] = languages.SymbolKind.Interface;
    kindsMap[OmniSharpKinds.Struct] = languages.SymbolKind.Struct;

    kindsMap[OmniSharpKinds.Constant] = languages.SymbolKind.Constant;
    kindsMap[OmniSharpKinds.Destructor] = languages.SymbolKind.Method;
    kindsMap[OmniSharpKinds.EnumMember] = languages.SymbolKind.EnumMember;
    kindsMap[OmniSharpKinds.Event] = languages.SymbolKind.Event;
    kindsMap[OmniSharpKinds.Field] = languages.SymbolKind.Field;
    kindsMap[OmniSharpKinds.Indexer] = languages.SymbolKind.Property;
    kindsMap[OmniSharpKinds.Method] = languages.SymbolKind.Method;
    kindsMap[OmniSharpKinds.Operator] = languages.SymbolKind.Operator;
    kindsMap[OmniSharpKinds.Property] = languages.SymbolKind.Property;

    kindsMap[OmniSharpKinds.Namespace] = languages.SymbolKind.Namespace;
    kindsMap[OmniSharpKinds.Unknown] = languages.SymbolKind.Class;
}

export module SemanticTokens {
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
    export enum DefaultTokenModifier {
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
    export enum SemanticHighlightClassification {
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

    export enum SemanticHighlightModifier {
        Static
    }

    export const tokenTypes: string[] = [];
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

    export const tokenModifiers: string[] = [];
    tokenModifiers[DefaultTokenModifier.declaration] = 'declaration';
    tokenModifiers[DefaultTokenModifier.static] = 'static';
    tokenModifiers[DefaultTokenModifier.abstract] = 'abstract';
    tokenModifiers[DefaultTokenModifier.deprecated] = 'deprecated';
    tokenModifiers[DefaultTokenModifier.modification] = 'modification';
    tokenModifiers[DefaultTokenModifier.async] = 'async';
    tokenModifiers[DefaultTokenModifier.readonly] = 'readonly';

    export const tokenTypeMap: (number | undefined)[] = [];
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

    export const tokenModifierMap: number[] = [];
    tokenModifierMap[SemanticHighlightModifier.Static] = 2 ** DefaultTokenModifier.static;
}
