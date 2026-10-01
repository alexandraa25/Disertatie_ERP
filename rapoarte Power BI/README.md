# Rapoarte Power BI

| Fișier | Domeniu |
| --- | --- |
| `educational.pbix` | Activități educaționale |
| `financial_analysis.pbix` | Analiză financiară |
| `hr.pbix` | Resurse umane |

## Utilizare

1. Deschide fișierul dorit în Power BI Desktop.
2. Verifică sursele de date și adaptează serverul, baza și acreditările la mediul propriu.
3. Actualizează datele și verifică vizualizările și erorile de interogare.
4. Pentru afișarea în aplicație, configurează publicarea și accesul în Power BI și actualizează URL-ul din componenta Angular corespunzătoare.

Pornirea aplicației ERP nu publică automat fișierele `.pbix`.

## Integrare Angular

Linkurile `https://app.powerbi.com/view?...` sunt definite în componentele `education-dashboard`, `financial-dashboard` și `hr-dashboard`, din `ERPSystem_Frontend/Client/src/app/pages/dashboard-analysis/`. Modalul comun este în `power-bi-modal/`.

Nu a fost identificat un serviciu backend de generare a tokenurilor Power BI Embed pentru această integrare. Verifică accesul înainte de publicarea datelor financiare, HR sau ale cursanților: linkurile publice nu oferă controlul de acces al aplicației ERP.

## Limitele verificării

Au fost verificate existența fișierelor și referințele Angular. Rapoartele nu au fost deschise în Power BI Desktop; modelul de date, măsurile DAX, acreditările și actualizarea nu au fost validate.

[Prezentarea proiectului](../README.md)
