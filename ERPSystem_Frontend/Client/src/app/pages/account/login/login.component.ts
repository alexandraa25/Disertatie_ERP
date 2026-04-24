import { Component, OnInit, HostListener, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../services/auth.service';
import { SnackbarService } from '../../../components/snack-bar/snack-bar.service';

type Vec2 = { x: number; y: number };

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [CommonModule, ReactiveFormsModule, MatSnackBarModule]
})
export class LoginComponent implements OnInit, OnDestroy {

  loginForm!: FormGroup;
  showPassword = false;
  isLoading = false;

  popupTitle: string = '';
  popupMessage: string = '';
  popupIsError: boolean = false;
  popupVisible: boolean = false;



  creatureT: string[] = Array(4).fill('translate(0 0)');
  pupilT: string[] = Array(4).fill('translate(0 0)');
  blinkT: string[] = Array(4).fill('scale(1 0)');
  accT: string[] = Array(4).fill('translate(0 0)');

  emblemT = 'translate(0 0)';
  capT = 'translate(0 0)';

  mouth = { x1: 390, x2: 405, y: 165 };

  private raf = 0;
  private mouse: Vec2 = { x: 0, y: 0 };
  private center: Vec2 = { x: window.innerWidth / 2, y: window.innerHeight / 2 };

  private pupilMax = [3.5, 3.0, 2.2, 3.0];
  private eyeCenters: Vec2[] = [
    { x: 135, y: 185 }, // m1
    { x: 255, y: 105 }, // m2 (eyes moved down vs original)
    { x: 325, y: 145 }, // m3
    { x: 370, y: 160 }  // m4
  ];

  private blinkTimers: any[] = [];

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private snackbar: SnackbarService
  ) { }
  
  ngOnDestroy(): void {
  cancelAnimationFrame(this.raf);
  this.blinkTimers.forEach(t => clearTimeout(t));
}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });

    this.recalcCenter();
    this.startBlinking();
    this.loop();
  }


  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  goToForgot() {
    console.log('CLICK');
    this.router.navigate(['/forgot-password']);
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.snackbar.showError('Invalid form. Please check your fields.', 3500);
      return;
    }
    this.processLogin();
  }

  processLogin() {
    this.isLoading = true;

    this.auth.login(this.loginForm.value.email, this.loginForm.value.password)
      .subscribe({
        next: (res: any) => {

          this.isLoading = false;

          if (!res.isSuccess) {
            this.snackbar.showError(
              'Emailul sau parola sunt gresite.',
              3500
            );
            return;
          }

          if (res.value?.emailNotConfirmed) {

            this.router.navigate(['/confirm-email-registration'], {
              queryParams: {
                userId: res.value.userId
              }
            });

            return;
          }

          if (res.value?.requiresCode) {

            sessionStorage.setItem("tempToken", res.value.tempToken);

            this.router.navigate(['/confirm-login-code']);
            return;
          }

          this.router.navigate(['/profil-user']);
        },

        error: () => {
          this.isLoading = false;
          this.snackbar.showError(
            'Login failed.',
            2000
          );
        }
      });
  }

  @HostListener('window:resize')
  onResize() {
    this.recalcCenter();
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(e: MouseEvent) {
    this.mouse.x = e.clientX;
    this.mouse.y = e.clientY;
  }

  private recalcCenter() {
    this.center.x = window.innerWidth / 2;
    this.center.y = window.innerHeight / 2;
  }

  // ===== LOOP =====
  private loop = () => {
    const dx = (this.mouse.x - this.center.x) / Math.max(1, this.center.x);
    const dy = (this.mouse.y - this.center.y) / Math.max(1, this.center.y);

    // Creatures parallax
    for (let i = 0; i < 4; i++) {
      const factor = 12 + i * 7;
      const moveX = dx * factor;
      const moveY = dy * factor;
      const rot = dx * (2.2 + i * 0.7);
      this.creatureT[i] = `translate(${moveX} ${moveY}) rotate(${rot} 250 130)`;
    }

    // Emblem parallax (slower)
    this.emblemT = `translate(${dx * 6} ${dy * 6}) rotate(${dx * 0.8} 145 160)`;

    // Pupils follow (clamped)
    const v = this.limitVec({ x: dx, y: dy }, 1);
    for (let i = 0; i < 4; i++) {
      const px = v.x * this.pupilMax[i];
      const py = v.y * this.pupilMax[i];
      this.pupilT[i] = `translate(${px} ${py})`;
    }

    // Accessories pulse/bob (time-based)
    const t = performance.now() / 1000;

    // m1 green dot: pulse
    const p1 = 1 + Math.sin(t * 2.2) * 0.12;
    this.accT[0] = `translate(205 188) scale(${p1}) translate(-205 -188)`;

    // m3 chip bob
    this.accT[2] = `translate(${Math.sin(t * 2.0) * 1.2 + dx * 1.4} ${Math.cos(t * 2.4) * 1.2 + dy * 1.4})`;

    // m4 badge pulse + tiny drift
    const p4 = 1 + Math.sin(t * 2.6) * 0.10;
    this.accT[3] = `translate(404 208) scale(${p4}) translate(-404 -208) translate(${dx * 0.8} ${dy * 0.8})`;

    // cap string wiggle
    this.capT = `translate(${Math.sin(t * 3.0) * 0.8} ${Math.cos(t * 2.8) * 0.6})`;

    // Mouth reacts (m4)
    const mouthShift = dx * 2.5;
    const mouthOpen = Math.abs(dy) * 3;
    const baseLen = 15;
    const len = baseLen + mouthOpen * 2;

    this.mouth.y = 165 + dy * 2;
    this.mouth.x1 = 390 + mouthShift;
    this.mouth.x2 = this.mouth.x1 + len;

    this.raf = requestAnimationFrame(this.loop);
  };

  private limitVec(v: Vec2, maxLen: number): Vec2 {
    const len = Math.hypot(v.x, v.y);
    if (len <= maxLen || len === 0) return v;
    return { x: (v.x / len) * maxLen, y: (v.y / len) * maxLen };
  }

  private startBlinking() {
    for (let i = 0; i < 4; i++) {
      this.setBlink(i, false);
      this.scheduleBlink(i);
    }
  }

  private scheduleBlink(i: number) {
    const next = 1400 + Math.random() * 2600;
    const timer = setTimeout(() => {
      this.doBlink(i);
      this.scheduleBlink(i);
    }, next);
    this.blinkTimers[i] = timer;
  }

  private doBlink(i: number) {
    this.setBlink(i, true);
    setTimeout(() => this.setBlink(i, false), 120 + Math.random() * 80);
  }

  private setBlink(i: number, on: boolean) {
    const anchor = this.eyeCenters[i];
    if (!on) {
      this.blinkT[i] = `translate(${anchor.x} ${anchor.y}) scale(1 0) translate(${-anchor.x} ${-anchor.y})`;
      return;
    }
    this.blinkT[i] = `translate(${anchor.x} ${anchor.y}) scale(1 1) translate(${-anchor.x} ${-anchor.y})`;
  }

  

}
