import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppToken, UserToken } from '../types/auth';
import { Observable, of } from 'rxjs';
import { publishReplay, map, tap, refCount } from 'rxjs/operators';
import { ApplicationinsightsService } from './applicationinsights.service';

@Injectable({
  providedIn: 'root'
})

export class AuthService {

  private _appToken: Observable<string>;

  constructor(private http: HttpClient, private ai: ApplicationinsightsService) { }

  public get appToken(): Observable<string> {
    if (!this._appToken) {
      this._appToken = this.http.get<AppToken>('/api/appauth').pipe(
        map(t => t.accessToken),
        publishReplay(1),
        refCount()
      );
    }

    return this._appToken;
  }
}
