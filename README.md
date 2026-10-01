# EduManager — ERP pentru activități educaționale

Proiect de disertație pentru gestionarea unui centru educațional: cursanți, cursuri, contracte, plăți, angajați, marketing și analiza feedbackului.

## Structură și documentație

| Componentă | Tehnologii | Ghid |
| --- | --- | --- |
| Backend | ASP.NET Core / .NET 10, EF Core, SQL Server | [ERPSystem](ERPSystem/README.md) |
| Interfață web | Angular 18.2.14, TypeScript, Angular Material | [Frontend](ERPSystem_Frontend/Client/README.md) |
| Analiza feedbackului | Python, FastAPI, Transformers, KeyBERT | [NLP](NPL-service/README.md) |
| Rapoarte | Power BI Desktop | [Power BI](rapoarte%20Power%20BI/README.md) |
| Bază de date | `script.sql`: schemă și date SQL Server | Detalii în ghidul backend |

Directorul `NPL-service` păstrează denumirea existentă în repository; implementează un serviciu NLP.

## Funcționalități

- Conturi, autentificare JWT, confirmare prin e-mail, recuperarea parolei și profil.
- Cursanți, cursuri, sesiuni și înscrieri.
- Contracte, acte adiționale, semnături, documente PDF, rate și plăți.
- Angajați, documente HR și cereri de concediu.
- Campanii de marketing, newslettere și istoricul e-mailurilor.
- Feedback pentru cursuri, evaluări ale cursanților și recenzii externe.
- Analiza sentimentului, cuvinte-cheie, teme și indicatori calculați din feedback.
- Dashboarduri, rapoarte Power BI, exporturi Excel, notificări și jurnal de activitate.

Interfața definește acces pentru roluri precum `Admin`, `Manager`, `Secretary`, `Teacher`, `HR`, `Accountant` și `Marketing`. Accesul la date trebuie protejat și în API.

## Arhitectură

```text
Angular :4200 → API ASP.NET Core :7195 → SQL Server
                                    → SendGrid
                                    → FastAPI NLP :8000 → modele NLP
Angular → rapoarte publicate în Power BI
```

## Pornire locală

Comenzile sunt pentru PowerShell. Rulează fiecare serviciu într-un terminal separat.

1. Pregătește SDK .NET 10, SQL Server, Node.js cu npm și Python. Versiunea Python nu este fixată în proiect.
2. Configurează SQL, JWT și SendGrid și inițializează baza conform [ghidului backend](ERPSystem/README.md). Comanda de inițializare aplică migrarea și adaugă rolurile, administratorul și șabloanele lipsă.
3. Pornește [serviciul NLP](NPL-service/README.md); prima pornire necesită descărcarea modelelor dacă nu sunt deja în cache.
4. Din rădăcina repository-ului, pornește backendul:

   ```powershell
   dotnet restore .\ERPSystem\ERPSystem\ERPSystem.csproj
   dotnet dev-certs https --trust
   dotnet run --project .\ERPSystem\ERPSystem\ERPSystem.csproj --launch-profile https
   ```

5. În alt terminal, pornind tot din rădăcina repository-ului:

   ```powershell
   Set-Location .\ERPSystem_Frontend\Client
   npm ci
   npm start
   ```

| Serviciu | Adresă |
| --- | --- |
| Interfață | http://localhost:4200 |
| API / Swagger | https://localhost:7195/swagger |
| API HTTP alternativ | http://localhost:5116 |
| Documentație NLP | http://127.0.0.1:8000/docs |

Autentificarea completă necesită un cont pregătit în baza de date și configurarea e-mailurilor. Nu există un cont demo documentat cu parolă publică.

## Verificarea proiectului

Verificare efectuată la 1 octombrie 2026, pe codul local:

- Backend: compilare reușită cu SDK 10.0.401. Persistă avertismente, în principal privind valorile nullable.
- Frontend: build de producție reușit cu Node.js 24.13.0; pachet inițial de aproximativ 705 kB după eliminarea scriptului Font Awesome redundant.
- Verificările de securitate folosesc rutele reale și middleware-ul JWT: acces anonim, roluri, tokenuri invalide/expirate și CORS. Sunt verificate și șabloanele incluse.
- Rezultat: 424 verificări HTTP trecute pe 138 de rute ale aplicației și o rută de test pentru politica implicită; 7 teste frontend trecute în ChromeHeadless.
- Nu au fost executate migrări, trimiteri de e-mail sau fluxuri complete cu SQL Server, NLP și Power BI.
- Frontendul include teste pentru structura aplicației și trimiterea tokenului doar către API-ul configurat. Nu există încă teste dedicate modelelor NLP.

### Corecții incluse

- API protejat implicit; acces anonim declarat explicit pentru autentificare/recuperare și fluxurile de semnare/feedback bazate pe token.
- API centralizat în `src/environments/environment.ts`; adresa NLP, CORS și linkurile din e-mail sunt configurabile.
- Comandă separată de inițializare cu șapte roluri, opt șabloane de e-mail și două șabloane de documente, fără suprascrierea datelor existente.
- Exportul SQL folosește directoarele implicite ale serverului. Conține în continuare date: inspectează-l înainte de import sau distribuire.
- Metodele de profil folosesc `/me/profile`. Scriptul SSR nefuncțional a fost eliminat; aplicația se construiește pentru browser.
- Buildul nu mai descarcă fonturi în timpul compilării; fonturile Google rămân resurse externe încărcate de browser.

### Comenzi de verificare

Din rădăcina repository-ului:

```powershell
dotnet run --project .\ERPSystem\ERPSystem.SecurityChecks\ERPSystem.SecurityChecks.csproj
Set-Location .\ERPSystem_Frontend\Client
npm run build
npm run test:ci
```

Testele de securitate verifică accesul până la intrarea în handler, fără execuția serviciilor de business. Pentru validarea funcțională completă sunt necesare servicii și date de test configurate separat.

Folosește variabile de mediu pentru secrete și nu copia chei sau date personale în documentație.
