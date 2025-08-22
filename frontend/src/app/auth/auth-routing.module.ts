import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { WarmUpComponent } from '../components/warm-up/warm-up.component';
import { DebugComponent } from '../debug/debug.component';
// import { RegisterComponent } from './register/register.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'warmup', component: WarmUpComponent },
  { path: '/debug', component: DebugComponent }
  // { path: 'register', component: RegisterComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuthRoutingModule { }
