import "@common/globals";

describe("nameof", () => {
    test("resolves class property name", () => {
        const result = nameof<TestClass>("name");
        expect(result).toBe("name");
    });

    test("resolves interface property name", () => {
        const result = nameof<TestInterface>("name");
        expect(result).toBe("name");
    });

    test("resolves class name", () => {
        const result = nameof(TestClass);
        expect(result).toBe("TestClass");
    });

    test("resolves class with ctor params name", () => {
        const result = nameof(TestClass2);
        expect(result).toBe("TestClass2");
    });

    test("resolves property name from class object", () => {
        const obj = new TestClass();
        const result = nameof(obj, "age");
        expect(result).toBe("age");
    });

    test("resolves property name from interface object", () => {
        const obj: TestInterface = {name: "John", age: 5};
        const result = nameof(obj, "age");
        expect(result).toBe("age");
    });
});

class TestClass {
    public name = "John";
    public age = 10;
}

class TestClass2 {
    constructor(public name: string) {
    }

    public age = 10;
}

interface TestInterface {
    name: string;
    age: number;
}
