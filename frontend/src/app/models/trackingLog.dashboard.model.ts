// Add an interface for type safety
interface TrackingLog {
    id: number;
    eventDate: Date;
    urgency: string;
    awokeFromSleep: boolean;
    painLevel: number;
    notes?: string;
}
