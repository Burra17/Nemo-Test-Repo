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
| `GET /api/todos/202` | 200 med ekonomis uppgift | 404, ingen data |
| `PUT /api/todos/202` | 200, titeln ändrad och förfallodatumet borta | 404 (bevisat med testet för MB-2.2) |
| `PUT /api/todos/202/done` | 200, uppgiften markerad som klar (`isDone: true`, `completedAt` satt) | 404 |
| `DELETE /api/todos/202` | 200, uppgiften borttagen. Ekonomi får därefter 404 på sin egen uppgift. | 404, och ekonomi får fortfarande 200 med sin uppgift |

```powershell
curl.exe -i -X PUT -H "X-Team: salj" http://localhost:5069/api/todos/202/done
curl.exe -i -X DELETE -H "X-Team: salj" http://localhost:5069/api/todos/202
curl.exe -i -H "X-Team: ekonomi" http://localhost:5069/api/todos/202
```

**Sammanfattning:** sälj kan läsa, ändra, bocka av och ta bort ekonomis uppgifter, bara genom att byta id i
adressen. Alla fyra endpoints som tar ett id saknar kontroll av team.

**Test:** `backend/Starter.Tests/Features/Todos/TeamIsolationTests.cs`. Varje test skapar en uppgift som
ekonomi, försöker komma åt den som sälj och kontrollerar både svaret (404) och att ekonomis uppgift är oförändrad.
Alla fyra committades röda innan lagningen.

| Kriterium | Test |
|---|---|
| MB-1.1 | `Another_teams_todo_returns_404_and_no_data` |
| MB-2.1 | `Marking_another_teams_todo_done_returns_404` |
| MB-2.2 | `Updating_another_teams_todo_returns_404`, `Deleting_another_teams_todo_returns_404` |

```powershell
dotnet test --filter "Abuse~MB-"
```

**Lagning:** `TodoRepository.FindAsync` sökte bara på id. Den filtrerar nu också på anroparens team, på samma
sätt som listan:

```csharp
db.Todos.Where(t => t.TeamId == TeamId).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
```

Alla fyra endpoints hämtar uppgiften via `FindAsync`, så en ändring lagar alla fyra. En uppgift som tillhör ett annat
team ser nu ut precis som en uppgift som inte finns: 404, inte 403, så att angriparen inte får veta att den finns.
