import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {

  appToken: string;

  constructor(private auth: AuthService) {
  }

  ngOnInit() {
    this.auth.appToken.subscribe(t => this.appToken = t);
  }

}
