import { Injectable } from '@angular/core';
import { ApplicationinsightsService } from './applicationinsights.service';
import * as ClientJS from 'clientjs';

@Injectable({
  providedIn: 'root'
})
export class ClientinfoService {

  private clientjs: any;

  get userAgent(): string {
    return this.clientjs.getUA();
  }

  constructor(private ai: ApplicationinsightsService) {
    this.clientjs = new ClientJS();
    console.log(this.userAgent);
    
  }
}
