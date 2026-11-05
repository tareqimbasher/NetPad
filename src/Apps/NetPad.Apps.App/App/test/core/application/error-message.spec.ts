import {ApiException} from "@application/api";
import {errorMessageOf} from "@application/error-message";

const apiException = (body: string) =>
    new ApiException("An unexpected server error occurred.", 500, body, {}, null);

describe("errorMessageOf", () => {
    test("a failed request gives the backend's message", () => {
        expect(errorMessageOf(apiException(JSON.stringify({message: "Feed unavailable."})), "Fallback"))
            .toBe("Feed unavailable.");
    });

    test("a failed request whose body has no message gives the fallback", () => {
        expect(errorMessageOf(apiException(JSON.stringify({details: "x"})), "Fallback")).toBe("Fallback");
    });

    test("a failed request whose body isn't JSON gives the fallback", () => {
        expect(errorMessageOf(apiException("<html>Bad gateway</html>"), "Fallback")).toBe("Fallback");
    });

    test("an error gives its own message", () => {
        expect(errorMessageOf(new Error("Failed to fetch"), "Fallback")).toBe("Failed to fetch");
    });

    test("an error with an empty message gives the fallback", () => {
        expect(errorMessageOf(new Error(""), "Fallback")).toBe("Fallback");
    });

    test("a thrown string gives the fallback", () => {
        expect(errorMessageOf("boom", "Fallback")).toBe("Fallback");
    });
});
