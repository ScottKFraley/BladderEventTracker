import { NgModule } from '@angular/core';
import { TOKEN_REFRESH_THRESHOLD } from './auth.config';
import { CommonModule } from '@angular/common';
import { AuthRoutingModule } from './auth-routing.module';


@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    AuthRoutingModule
  ],
  providers: [
    { provide: TOKEN_REFRESH_THRESHOLD, useValue: 300000 } // 5 minutes for production
  ]
})
export class AuthModule { }
