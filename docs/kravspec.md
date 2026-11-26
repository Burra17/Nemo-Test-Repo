# Kravspec: "Vi missar deadlines hela tiden"

> **Om källorna.** Intervjun gjordes med en AI som spelade Sara, eftersom jag inte var på plats.
> Det som kommer från kursmaterialet eller `docs/arendet.md` är fakta om Sara.
> Det som är märkt **⚠️ improviserat** hittade AI:n på under intervjun, och det ska kollas
> med läraren eller en klasskompis innan det räknas som ett riktigt krav.

## 1. Problemet, med kundens egna ord

> "Vi är ett team på åtta personer som använder er uppgiftsapp varje dag. Problemet är att vi missar
> deadlines hela tiden, och ingen märker det förrän kunden ringer och är arg."
> — Sara, teamledare

Buggrapport efter Kims demo:

> "Klockan nio på morgonen står det att 'Skicka offert' är en dag sen, fast den ska vara klar idag.
> Folk har slutat lita på listan."

**Sammanfattning på tre meningar:** Teamet missar deadlines för att ingen ser i tid vilka uppgifter
som håller på att bli sena, och de upptäcker det först när kunden ringer. Listan med försenade
uppgifter som finns idag visar fel, så teamet har slutat lita på den. Sara vill att teamet varje morgon
ska se vad som måste göras först, och att andelen uppgifter som blir klara i tid ska öka.

## 2. Mål, och hur vi vet att vi har nått dem

| | |
|---|---|
| **Mätvärde** | Andelen uppgifter med förfallodatum som blir klara i tid |
| **Nuläge** | Ungefär 60 % (Sara räknade för hand på förra månaden ⚠️ improviserat: hur hon räknade) |
| **Mål** | 85 % |
| **Tidsram** | 4 veckor efter driftsättning |
| **Hur vi mäter** | `GET /api/todos/stats` räknar ut andelen ur systemet, så att ingen behöver räkna för hand |

**Uppföljning:**

- **När:** ett möte fyra veckor efter driftsättning, bokat redan samma dag som funktionen driftsätts, så att det inte
  glöms bort. En kortare avstämning efter två veckor, för att se att siffran alls rör sig.
- **Med vem:** Sara, som äger målet, och gärna en eller två från teamet, som kan berätta varför en uppgift blev sen.
- **Hur:** vi hämtar `GET /api/todos/stats` tillsammans och jämför andelen klara i tid med nuläget (60 %) och målet
  (85 %). Siffran räknas bara på uppgifter som blivit klara *efter* driftsättningen, eftersom äldre uppgifter saknar
  tidpunkt för när de blev klara.
- **Vad vi frågar:** har teamet slutat missa deadlines, eller har de bara slutat sätta datum? Om antalet uppgifter
  med förfallodatum har sjunkit kraftigt kan andelen se bra ut utan att något har blivit bättre.
- **Om målet inte är nått:** vi tittar på de sena uppgifterna tillsammans och letar mönster, till exempel att vissa
  sorters uppgifter alltid blir sena, och bestämmer nästa steg med Sara.

## 3. Frågor vi ställde, och svaren

| Fråga | Svar (sammanfattat) |
|---|---|
| Vad händer idag när en deadline missas? | Kunden ringer eller mejlar. Någon får släppa allt och fixa det i sista minuten, och Sara får be om ursäkt. Uppgiften låg oftast i appen hela tiden. Förra veckan missades två saker ⚠️ improviserat. |
| Varför märker ingen att uppgiften närmar sig sitt datum? | Listan är lång och sorteras i den ordning uppgifterna skapades, så det som brådskar syns inte. Alla kollar mest sina egna saker. Ingen hinner gå igenom hela listan. ⚠️ improviserat: 100–150 öppna uppgifter, sortering på skapad-tid, uppgifter som ingen äger, en halvtimme att gå igenom listan. |
| När räknas något som försenat? | "Har jag sagt fredag så får det bli klart fredag kväll." Uppgiften är sen först dagen efter förfallodatumet. ⚠️ improviserat: klara uppgifter är aldrig försenade, och uppgifter utan datum kan aldrig vara försenade. |
| Vilken tidszon gäller om servern står i ett annat land? | Svensk tid. "Fredag kväll är fredag kväll i Stockholm." ⚠️ improviserat: alla sitter på samma kontor. |
| Hur många använder appen samtidigt? | Alla åtta, varje dag. Alla öppnar den ungefär samtidigt på morgonen. ⚠️ improviserat: mellan 08:00 och 08:30, före morgonmötet. |
| Om listan laddar på en kvarts sekund, känns det direkt? | Ja, även när alla öppnar den samtidigt. Listan växer, och kan ha runt tusen uppgifter om ett år ⚠️ improviserat. |
| Om en månad, vad har ändrats om vi har lyckats? | Färre missade deadlines, och folk litar på listan igen. Från 6 av 10 klara i tid till 8,5 av 10. Sara vill inte räkna för hand. |
| Vad räknas som klar i tid? | Klar senast på förfallodagen, svensk tid. Det räknas när uppgiften bockas av. ⚠️ improviserat: uppgifter utan datum räknas inte med, och det som gäller är när den bockades av, inte när jobbet egentligen blev klart. |

## 4. User stories med acceptanskriterier

Alla exempel använder svensk tid (`Europe/Stockholm`). I november är det UTC+1.

### Story 1: Ge en uppgift ett förfallodatum

**Som** teamledare **vill jag** kunna sätta ett förfallodatum på en uppgift **så att** alla vet
vilken dag den ska vara klar.

- **AK-1.1** Givet att det är 2026-11-16 i Sverige, när jag skapar en uppgift med förfallodatum
  2026-11-20, så svarar API:t 201 och uppgiften har `dueDate` = `2026-11-20` (ett datum, utan klockslag).
- **AK-1.2** ⚠️ improviserat (från Övning 6). Givet att det är 2026-11-16 i Sverige, när jag skapar en
  uppgift med förfallodatum 2026-11-15, så svarar API:t 400.
- **AK-1.3** Givet att det är 2026-11-16 i Sverige, när jag skapar en uppgift utan förfallodatum,
  så svarar API:t 201 och uppgiften har `dueDate` = `null`.

### Story 2: Se vilka uppgifter som är försenade

**Som** teammedlem **vill jag** se vilka uppgifter som är försenade **så att** jag kan agera innan
kunden ringer.

- **AK-2.1** Givet en uppgift med förfallodatum idag, när klockan är 23:59 svensk tid, så är den inte
  försenad. *(Det här är Saras bugg.)*
- **AK-2.2** Givet en uppgift som inte är klar och har förfallodatum 2026-11-16, när klockan är
  2026-11-17 00:00 svensk tid (2026-11-16 23:00 UTC), så är den försenad med 1 dag.
- **AK-2.3** ⚠️ improviserat. Givet en klar uppgift med förfallodatum 2026-11-10, när det är
  2026-11-16, så är den inte försenad.
- **AK-2.4** ⚠️ improviserat. Givet en uppgift utan förfallodatum, när som helst, så är den inte försenad.

*Den här storyn har fyra kriterier. AK-2.3 och AK-2.4 skulle kunna slås ihop, men då skulle två
utvecklare kunna skriva olika tester.*

### Story 3: Se vad jag ska göra först

**Som** teammedlem **vill jag** på morgonen se uppgifterna i den ordning de brådskar **så att** det
som ska vara klart först blir gjort först.

- **AK-3.1** Givet tre uppgifter som inte är klara, med förfallodatum igår, idag och imorgon, när jag
  hämtar arbetslistan, så kommer de i ordningen igår, idag, imorgon. ⚠️ improviserat: uppgifter med
  samma förfallodatum kommer i den ordning de skapades (lägst id först).
- **AK-3.2** Givet en uppgift utan förfallodatum och en med förfallodatum om 30 dagar, när jag
  hämtar arbetslistan, så kommer uppgiften utan förfallodatum sist.
- **AK-3.3** ⚠️ improviserat (upptäckt i use caset, undantag U1). Givet att API:t inte svarar, när
  arbetslistan laddas i appen, så visas ett felmeddelande, inte en tom lista.
- **AK-3.4** ⚠️ improviserat (upptäckt när testerna skrevs, alternativt flöde A5). Givet en klar
  uppgift med förfallodatum igår och en uppgift som inte är klar med förfallodatum imorgon, när jag
  hämtar arbetslistan, så kommer den klara uppgiften sist.

### Story 4: Mäta om vi blir bättre

**Som** teamledare **vill jag** se hur stor andel av uppgifterna som blir klara i tid **så att** jag
vet om teamet missar färre deadlines utan att behöva räkna för hand.

- **AK-4.1** Givet en uppgift med förfallodatum 2026-11-20, när den markeras klar 2026-11-20 kl 22:00
  svensk tid, så räknas den som klar i tid i `GET /api/todos/stats`.
- **AK-4.2** Givet en uppgift med förfallodatum 2026-11-20, när den markeras klar 2026-11-21 kl 00:30
  svensk tid, så räknas den som klar sent i `GET /api/todos/stats`.
- **AK-4.3** Givet att ingen uppgift med förfallodatum har markerats som klar, när jag hämtar
  `GET /api/todos/stats`, så är andelen klara i tid `null`, inte `0`.
- **AK-4.4** (upptäckt i use caset, undantag U2) Givet att det inte finns någon uppgift med id 999,
  när jag markerar uppgift 999 som klar, så svarar API:t 404.

## 5. Use case: Planera dagen

**Aktör:** Teammedlem
**Mål:** Veta vad som ska göras först idag.
**Förutsättning:** Det finns uppgifter i appen.

### Huvudflöde

1. Teammedlemmen öppnar appen på morgonen.
2. Appen hämtar arbetslistan från API:t.
3. API:t räknar ut vilka uppgifter som är försenade, utifrån dagens datum i svensk tid. *(AK-2.1, AK-2.2)*
4. Appen visar uppgifterna med den som brådskar mest först. *(AK-3.1, AK-3.2)*
5. Teammedlemmen börjar på den översta uppgiften och markerar den som klar när den är klar. *(AK-4.1)*

### Alternativa flöden

| Id | Flöde | Täcks av |
|---|---|---|
| A1 | Inget är försenat. Listan visar uppgifterna som ska vara klara idag först. | AK-3.1 |
| A2 | Uppgiften ska vara klar idag, och det är fortfarande idag (Kims bugg). Den visas inte som försenad. | AK-2.1 |
| A3 | En uppgift blir klar för sent. Den räknas som klar sent i statistiken. | AK-4.2 |
| A4 | En uppgift saknar förfallodatum. Den hamnar sist och blir aldrig försenad. | AK-3.2, AK-2.4 |
| A5 | En uppgift är redan klar. Den hamnar sist i listan, eftersom den inte behöver göras. | AK-3.4 *(saknades, lades till)* |

### Undantagsflöden

| Id | Flöde | Täcks av |
|---|---|---|
| U1 | API:t svarar inte. Appen visar ett felmeddelande. | AK-3.3 *(saknades, lades till)* |
| U2 | Uppgiften har tagits bort av någon annan. Att markera den som klar ger 404. | AK-4.4 *(saknades, lades till)* |

## 6. Icke-funktionella krav

| Id | Krav | Hur vi mäter |
|---|---|---|
| IFK-1 | `GET /api/todos` svarar på under 200 ms (p99) med 1 000 uppgifter och 20 samtidiga användare (8 personer plus marginal), utan fel. | Lasttest med autocannon, Övning 7 |
| IFK-2 | Att sortera arbetslistan med 1 000 uppgifter tar under 1 ms. | BenchmarkDotNet i Release, Övning 7 |
| IFK-3 | Alla datumregler använder svensk tid (`Europe/Stockholm`), oavsett vilken tidszon servern har. | Tester med `FakeTimeProvider` (AK-2.2) |

## 7. Utanför scope

Det här gör vi medvetet inte nu:

- Påminnelser via mejl eller notiser.
- Att tilldela uppgifter till en ägare. Sara nämnde att vissa uppgifter är "ingens" ⚠️ improviserat, men det är ett eget problem att utreda.
- Inloggning och olika användare.
- Paginering av listan (extra-övning).
- Att visa "Försenad" i frontendlistan (extra-övning).
