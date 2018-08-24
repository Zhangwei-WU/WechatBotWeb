import { Component } from '@angular/core';
import { ApplicationinsightsService } from './services/applicationinsights.service';
import { ClientinfoService } from './services/clientinfo.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'app';

  constructor(private ai : ApplicationinsightsService, private client : ClientinfoService) {
  } 
}
