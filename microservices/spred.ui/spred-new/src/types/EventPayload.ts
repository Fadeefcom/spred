export interface EventPayload {
    eventType: string;
    location: string;
    tags: Record<string, string>;
    timestamp: string;
}