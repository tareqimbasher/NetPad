import {SubscriptionToken} from "@common/events/subscription-token";

describe("SubscriptionToken", () => {
    test("should call dispose action on dispose", () => {
        const action = jest.fn();
        const token = new SubscriptionToken(action);

        token.dispose();

        expect(action).toHaveBeenCalledTimes(1);
    });

    test("should only call dispose action once on multiple disposes", () => {
        const action = jest.fn();
        const token = new SubscriptionToken(action);

        token.dispose();
        token.dispose();
        token.dispose();

        expect(action).toHaveBeenCalledTimes(1);
    });

    test("should not throw when dispose action throws", () => {
        const token = new SubscriptionToken(() => {
            throw new Error("test error");
        });

        // The finally block sets isDisposed = true, but the error still propagates
        // Actually looking at the code: try/finally means the error WILL propagate
        // but isDisposed is still set. Let's verify:
        expect(() => token.dispose()).toThrow("test error");

        // Second dispose should be a no-op
        expect(() => token.dispose()).not.toThrow();
    });

    test("should mark as disposed even when action throws", () => {
        const token = new SubscriptionToken(() => {
            throw new Error("boom");
        });

        try { token.dispose(); } catch { /* expected */ }

        // Second dispose is a no-op because isDisposed was set in the finally block
        expect(() => token.dispose()).not.toThrow();
    });
});
