import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { UserProfileService } from '../../services/user-profile.service';
import { UserProfileDto } from '../../models/user-profile.model';


@Component({
   selector: 'app-profil-user',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './profil-user.component.html',
  styleUrls: ['./profil-user.component.css'],
  
})
export class ProfilUserComponent implements OnInit {

  loading = true;
  saving = false;

  form!: FormGroup;   // ⚠️ DOAR declarare, fără this.fb aici

  constructor(
    private fb: FormBuilder,
    private userProfileService: UserProfileService
  ) {}

  ngOnInit(): void {

    // 🔹 aici inițializezi form-ul
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      phone: [''],
      jobTitle: [''],
      avatarUrl: [''],
      preferredLanguage: ['ro'],
      timeZone: ['Europe/Bucharest']
    });

    // 🔹 apoi încarci profilul
    this.userProfileService.getProfile().subscribe({
      next: (data) => {
        this.form.patchValue(data);
        this.loading = false;
      },
      error: () => {
        alert("Eroare la încărcarea profilului");
        this.loading = false;
      }
    });
  }

  save(): void {
    if (this.form.invalid) return;

    this.saving = true;

    const profile = this.form.value as UserProfileDto;

    this.userProfileService.updateProfile(profile).subscribe({
      next: () => {
        this.saving = false;
        alert("Profil salvat!");
      },
      error: () => {
        this.saving = false;
        alert("Eroare salvare profil");
      }
    });
  }
}
