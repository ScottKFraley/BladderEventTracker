// app/state/config/config.reducer.ts
import { createReducer, on } from '@ngrx/store';
import * as ConfigActions from './config.actions';
import { ConfigState } from './config.state';


export const initialState: ConfigState = {
    daysPrevious: 2,
    loading: false,
    error: null
};

export const configReducer = createReducer(
    initialState,
    on(ConfigActions.loadConfig, state => ({
        ...state,
        loading: true
    })),
    on(ConfigActions.loadConfigSuccess, (state, { daysPrevious }) => ({
        ...state,
        daysPrevious,
        loading: false
    })),
    on(ConfigActions.loadConfigFailure, (state, { error }) => ({
        ...state,
        error,
        loading: false
    })),
    on(ConfigActions.updateDaysPrevious, state => ({
        ...state,
        loading: true
    })),
    on(ConfigActions.updateDaysPreviousSuccess, (state, { days }) => ({
        ...state,
        daysPrevious: days,
        loading: false
    })),
    on(ConfigActions.updateDaysPreviousFailure, (state, { error }) => ({
        ...state,
        error,
        loading: false
    }))
);
