import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppToken, UserToken } from './types/auth';
import { Observable, of } from 'rxjs';
import { ApplicationinsightsService } from './applicationinsights.service';

@Injectable({
  providedIn: 'root'
})

export class AuthService {
  
  constructor(private http: HttpClient, private ai: ApplicationinsightsService) { }

  public getAppToken(): Observable<AppToken> {
    return this.http.get<AppToken>('/api/appauth');
  }
}
