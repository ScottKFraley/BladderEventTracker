
export interface TrackingLogModel {
    id: string;
    eventDate: string;
    accident: boolean;
    changePadOrUnderware: boolean;
    leakAmount: number;
    urgency: number;
    awokeFromSleep: boolean;
    painLevel: number;
    notes?: string;
}
