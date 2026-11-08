import {ApiException} from "./api";

/**
 * The text to show the user for a failure: the backend's reason for a failed request that has one, else `fallback`.
 *
 * Reads errorResponse rather than message, because message falls back to NSwag's generic text when the backend
 * gives no reason.
 */
export function errorMessageOf(err: unknown, fallback: string): string {
    return (err instanceof ApiException && err.errorResponse?.message) || fallback;
}
