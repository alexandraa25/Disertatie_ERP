# Microserviciu NLP pentru feedback

Serviciu FastAPI în `main.py`, cu versiunea API `2.0.0`. Analizează recenzii despre cursuri, evaluări ale cursanților și recenzii externe. Numele directorului existent este `NPL-service`.

## Instalare și pornire

Ai nevoie de Python și pip, spațiu pentru PyTorch și modele și internet pentru prima descărcare. Proiectul nu fixează versiunea Python; verifică compatibilitatea interpreterului ales cu pachetele din `requirements.txt`.

Din rădăcina repository-ului, în PowerShell:

```powershell
Set-Location .\NPL-service
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m uvicorn main:app --host 127.0.0.1 --port 8000
```

Nu este necesară activarea mediului virtual. Directorul `.venv` este local și nu trebuie inclus în commituri. Modelele se încarcă înainte ca serverul să răspundă; prima pornire poate dura mai mult. `--reload` este opțional pentru dezvoltare și reîncarcă modelele la modificări.

## Modele și prelucrare

- Sentiment: `nlptown/bert-base-multilingual-uncased-sentiment`, prin Transformers.
- Cuvinte-cheie: KeyBERT cu `distiluse-base-multilingual-cased-v2`.
- Reguli locale: normalizarea diacriticelor, expresii în română, teme, emoții și scoruri.

Modelul de sentiment primește primele 512 **caractere**, nu un număr explicit de tokenuri. Regulile și extragerea cuvintelor-cheie folosesc textul complet. Rezumatul se construiește din șabloane.

## API

| Metodă | Cale | Rol |
| --- | --- | --- |
| GET | `/` | Stare și versiune |
| POST | `/analyze-review` | Analiza textului |
| GET | `/docs` | Documentație interactivă |

Exemplu PowerShell, după pornire:

```powershell
$body = @{
    text = 'Profesorul explica clar, iar materialele sunt utile.'
    reviewType = 'course_review'
    rating = 5
} | ConvertTo-Json
Invoke-RestMethod -Uri 'http://127.0.0.1:8000/analyze-review' -Method Post -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
```

`text` este obligatoriu, dar poate fi gol. `reviewType` acceptă `course_review` (implicit), `student_evaluation` și `external_review`. `rating` este opțional, între 1 și 5.

Răspunsul conține sentimentul (`pozitiv`, `negativ`, `neutru`), scorul, procente, emoție, cuvinte-cheie, teme și rezumat. În funcție de tip, conține scoruri pentru profesor, curs, comportament, progres, risc sau percepție publică. Câmpurile neaplicabile sunt `null`. Riscul este pe scala 0–100, iar scorurile de categorie pe scala 0–1.

## Integrare și limite

Backendul .NET apelează implicit `http://127.0.0.1:8000`; adresa se suprascrie prin setarea `Nlp:BaseUrl` sau variabila `Nlp__BaseUrl`. Transmite doar `text` și `reviewType`, fără `rating`.

Rezultatele combină un model multilingv și euristici; nu sunt probabilități calibrate. Nu există un raport de validare a preciziei pe texte românești în repository. Procentele temelor sunt rotunjite individual și pot să nu însumeze exact 100.

Serviciul nu configurează autentificare proprie; comanda de pornire îl limitează la interfața locală. Nu au fost identificate teste automate dedicate, iar modelele nu au fost descărcate sau executate în verificarea documentației.

[Prezentarea proiectului](../README.md)
