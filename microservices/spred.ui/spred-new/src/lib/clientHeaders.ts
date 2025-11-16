export function getClientHeaders() {
    return {
        'X-Client-Timezone': Intl.DateTimeFormat().resolvedOptions().timeZone,
        'X-Client-Language': navigator.language || '',
        'X-Client-Resolution': `${window.screen.width}x${window.screen.height}`,
        'X-Client-ColorDepth': window.screen.colorDepth.toString()
    };
}