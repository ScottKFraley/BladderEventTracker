import{ createAction, props } from'@ngrx/store';

export const loadConfig = createAction('[Config] Load');

export const loadConfigSuccess = createAction(
  '[Config] Load Success',
  props<{ daysPrevious: number}>()
);

export const loadConfigFailure = createAction(
  '[Config] Load Failure',
  props<{ error: string}>()
);

export const updateDaysPrevious = createAction(
  '[Config] Update Days Previous',
  props<{ days: number}>()
);

export const updateDaysPreviousSuccess = createAction(
  '[Config] Update Days Previous Success',
  props<{ days: number}>()
);

export const updateDaysPreviousFailure = createAction(
  '[Config] Update Days Previous Failure',
  props<{ error: string}>()
);
