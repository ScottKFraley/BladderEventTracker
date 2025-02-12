import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    NgbModule,
    RouterOutlet],
  template: `
    <h1>Welcome to {{title}}!</h1>

    <p>
      app.component.ts file. NEED TO FIX obviously!
    </p>

    <router-outlet />
  `,
  styles: [],
})
export class AppComponent {
  title = 'Bladder Event Tracker';
}
