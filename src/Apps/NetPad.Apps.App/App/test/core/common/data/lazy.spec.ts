import {Lazy} from "@common/data/lazy";

describe("Lazy", () => {
    test("should not be initialized before value is accessed", () => {
        const lazy = new Lazy(() => 42);
        expect(lazy.isInitialized).toBe(false);
    });

    test("should initialize when value is accessed", () => {
        const lazy = new Lazy(() => 42);
        const value = lazy.value;

        expect(lazy.isInitialized).toBe(true);
        expect(value).toBe(42);
    });

    test("should call initializer only once", () => {
        const initializer = jest.fn(() => "hello");
        const lazy = new Lazy(initializer);

        lazy.value;
        lazy.value;
        lazy.value;

        expect(initializer).toHaveBeenCalledTimes(1);
    });

    test("should cache and return the same value on subsequent accesses", () => {
        let counter = 0;
        const lazy = new Lazy(() => ++counter);

        expect(lazy.value).toBe(1);
        expect(lazy.value).toBe(1);
        expect(lazy.value).toBe(1);
    });

    test("should work with object values", () => {
        const obj = {name: "test"};
        const lazy = new Lazy(() => obj);

        expect(lazy.value).toBe(obj);
    });

    test("should work with null as a valid initialized value", () => {
        const lazy = new Lazy<string | null>(() => null);
        const value = lazy.value;

        expect(lazy.isInitialized).toBe(true);
        expect(value).toBeNull();
    });

    test("should work with undefined as initialized value", () => {
        const initializer = jest.fn(() => undefined);
        const lazy = new Lazy<undefined>(initializer);

        lazy.value;

        // _isInitialized flag is set to true, so the initializer is not called again
        expect(lazy.isInitialized).toBe(true);
        expect(initializer).toHaveBeenCalledTimes(1);
    });
});
