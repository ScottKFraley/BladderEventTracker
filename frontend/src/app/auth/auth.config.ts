// src/app/auth/auth.config.ts
import { InjectionToken } from '@angular/core';

export const TOKEN_REFRESH_THRESHOLD = new InjectionToken<number>('TOKEN_REFRESH_THRESHOLD');
