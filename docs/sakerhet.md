# Säkerhet: hålen vi hittade, bevisade och lagade

> Alla anrop är gjorda mot vår egen app på `localhost`, med branchen `exercise/vecka-3-holes` från
> startprojektet. Inget annat system har testats.
>
> Kommandona är skrivna för Windows PowerShell, därför `curl.exe` i stället för `curl`.

## Hål 1: byta id och komma åt ett annat teams uppgift (MB-1, MB-2)

**Bevis:** sälj hämtar ekonomis uppgift 202.

```powershell
curl.exe -H "X-Team: salj" http://localhost:5069/api/todos/202
```

Svar före lagning:

```json
{"id":202,"title":"Bonusunderlag ledningsgrupp (konfidentiellt)","isDone":false,"createdAt":"2026-11-02T08:00:00+00:00","dueDate":"2026-12-15","priority":"High","isOverdue":false,"completedAt":null}
```

Uppgift 202 finns inte i säljs lista (`GET /api/todos` med `X-Team: salj`), bara i ekonomis. Listan filtrerar
alltså på team, men hämtningen av en enskild uppgift gör det inte.

**Bevis 2:** sälj ändrar ekonomis uppgift 202. JSON ligger i en fil, eftersom Windows PowerShell tar bort
citattecknen om JSON skrivs direkt på kommandoraden.

```powershell
'{"title":"Hackad av salj","priority":"Normal"}' | Out-File -Encoding ascii body.json
curl.exe -i -X PUT -H "X-Team: salj" -H "Content-Type: application/json" --data-binary "@body.json" http://localhost:5069/api/todos/202
curl.exe -H "X-Team: ekonomi" http://localhost:5069/api/todos/202
```

Svar före lagning: `HTTP/1.1 200 OK`, och när ekonomi hämtar uppgiften är titeln `"Hackad av salj"`. Förfallodatumet
(2026-12-15) har dessutom försvunnit, eftersom anropet inte skickade med något. Ekonomi skulle inte ens se att
uppgiften har en deadline längre.

**Fler endpoints som släpper igenom:**

| Anrop | Svar före lagning | Svar efter lagning |
|---|---|---|
| `GET /api/todos/202` | 200 med ekonomis uppgift | |
| `PUT /api/todos/202` | 200, titeln ändrad och förfallodatumet borta | |
| `PUT /api/todos/202/done` | 200, uppgiften markerad som klar (`isDone: true`, `completedAt` satt) | |
| `DELETE /api/todos/202` | 200, uppgiften borttagen. Ekonomi får därefter 404 på sin egen uppgift. | |

```powershell
curl.exe -i -X PUT -H "X-Team: salj" http://localhost:5069/api/todos/202/done
curl.exe -i -X DELETE -H "X-Team: salj" http://localhost:5069/api/todos/202
curl.exe -i -H "X-Team: ekonomi" http://localhost:5069/api/todos/202
```

**Sammanfattning:** sälj kan läsa, ändra, bocka av och ta bort ekonomis uppgifter, bara genom att byta id i
adressen. Alla fyra endpoints som tar ett id saknar kontroll av team.

**Test:** *(fylls i)*

**Lagning:** *(fylls i)*
