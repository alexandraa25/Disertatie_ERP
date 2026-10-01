import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('user');
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('renders the application shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('app-navbar')).not.toBeNull();
    expect(element.querySelector('router-outlet')).not.toBeNull();
    expect(element.querySelector('app-footer')).not.toBeNull();
    expect(element.querySelector('.scroll-to-top')).toBeNull();
  });

  it('shows the scroll control and returns to the top when clicked', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.componentInstance.showScrollTop = true;
    const scroll = spyOn(window, 'scrollTo');
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.scroll-to-top') as HTMLButtonElement).click();
    expect(scroll.calls.mostRecent().args as unknown[]).toEqual([{ top: 0, behavior: 'smooth' }]);
  });
});
