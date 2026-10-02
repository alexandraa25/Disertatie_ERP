# Frontend — EduManager

Interfață Angular 18.2.14 pentru administrarea activităților educaționale, financiare și HR. Utilizează Angular Material/CDK, Chart.js, Quill, SweetAlert2 și Font Awesome.

## Instalare și pornire

Din rădăcina repository-ului, în PowerShell:

```powershell
Set-Location .\ERPSystem_Frontend\Client
npm ci
npm start
```

Aplicația este disponibilă la `http://localhost:4200`. `package-lock.json` fixează dependențele pentru instalarea cu `npm ci`.

Ai nevoie de Node.js și npm. Pachetul Angular CLI instalat declară intervalul Node `^18.19.1 || ^20.11.1 || >=22.0.0`; repository-ul nu fixează separat versiunea Node.

Pornește [backendul](../../ERPSystem/README.md) cu profilul `https` și configurează baza de date și SendGrid înainte de verificarea autentificării complete.

## Conexiuni și configurare

Adresa API este centralizată în `src/environments/environment.ts`, în `apiBaseUrl` (implicit `https://localhost:7195`). Modifică această valoare înainte de build pentru alt mediu și configurează originile permise prin `Cors:AllowedOrigins` în backend. Folosește adresa fără `/` final.

Metodele de profil din `auth.service.ts` și `user-profile.service.ts` folosesc endpointurile `GET/PUT /me/profile`. Nu este necesar un serviciu pe portul 3000.

Linkurile Power BI se află în componentele `education-dashboard`, `financial-dashboard` și `hr-dashboard`, în `src/app/pages/dashboard-analysis/`.

## Organizare

| Cale | Conținut |
| --- | --- |
| `src/app/app.routes.ts` | Rute, componente și roluri permise |
| `src/app/pages/account/` | Autentificare, confirmări, profil și parole |
| `src/app/pages/academics/` | Cursanți, cursuri și înscrieri |
| `src/app/pages/financiar/` | Contracte, acte adiționale și plăți |
| `src/app/pages/hr/` | Angajați și concedii |
| `src/app/pages/marketing/` | Campanii, newslettere și e-mailuri |
| `src/app/pages/feedback/` | Feedback și analize |
| `src/app/pages/dashboard-analysis/` | Dashboarduri și Power BI |
| `src/app/pages/services/`, `src/app/pages/models/` | Apeluri API și modele TypeScript |
| `src/app/components/` | Componente comune și guards |

Tokenul de acces este păstrat în `localStorage`. Interceptorul îl trimite doar către API-ul configurat și nu deconectează utilizatorul pentru erori 401 de la servicii externe. Guards controlează navigarea, iar API-ul aplică autentificarea și politicile de rol.

## Comenzi

Rulează din acest director:

```powershell
npm start
npm run build
npm run build -- --configuration development
npm run watch
npm test
npm run test:ci
```

- `start`: server local cu reîncărcare la modificări.
- `build`: build de producție, în `dist/plynk-front-master/`, conform `angular.json`.
- `build -- --configuration development`: compilare în configurația development.
- `watch`: compilare continuă pentru dezvoltare.
- `test`: Jasmine/Karma; necesită un browser compatibil cu lansatorul Chrome configurat.


## Depanare și verificare

| Simptom | Ce verifici |
| --- | --- |
| API inaccesibil | Profilul backend `https`, portul 7195 și certificatul local |
| Eroare CORS | Originea `http://localhost:4200` permisă de API |
| Nu sosește codul de autentificare | SendGrid, expeditorul și șablonul activ din baza de date |
| Analiza feedbackului eșuează | Serviciul NLP la portul 8000 |
| Buildul depășește bugetele | Mesajele Angular și limitele `budgets` din `angular.json` |
| Raport Power BI indisponibil | Linkul publicat și sursele de date |


[Prezentarea proiectului](../../README.md)
