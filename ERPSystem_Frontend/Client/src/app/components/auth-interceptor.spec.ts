import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { authInterceptor } from './auth-interceptor';

describe('API authentication interceptor', () => {
  let client: HttpClient;
  let requests: HttpTestingController;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(),
        { provide: Router, useValue: router }]
    });
    client = TestBed.inject(HttpClient);
    requests = TestBed.inject(HttpTestingController);
    localStorage.setItem('accessToken', 'test-token');
  });

  afterEach(() => {
    requests.verify();
    localStorage.removeItem('accessToken');
    localStorage.removeItem('user');
  });

  it('sends the token to the configured API', () => {
    const url = `${environment.apiBaseUrl}/me/profile`;
    client.get(url).subscribe();
    const request = requests.expectOne(url);
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({});
  });

  it('does not expose the token to an unrelated or similar URL', () => {
    for (const url of ['https://example.test/data', `${environment.apiBaseUrl}.example.test/data`]) {
      client.get(url).subscribe();
      const request = requests.expectOne(url);
      expect(request.request.headers.has('Authorization')).toBeFalse();
      request.flush({});
    }
  });

  it('clears the session when the API rejects the token', () => {
    const url = `${environment.apiBaseUrl}/me/profile`;
    localStorage.setItem('user', '{}');
    client.get(url).subscribe({ error: () => {} });
    requests.expectOne(url).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('keeps the session when an external request fails', () => {
    client.get('https://example.test/data').subscribe({ error: () => {} });
    requests.expectOne('https://example.test/data').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(localStorage.getItem('accessToken')).toBe('test-token');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not send placeholder tokens', () => {
    for (const token of ['null', 'undefined', '']) {
      localStorage.setItem('accessToken', token);
      const url = `${environment.apiBaseUrl}/auth/login`;
      client.post(url, {}).subscribe();
      const request = requests.expectOne(url);
      expect(request.request.headers.has('Authorization')).toBeFalse();
      request.flush({});
    }
  });
});
