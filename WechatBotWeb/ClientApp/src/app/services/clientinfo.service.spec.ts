import { TestBed, inject } from '@angular/core/testing';

import { ClientinfoService } from './clientinfo.service';

describe('ClientinfoService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientinfoService]
    });
  });

  it('should be created', inject([ClientinfoService], (service: ClientinfoService) => {
    expect(service).toBeTruthy();
  }));
});
