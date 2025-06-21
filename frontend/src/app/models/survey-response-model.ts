export interface SurveyResponse {
    EventDate: string;
    Accident: boolean;
    ChangePadOrUnderware: boolean;
    LeakAmount: number;
    Urgency: number;
    AwokeFromSleep: boolean;
    PainLevel: number;
    Notes?: string;
}
