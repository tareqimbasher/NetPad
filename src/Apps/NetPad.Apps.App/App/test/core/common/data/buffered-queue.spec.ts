import "@common/globals";
import {BufferedQueue} from "@common/data/buffered-queue";

describe("BufferedQueue", () => {
    beforeEach(() => {
        jest.useFakeTimers();
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    test("should throw if onFlush is not defined", () => {
        expect(() => new BufferedQueue({onFlush: undefined as never})).toThrow();
    });

    describe("flush on size", () => {
        test("should flush when item count exceeds flushOnSize", () => {
            const flushed: number[][] = [];
            const queue = new BufferedQueue<number>({
                flushOnSize: 2,
                onFlush: async (items) => { flushed.push([...items]); },
            });

            queue.add(1);
            queue.add(2);
            expect(flushed).toHaveLength(0); // size is 2, threshold is > 2

            queue.add(3); // now 3 items, exceeds threshold of 2
            expect(flushed).toHaveLength(1);
            expect(flushed[0]).toEqual([1, 2, 3]);
        });

        test("should not auto-flush when size is at or below threshold", () => {
            const flushed: number[][] = [];
            const queue = new BufferedQueue<number>({
                flushOnSize: 5,
                onFlush: async (items) => { flushed.push([...items]); },
            });

            queue.add(1);
            queue.add(2);
            queue.add(3);

            expect(flushed).toHaveLength(0);
        });
    });

    describe("flush on interval", () => {
        test("should flush on interval when items are present", async () => {
            const flushed: string[][] = [];
            const queue = new BufferedQueue<string>({
                flushOnInterval: 1000,
                onFlush: async (items) => { flushed.push([...items]); },
            });

            queue.add("a");
            queue.add("b");

            jest.advanceTimersByTime(1000);

            expect(flushed).toHaveLength(1);
            expect(flushed[0]).toEqual(["a", "b"]);
        });

        test("should not flush empty queue on interval", () => {
            const onFlush = jest.fn(async () => {});
            new BufferedQueue<string>({
                flushOnInterval: 1000,
                onFlush,
            });

            jest.advanceTimersByTime(1000);

            // onFlush is called but with empty array - the implementation
            // splices items and only calls onFlush if flushed.length > 0
            expect(onFlush).not.toHaveBeenCalled();
        });

        test("should restart timer after flush", async () => {
            const flushed: string[][] = [];
            const queue = new BufferedQueue<string>({
                flushOnInterval: 1000,
                onFlush: async (items) => { flushed.push([...items]); },
            });

            queue.add("a");
            jest.advanceTimersByTime(1000);
            expect(flushed).toHaveLength(1);

            // The timer restart happens in a .then() callback, so we need
            // to flush microtasks before the new setTimeout is registered
            await Promise.resolve();

            queue.add("b");
            jest.advanceTimersByTime(1000);
            expect(flushed).toHaveLength(2);
            expect(flushed[1]).toEqual(["b"]);
        });
    });

    describe("manual flush", () => {
        test("should flush all items on manual flush", () => {
            const flushed: number[][] = [];
            const queue = new BufferedQueue<number>({
                onFlush: async (items) => { flushed.push([...items]); },
            });

            queue.add(1);
            queue.add(2);
            queue.flush();

            expect(flushed).toHaveLength(1);
            expect(flushed[0]).toEqual([1, 2]);
        });

        test("manual flush with empty queue should not call onFlush", () => {
            const onFlush = jest.fn(async () => {});
            const queue = new BufferedQueue<number>({onFlush});

            queue.flush();

            expect(onFlush).not.toHaveBeenCalled();
        });

        test("manual flush should reset interval timer", async () => {
            const flushed: string[][] = [];
            const queue = new BufferedQueue<string>({
                flushOnInterval: 1000,
                onFlush: async (items) => { flushed.push([...items]); },
            });

            queue.add("a");

            // Advance partway, then manually flush
            jest.advanceTimersByTime(500);
            queue.flush();
            expect(flushed).toHaveLength(1);

            // The timer restart happens in a .then() callback
            await Promise.resolve();

            // Timer should have been reset. Add another item.
            queue.add("b");

            // Original timer would have fired at 500ms from now, but since
            // it was reset on flush it should fire 1000ms from the flush
            jest.advanceTimersByTime(500);
            expect(flushed).toHaveLength(1); // not yet

            jest.advanceTimersByTime(500);
            expect(flushed).toHaveLength(2);
            expect(flushed[1]).toEqual(["b"]);
        });
    });
});
