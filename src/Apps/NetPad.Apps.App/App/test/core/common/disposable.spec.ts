import {WithDisposables} from "@common/disposable";

describe("WithDisposables", () => {
    test("should dispose disposables", () => {
        const output: string[] = [];
        const obj = new DisposableObject();
        obj.test("dispose1", output);
        obj.test("dispose2", output);

        obj.dispose();

        expect(output.length).toBe(2);
        expect(output[0]).toBe("dispose1");
        expect(output[1]).toBe("dispose2");
    });
});

class DisposableObject extends WithDisposables {
    public test(str: string, output: string[]) {
        this.addDisposable(() => output.push(str));
    }
}
