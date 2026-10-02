# EduManager — backend

API ASP.NET Core Minimal APIs, cu proiectul executabil în `ERPSystem/ERPSystem.csproj` și soluția `ERPSystem.slnx`.

## Tehnologii și structură

.NET 10, Entity Framework Core 10.0.7, SQL Server, Identity/JWT, Swagger/OpenAPI, Serilog, SendGrid, QuestPDF și ClosedXML.

| Director în proiect | Conținut |
| --- | --- |
| `Modules/` | Endpointuri, servicii și DTO-uri pe module |
| `Data/` | Entități, context EF și audit |
| `Migrations/` | Migrarea inițială și modelul EF |
| `Configuration/` | Servicii, configurare CORS și maparea API |
| `Shared/` | E-mail, PDF, Excel, notificări și jurnal |
| `Utils/` | Setări, constante și modele de răspuns |

## Configurare

Comenzile de mai jos pornesc din rădăcina repository-ului. Variabilele de mediu suprascriu `ERPSystem/ERPSystem/appsettings.json`. Definește-le în terminalul folosit pentru migrare și pornirea API-ului:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=ERP_EduManager;Trusted_Connection=True;TrustServerCertificate=True'
$env:JwtSettings__SecretKey = '<cheie-aleatoare-lunga-generata-local>'
$env:JwtSettings__Issuer = 'EduManager'
$env:JwtSettings__Audience = 'EduManagerClient'
$env:ERPSystemSettings__BaseUrl = 'http://localhost:4200'
$env:EmailConnectionSettings__Token = '<cheie-api-sendgrid>'
$env:EmailConnectionSettings__DefaultFromEmail = '<expeditor-verificat>'
$env:Nlp__BaseUrl = 'http://127.0.0.1:8000'
$env:Cors__AllowedOrigins__0 = 'http://localhost:4200'
```

Înlocuiește valorile dintre `<...>` și adaptează instanța SQL. `TrustServerCertificate=True` este folosit în exemplul de dezvoltare locală.

`BaseUrl` construiește linkurile către interfață. Trimiterea e-mailurilor folosește `SendGridClient` și șabloane active din `EmailTemplates`. Clasa de setări declară și `SendEmailUrl`, însă trimiterea se bazează pe clientul SendGrid și token.

Adresa NLP se configurează prin `Nlp:BaseUrl`, cu valoarea implicită `http://127.0.0.1:8000`. CORS folosește lista `Cors:AllowedOrigins`, implicit `http://localhost:4200`; pentru origini suplimentare setează `Cors__AllowedOrigins__1` etc. `ERPSystemSettings:BaseUrl` este folosit și pentru linkurile de semnare și feedback.

## Inițializarea bazei de date

Alege una dintre variante pentru o bază nouă; nu importa exportul peste schema deja creată prin migrare.

### Bază nouă cu datele necesare aplicației

```powershell
dotnet restore .\ERPSystem\ERPSystem\ERPSystem.csproj
$env:BootstrapAdmin__Email = '<adresa-administratorului>'
$env:BootstrapAdmin__Password = '<parola-puternica-unica>'
dotnet run --project .\ERPSystem\ERPSystem\ERPSystem.csproj --no-launch-profile -- --initialize-database
Remove-Item Env:BootstrapAdmin__Password
```

Înlocuiește valorile dintre `<...>` înainte de execuție. Comanda separată aplică migrarea `20260510145227_InitialCreate`, adaugă cele șapte roluri (inclusiv `Marketing`), opt șabloane de e-mail și două șabloane de documente din resursa `Data/Seed/templates.json`, apoi creează administratorul dacă lipsește. Nu importă utilizatori sau date personale din exportul SQL și nu pornește serverul ori serviciul de expirare a contractelor.

Inițializarea păstrează parolele și șabloanele existente și refuză promovarea unui cont existent fără rol de administrator. Inserările sunt într-o tranzacție, după aplicarea migrărilor. Administratorul nou are e-mailul confirmat și trebuie să schimbe parola; autentificarea păstrează pasul de confirmare prin cod e-mail, deci SendGrid trebuie configurat.

După autentificare, completează datele reale în pagina Companie înainte de a crea contracte. Inițializatorul nu inventează date fiscale sau bancare. Comanda poate fi reluată pentru completarea elementelor lipsă; pornirea obișnuită a aplicației nu execută această inițializare.

### Importul exportului SQL

Deschide `script.sql` din rădăcină în SQL Server Management Studio și inspectează-l. Scriptul creează `ERP_EduManager`, include date și istoricul migrării și setează compatibilitatea `160`. Folosește o instanță compatibilă, de exemplu SQL Server 2022.

`CREATE DATABASE` folosește directoarele implicite de date și log ale instanței SQL. Nu executa exportul peste o bază de lucru existentă. Datele importate nu trebuie considerate automat date demo anonimizate.

Exportul include roluri și șabloane istorice. După import, poți rula comanda de inițializare pentru completarea rolului `Marketing` și a altor elemente lipsă, folosind adresa unui administrator existent sau o adresă nouă. Nu presupune că parolele conturilor importate sunt cunoscute.

## Compilare și pornire

```powershell
dotnet dev-certs https --trust
dotnet build .\ERPSystem\ERPSystem\ERPSystem.csproj
dotnet run --project .\ERPSystem\ERPSystem\ERPSystem.csproj --launch-profile https
```

Swagger: `https://localhost:7195/swagger`. Profilul `https` expune și `http://localhost:5116`; Angular folosește portul HTTPS 7195.

La pornire este înregistrat și `ContractExpirationService`, un serviciu de fundal; folosește o bază de dezvoltare pentru verificări.

## API și integrare

Principalele prefixe: `/auth`, `/me`, `/admin`, `/employee`, `/leaves`, `/students`, `/courses`, `/contracts`, `/additional-act`, `/dashboard`, `/company`, `/mk-campaign`, `/feedback`, `/notifications`. Consultă Swagger pentru rutele și modelele exacte.

`NlpAnalysisService` trimite `text` și `reviewType` către `POST /analyze-review`. Nu transmite parametrul opțional `rating` acceptat de Python.

## Autorizare și verificare

Grupurile API și politica implicită cer autentificare. Excepțiile publice sunt declarate pe endpoint: login, confirmări, recuperarea parolei, logout și operațiile de semnare/feedback care își verifică tokenurile. Înregistrarea unui utilizator și declanșarea manuală a expirării contractelor cer rolul `Admin`. Restricțiile de rol existente sunt păstrate.

```powershell
dotnet run --project .\ERPSystem\ERPSystem.SecurityChecks\ERPSystem.SecurityChecks.csproj
```

Proiectul de verificare pornește un server local pe un port liber, cu JWT-uri de test, și oprește cererile după autorizare, înaintea serviciilor de business. Verifică toate rutele pentru acces anonim, utilizator fără rol și administrator, plus tokenuri expirate/invalide, roluri nepotrivite, politica implicită, CORS și resursele de inițializare.


[Prezentarea proiectului](../README.md)
