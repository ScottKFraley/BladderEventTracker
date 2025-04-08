// app/state/config/config.effects.ts
import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, mergeMap, catchError } from 'rxjs/operators';
import { ConfigService } from '../../services/config.service';
import * as ConfigActions from './config.actions';

@Injectable()
export class ConfigEffects {
    loadConfig$ = createEffect(() => this.actions$.pipe(
        ofType(ConfigActions.loadConfig),
        mergeMap(() => this.configService.getDaysPrevious()
            .pipe(
                map(days => ConfigActions.loadConfigSuccess({ daysPrevious: days })),
                catchError(error => of(ConfigActions.loadConfigFailure({ error: error.message })))
            ))
    )
    );

    updateDaysPrevious$ = createEffect(() => this.actions$.pipe(
        ofType(ConfigActions.updateDaysPrevious),
        mergeMap(action => {
            this.configService.setDaysPrevious(action.days);
            return of(ConfigActions.updateDaysPreviousSuccess({ days: action.days }));
        })
    )
    );
    constructor(
        private actions$: Actions,
        private configService: ConfigService) { }
}
