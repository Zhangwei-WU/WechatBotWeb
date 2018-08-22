import { TestBed, inject } from '@angular/core/testing';

import { ApplicationinsightsService } from './applicationinsights.service';

describe('ApplicationinsightsService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApplicationinsightsService]
    });
  });

  it('should be created', inject([ApplicationinsightsService], (service: ApplicationinsightsService) => {
    expect(service).toBeTruthy();
  }));
});
