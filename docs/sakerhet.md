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

## Hål 2: fält som formuläret aldrig visar (MB-3)

Formuläret skickar bara titel, förfallodatum och prioritet. Men API:t tar emot fler fält än så, och sparar dem.
JSON ligger i en fil, eftersom Windows PowerShell tar bort citattecknen om JSON skrivs direkt på kommandoraden.

**Bevis 1 (MB-3.1):** sälj skapar en uppgift och skickar med `teamId`.

```powershell
'{"title":"Smugglad av salj","priority":"Normal","teamId":2}' | Out-File -Encoding ascii body.json
curl.exe -i -H "X-Team: salj" -H "Content-Type: application/json" --data-binary "@body.json" http://localhost:5069/api/todos
curl.exe -H "X-Team: ekonomi" http://localhost:5069/api/todos
```

Svar före lagning: `201 Created` (id 403), och *"Smugglad av salj"* ligger i **ekonomis** lista. Team 2 är ekonomi.
Sälj kan alltså plantera uppgifter hos ekonomi som ser ut att komma från ekonomis eget team.

**Bevis 2 (MB-3.2):** sälj flyttar sin egen uppgift 101 till ekonomi.

```powershell
'{"title":"Ring tillbaka till Volvo om offerten","priority":"High","teamId":2}' | Out-File -Encoding ascii body.json
curl.exe -i -X PUT -H "X-Team: salj" -H "Content-Type: application/json" --data-binary "@body.json" http://localhost:5069/api/todos/101
curl.exe -H "X-Team: ekonomi" http://localhost:5069/api/todos
```

Svar före lagning: `200 OK`, och Volvo-uppgiften ligger nu i ekonomis lista. Den har försvunnit från säljs lista.

**Bevis 3 (MB-3.3):** sälj döljer sin uppgift 102 utan att ta bort den, med flaggan `isDeleted`.

```powershell
'{"title":"Uppdatera pipeline i CRM","priority":"Normal","isDeleted":true}' | Out-File -Encoding ascii body.json
curl.exe -i -X PUT -H "X-Team: salj" -H "Content-Type: application/json" --data-binary "@body.json" http://localhost:5069/api/todos/102
curl.exe -H "X-Team: salj" http://localhost:5069/api/todos
curl.exe -i -H "X-Team: salj" http://localhost:5069/api/todos/102
```

Svar före lagning: `200 OK`. Uppgift 102 är borta ur säljs lista, fast ingen har gjort `DELETE`. Den syns inte
heller i sökningen eller statistiken. Men `GET /api/todos/102` ger fortfarande `200` med uppgiften: appen behandlar
"borttagen" olika på olika ställen.

| Anrop som sälj | Svar före lagning | Svar efter lagning |
|---|---|---|
| `POST` med `teamId: 2` | 201, uppgiften hamnar hos ekonomi | |
| `PUT /101` med `teamId: 2` | 200, uppgiften flyttas till ekonomi | |
| `PUT /102` med `isDeleted: true` | 200, uppgiften försvinner ur listan | |

**Test och lagning:** *(fylls i)*

## Hål 3: SQL-injektion i sökningen (MB-4)

**Bevis 1 (MB-4.1):** sälj söker med SQL i sökfältet. `--data-urlencode` kodar texten så att den kan stå i adressen.

```powershell
curl.exe -G -H "X-Team: salj" "http://localhost:5069/api/todos/search" --data-urlencode "q=' OR 1=1 --"
```

Svar före lagning: alla tio uppgifter från alla fyra team, bland annat ekonomis *"Bonusunderlag ledningsgrupp
(konfidentiellt)"* och *"Byt lösenord till bankportalen"*. Ingen av dem innehåller söktexten.

Söktexten klistrades in direkt i SQL-satsen, så frågan blev:

```sql
SELECT * FROM Todos WHERE TeamId = 1 AND IsDeleted = 0 AND Title LIKE '%' OR 1=1 --%'
```

`'` avslutade texten, `OR 1=1` gjorde villkoret sant för alla rader, och `--` kommenterade bort resten, inklusive
team-filtret.

**Bevis 2 (MB-4.2):** ett ensamt citattecken.

```powershell
curl.exe -i -G -H "X-Team: salj" "http://localhost:5069/api/todos/search" --data-urlencode "q='"
```

Svar före lagning: `500 Internal Server Error` med `SqliteException: unrecognized token`.

| Anrop | Svar före lagning | Svar efter lagning |
|---|---|---|
| Sök på `' OR 1=1 --` | 200 med alla teams uppgifter | 200 och `[]` |
| Sök på `'` | 500 med databasfel | 200 och `[]` |
| Sök på `Offert` (vanlig sökning) | Säljs Volvo-uppgift | Samma, oförändrat |

**Test:** `backend/Starter.Tests/Features/Todos/SearchInjectionTests.cs`, båda committade röda innan lagningen.

| Kriterium | Test |
|---|---|
| MB-4.1 | `Search_with_sql_returns_no_todos_from_other_teams` |
| MB-4.2 | `Search_with_a_single_quote_returns_200_and_no_database_error` |

**Lagning:** `TodoRepository.SearchAsync` använde `FromSqlRaw` med användarens text inklistrad. Den använder nu LINQ,
så att söktexten skickas som en parameter och aldrig blir en del av SQL-satsen:

```csharp
.Where(t => t.TeamId == TeamId && !t.IsDeleted && EF.Functions.Like(t.Title, $"%{query}%"))
```

`EF.Functions.Like` i stället för `Title.Contains`, så att sökningen fortfarande inte skiljer på stora och små
bokstäver. Lagningen tog också bort kodanalysatorns varning EF1002, som hade gjort CI rött.

## Hål 4: felmeddelanden som avslöjar hur appen är byggd

**Bevis (upptäckt under hål 3):** samma anrop med ett ensamt `'` gav, före lagningen av hål 3, ett svar med:

- typen av databas: `Microsoft.Data.Sqlite.SqliteException`
- hela stacktracen, från controllern ner till databasen
- filsökvägar på utvecklarens dator, med radnummer: `...\Features\Todos\TodoRepository.cs:line 30`
- alla headers i anropet, och vilken endpoint och controller som kördes

En angripare lär sig alltså vilken databas appen använder, var den sårbara frågan finns, och att den kraschar på
citattecken, vilket bekräftar att SQL-injektion fungerar. Svaret kom eftersom API:t kördes i `Development`.

**Test och lagning:** *(fylls i)*
