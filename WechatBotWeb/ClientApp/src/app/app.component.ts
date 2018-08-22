import { Component } from '@angular/core';
import { ApplicationinsightsService } from './applicationinsights.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'app';

  constructor(private ai : ApplicationinsightsService) {
  } 
}
