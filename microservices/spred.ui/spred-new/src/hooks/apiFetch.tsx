import Cookies from "js-cookie";
import FingerprintJS from '@fingerprintjs/fingerprintjs';

export async function apiFetch(input: RequestInfo, init: RequestInit = {}) {
    const accessToken = Cookies.get(import.meta.env.VITE_COOKIE_NAME);

    const defaultHeader: HeadersInit = { 'Authorization': `Bearer ${accessToken}`,
        'X-Client-DeviceId' : await getDeviceId()}

    const config: RequestInit = {
        ...init,
        headers: {
            ...Object.fromEntries(new Headers(init.headers as HeadersInit)),
            ...defaultHeader
        },
        credentials: init.credentials,
    };

    return fetch(input, config);
}

export async function getDeviceId(): Promise<string> {
    const fp = await FingerprintJS.load();
    const result = await fp.get();
    return result.visitorId;
}