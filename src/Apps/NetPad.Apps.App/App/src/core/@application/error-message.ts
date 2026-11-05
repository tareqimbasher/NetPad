import {ApiException} from "./api";

/**
 * The text to show the user for a failure: the backend's message for a failed request, else the error's own
 * message, else `fallback`.
 */
export function errorMessageOf(err: unknown, fallback: string): string {
    if (err instanceof ApiException) {
        return err.errorResponse?.message || fallback;
    }

    return (err instanceof Error && err.message) || fallback;
}
