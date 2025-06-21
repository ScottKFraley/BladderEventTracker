// app/state/config/config.state.ts
export interface ConfigState {
    daysPrevious: number;
    loading: boolean;
    error: string | null;
}
