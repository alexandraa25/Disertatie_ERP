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


