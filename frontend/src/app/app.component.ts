import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    NgbModule,
    RouterOutlet],
  templateUrl: 'app.component.html',
  styles: [],
})
export class AppComponent {
  title = 'Bladder Event Tracker';
}
