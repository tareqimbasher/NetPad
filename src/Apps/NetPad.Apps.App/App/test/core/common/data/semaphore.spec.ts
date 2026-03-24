import {Semaphore} from "@common/data/semaphore";

describe("Semaphore", () => {
    test("should allow acquisition up to the limit", async () => {
        const semaphore = new Semaphore(3);

        await semaphore.acquire();
        await semaphore.acquire();
        await semaphore.acquire();

        // All three acquired without blocking
        expect(true).toBe(true);
    });

    test("should block when limit is reached", async () => {
        const semaphore = new Semaphore(1);
        const order: number[] = [];

        await semaphore.acquire();
        order.push(1);

        // This acquire should block
        const blocked = semaphore.acquire().then(() => order.push(3));

        order.push(2);

        // Release to unblock
        semaphore.release();
        await blocked;

        expect(order).toEqual([1, 2, 3]);
    });

    test("should process queued acquires in FIFO order", async () => {
        const semaphore = new Semaphore(1);
        const order: string[] = [];

        await semaphore.acquire();

        const p1 = semaphore.acquire().then(() => order.push("first"));
        const p2 = semaphore.acquire().then(() => order.push("second"));

        semaphore.release(); // unblocks first
        await p1;

        semaphore.release(); // unblocks second
        await p2;

        expect(order).toEqual(["first", "second"]);
    });

    test("should throw when releasing with zero count", () => {
        const semaphore = new Semaphore(1);

        expect(() => semaphore.release()).toThrow("Semaphore count cannot be negative");
    });

    test("should allow re-acquisition after release", async () => {
        const semaphore = new Semaphore(1);

        await semaphore.acquire();
        semaphore.release();

        // Should not block
        await semaphore.acquire();
        semaphore.release();

        expect(true).toBe(true);
    });

    test("should handle limit of 2 correctly", async () => {
        const semaphore = new Semaphore(2);
        const order: number[] = [];

        await semaphore.acquire();
        await semaphore.acquire();

        // Third should block
        const p = semaphore.acquire().then(() => order.push(2));
        order.push(1);

        semaphore.release();
        await p;

        expect(order).toEqual([1, 2]);
    });
});
