import { Injectable } from '@angular/core';
import { ActivatedRoute, ActivatedRouteSnapshot, ResolveEnd, Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { Subscription } from 'rxjs';
import { environment } from '../environments/environment';
import { CookieService } from './cookie.service';

@Injectable({
  providedIn: 'root',
})
export class ApplicationinsightsService {
  private config: Microsoft.ApplicationInsights.IConfig = {
    instrumentationKey: environment.appInsights.instrumentationKey,
    isCookieUseDisabled: true,
    disableCorrelationHeaders: false,
    overridePageViewDuration: true
  };

  private routerSubscription: Subscription;

  constructor(private router: Router, private activatedRoute: ActivatedRoute, private cookie : CookieService) {
    if (!AppInsights.config) {
      AppInsights.downloadAndSetup(this.config);
    }

    this.routerSubscription = this.router.events
      .subscribe((event) => {
        if (event instanceof ResolveEnd) {
          const activatedComponent = this.getActivatedComponent(event.state.root);
          if (activatedComponent) {
            this.logPageView(`${activatedComponent.name} ${this.getRouteTemplate(event.state.root)}`, event.urlAfterRedirects);
          }
        }
      });

    AppInsights.queue.push(function () {
      AppInsights.context.addTelemetryInitializer(function (envelope) {
        envelope.tags['ai.session.id'] = cookie.get('WB-Session-Id');
      });
    });
  }

  public logPageView(name: string, url?: string, properties?: { [key: string]: string }, measurements?: { [key: string]: number }, duration?: number) {
    AppInsights.trackPageView(name, url, this.addGlobalProperties(properties), measurements, duration);
  }

  public logEvent(name: string, properties?: { [key: string]: string }, measurements?: { [key: string]: number }) {
    AppInsights.trackEvent(name, this.addGlobalProperties(properties), measurements);
  }

  public logError(error: Error, properties?: { [key: string]: string }, measurements?: { [key: string]: number }) {
    AppInsights.trackException(error, null, this.addGlobalProperties(properties), measurements);
  }

  public setAuthenticatedUserId(userId: string, accountId: string): void {
    AppInsights.setAuthenticatedUserContext(userId, accountId, false);
  }

  public clearAuthenticatedUserId(): void {
    AppInsights.clearAuthenticatedUserContext();
  }
  
  private addGlobalProperties(properties?: { [key: string]: string }): { [key: string]: string } {
    if (!properties) {
      properties = {};
    }
    
    return properties;
  }

  private getActivatedComponent(snapshot: ActivatedRouteSnapshot): any {

    if (snapshot.firstChild) {
      return this.getActivatedComponent(snapshot.firstChild);
    }

    return snapshot.component;
  }

  private getRouteTemplate(snapshot: ActivatedRouteSnapshot): string {
    let path = '';
    if (snapshot.routeConfig) {
      path += snapshot.routeConfig.path;
    }

    if (snapshot.firstChild) {
      return path + this.getRouteTemplate(snapshot.firstChild);
    }

    return path;
  }
}
