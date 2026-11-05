import {csharpTypeName} from "../../../../../../src/windows/main/panes/explorer/data-connections/csharp-type-name";

describe("csharpTypeName", () => {
    it.each([
        ["Int32", "int"],
        ["String", "string"],
        ["Boolean", "bool"],
        ["Single", "float"],
        ["UInt64", "ulong"],
        ["Object", "object"],
    ])("maps %s to its keyword %s", (clr, keyword) => {
        expect(csharpTypeName(clr)).toBe(keyword);
    });

    it("keeps an array suffix", () => {
        expect(csharpTypeName("Byte[]")).toBe("byte[]");
        expect(csharpTypeName("Int32[][]")).toBe("int[][]");
    });

    it("keeps a nullable suffix", () => {
        expect(csharpTypeName("Int32?")).toBe("int?");
        expect(csharpTypeName("DateTime?")).toBe("DateTime?");
    });

    it.each(["DateTime", "Guid", "TimeSpan", "Nullable`1"])("returns %s unchanged", name => {
        expect(csharpTypeName(name)).toBe(name);
    });

    it("does not treat Object.prototype members as keywords", () => {
        expect(csharpTypeName("constructor")).toBe("constructor");
    });
});
