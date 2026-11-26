# Mätning: håller vi prestandakraven?

Övning 7, vecka 2. Kort svar: **ja, båda kraven håller med god marginal.**

## Datorn

| | |
|---|---|
| Processor | AMD Ryzen AI 7 350 |
| Minne | 31 GB |
| Operativsystem | Windows 11 |
| Programvara | .NET 10 (SDK 10.0.401), Node 24.21.0, BenchmarkDotNet 0.15.8, autocannon |
| Datum | 2026-11-26 |

API:t och lasttestet körde på samma dator, så siffrorna säger hur snabbt API:t är utan nätverk emellan. På en riktig
server, med nätverk och fler användare, blir svarstiderna längre.

## IFK-2: sortering av arbetslistan

**Kravet:** att sortera arbetslistan med 1 000 uppgifter tar under 1 ms.

**Hur vi mätte:** en benchmark med BenchmarkDotNet (`backend/Starter.Benchmarks/WorkListBenchmarks.cs`) som mäter
`TodoWorkList.Order` med 100, 1 000 och 10 000 uppgifter. Datan slumpas med en fast seed (42), så att varje körning
sorterar exakt samma lista: ungefär var fjärde uppgift är klar och var femte saknar förfallodatum. Körd i Release:

```powershell
cd backend
dotnet run -c Release --project Starter.Benchmarks -- --filter "*" --job short
```

| Antal uppgifter | Medeltid | Minne per sortering |
|---|---|---|
| 100 | 3,6 µs (0,0036 ms) | 4,15 KB |
| 1 000 | **52,6 µs (0,053 ms)** | 34 KB |
| 10 000 | 1 470 µs (1,47 ms) | 333 KB |

**Håller kravet?** Ja. 1 000 uppgifter sorteras på 0,053 ms, ungefär 19 gånger snabbare än gränsen.

**Värt att notera:** tio gånger fler uppgifter (1 000 → 10 000) ger 28 gånger längre tid. Sortering gör ungefär
*n · log n* jämförelser, inte *n*, och vid 10 000 uppgifter börjar skräpsamlaren städa mer (Gen1). Om listan växer till
10 000 uppgifter håller sorteringen inte längre kravet på under 1 ms.

`--job short` gör bara ett fåtal mätningar, så felmarginalen (kolumnen *Error* i BenchmarkDotNet) är stor. Det räcker
för att svara på om kravet håller, men inte för att jämföra små skillnader.

## IFK-1: API:t under last

**Kravet:** `GET /api/todos` svarar på under 200 ms (p99) med 1 000 uppgifter och 20 samtidiga användare, utan fel.

**Hur vi mätte:** API:t startat i Release, fyllt med 1 000 uppgifter, och sedan 20 samtidiga anslutningar i 10
sekunder med autocannon:

```powershell
# Terminal 1, i backend/
dotnet run -c Release --project Starter.Api

# Terminal 2, i repots rotmapp
node perf/seed.mjs 1000
npx autocannon -c 20 -d 10 http://localhost:5069/api/todos
```

| Mått | Värde |
|---|---|
| Latens p50 | 7 ms |
| Latens p97,5 | 19 ms |
| **Latens p99** | **23 ms** |
| Max | 199 ms |
| Anrop per sekund (medel) | 2 287 |
| Totalt | 23 000 anrop på 10 sekunder |
| Fel | 0 |
| Data per svar | ungefär 115 KB |

**Håller kravet?** Ja. p99 är 23 ms, långt under 200 ms, och inget anrop misslyckades.

## Ett misstag som lärde något: Debug i stället för Release

Första lasttestet kördes av misstag med API:t startat i Visual Studio, i **Debug**, med debuggern påkopplad:

| Mått | Debug med debugger | Release |
|---|---|---|
| Latens p50 | 312 ms | 7 ms |
| Latens p99 | 874 ms | 23 ms |
| Anrop per sekund | 59 | 2 287 |

Med Debug-siffrorna hade kravet **inte** hållit, fast koden var exakt densamma. Debuggern stannade dessutom när
autocannon avslutade och avbröt pågående anrop (`TaskCanceledException`), ett undantag som ASP.NET Core annars hanterar
själv. Därför mäts prestanda alltid i Release.

## Vad som kan bli ett problem

Varje anrop till `GET /api/todos` skickar **hela listan**, ungefär 115 KB med 1 000 uppgifter. Med fler uppgifter växer
både svaret och sorteringstiden. Det naturliga nästa steget är paginering, alltså att skicka till exempel 50 uppgifter
åt gången. Det är en extra-övning och inte gjord här.
