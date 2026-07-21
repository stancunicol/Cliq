# Arhitectură și analiză — Etapa 1

**Proiect:** PageObjectGenerator — generator automat de Page Objects și teste NUnit pentru Selenium/C#
**Etapă:** Etapa 1 – Analiză și Design (16.07–24.07)
**Livrabil:** Arhitectură și analiză
**Referință:** „Propunere Soluție – Generator de Cod Page Object pentru testare UI" (Ciprian Neagoe)

## 1. Scop

Acest document fixează în scris direcția tehnică a soluției, pe baza a ceea ce
a fost deja construit și validat ca prototip funcțional în perioada 14–20.07.
Ordinea reală a lucrului a fost inversă față de calendar — implementarea a
precedat documentul de arhitectură — motiv pentru care acest document descrie
decizii deja luate și testate, nu doar intenții. Pentru instrucțiuni de rulare
și detalii de implementare linie-cu-linie, vezi [README.md](README.md); acest
document răspunde la „de ce așa", nu la „cum se rulează".

## 2. Rezumat al direcției tehnice

| Dimensiune | Decizie | Motivație pe scurt |
|---|---|---|
| Limbaj/platformă | C# / .NET 8 | Cerut explicit în document ("Suport recomandat: C#, .NET") |
| Automatizare browser | Selenium WebDriver 4.46 | Cerut explicit; înlocuiește motorul CDP propriu din PoC-ul Python anterior |
| Browsere | Chrome, Edge | Cerute explicit; rezolvare automată a driver-elor via Selenium Manager |
| Framework de test | NUnit 3 | Cerut explicit |
| Descoperire elemente | Scanare live a DOM-ului randat, prin JS injectat | Nu parsare HTML statică — necesar pentru aplicații randate client-side (React/Angular/Vue), aliniat cu Puppeteer/UI Spy din exemplele documentului |
| Componentă AI | Neimplementată | Vezi secțiunea 8 |
| Integrare Visual Studio | Neimplementată | Vezi secțiunea 8 |

## 3. Structura sistemului

```
PageObjectGenerator.Core        — bibliotecă: scanare DOM, generare cod, execuție live
PageObjectGenerator.Cli         — client local (linie de comandă)
PageObjectGenerator.Api         — client HTTP (REST + WebSocket), pentru integrare externă
PageObjectGenerator.Core.Tests  — teste unitare (logică pură, fără browser)
PageObjectGenerator.Demo.Tests  — teste E2E (compilează și rulează output-ul real)
```

Decizia de arhitectură centrală: **un singur `Core` reutilizat de doi clienți**
(CLI și API), nu două implementări paralele. `PageObjectWorkflow` (generare) și
`PageObjectRunner` (execuție live) sunt punctul unic de adevăr; CLI-ul și
API-ul sunt straturi subțiri peste ele. Asta a fost validat empiric — orice
bug găsit în timpul dezvoltării (escaparea selectorilor, gruparea radio-urilor,
rezolvarea căilor de fișiere) a fost reparat o singură dată, în `Core`, și
disponibil imediat în ambii clienți.

## 4. Fluxul de generare

1. Se lansează browser-ul (Chrome/Edge) prin Selenium, se navighează la URL.
2. Se așteaptă explicit `document.readyState === "complete"` — nu se
   presupune că `Navigate().GoToUrl()` a terminat tot randarea client-side.
3. Se injectează un script JS care parcurge `input, textarea, select, button,
   a[href], h1-h6`, excluzând elementele pe care un om nu le-ar putea vedea
   sau accesa (dimensiune zero, `display:none`, `visibility:hidden`).
4. Pentru fiecare element rămas, se alege un locator, în ordine de prioritate:
   `data-testid` → `id` → `name` → selector structural (`nth-child`) — fiecare
   candidat verificat că se potrivește **unic** cu elementul lui înainte de a
   fi acceptat (protecție împotriva `id`/`name` duplicate în markup invalid).
5. Se generează clasa Page Object C# (o metodă per element, folosind
   `By.Id`/`By.Name` nativ din Selenium unde locatorul permite, `By.CssSelector`
   doar unde nu există strategie nativă) și clasa de test NUnit corespunzătoare.
6. Se salvează codul + o captură de ecran de referință + un fișier de metadata
   (`.meta.json`, cu URL/browser/lista de elemente), necesar ulterior pentru
   re-testare fără reparsarea codului C# generat.

## 5. Modelul de acțiuni pe elemente

Șapte tipuri de acțiune, fiecare cu generare de cod și comportament de test
propriu:

| Tip | Sursă HTML | Ce generează |
|---|---|---|
| `Fill` | `input`/`textarea` text | completare + verificare prezență |
| `Click` | `button`, `a[href]`, `input[submit\|button\|reset]` | click + hover + verificare prezență |
| `Select` | `select` | selecție single + verificare prezență |
| `MultiSelect` | `select[multiple]` | selecție multiplă + verificare prezență |
| `Check` | `input[checkbox]` | bifare/debifare + verificare prezență |
| `Radio` | `input[radio]`, grupate după `name` | selecție din grup (o metodă per grup, nu per opțiune) + verificare prezență |
| `Assert` | `h1`-`h6` | citire text — singurul tip care **verifică conținut**, nu doar prezență (asertare de tip snapshot, text capturat la generare devine valoare așteptată) |

Decizie relevantă: testele generate pentru tipurile de interacțiune verifică
doar `Displayed`/`Enabled`, nu execută acțiunea cu valori inventate — motorul
nu are cunoștințe de domeniu despre ce e o valoare "validă" (ex. un login
real), iar declanșarea unui submit ar strica testele următoare din aceeași
suită. Execuția reală (cu valori concrete, furnizate de apelant) se face
separat, prin `/run`.

## 6. API și integrare externă

API-ul HTTP (`PageObjectGenerator.Api`) e gândit explicit ca punct de
conectare pentru un client viitor (extensie Visual Studio sau altul):

- `POST /api/page-objects` — generează; `className`/`namespace` **nu** sunt
  câmpuri acceptate din request (motiv de securitate, vezi secțiunea 7) —
  numele clasei se derivă mereu din URL.
- `GET /api/page-objects`, `GET .../{className}`, `GET .../{className}/elements`,
  `DELETE .../{className}` — operații CRUD de bază pe clasele generate.
- `POST .../{className}/run` — execută vizual acțiunile, sincron, răspuns
  complet la final.
- `WS .../{className}/run/ws` — variantă cu progres live, un mesaj JSON per
  pas finalizat, pentru clienți care nu vor să aștepte în tăcere 20-30+ secunde.
- `GET /api/screenshots/{fileName}` — servește efectiv bytes de imagine
  (`image/png`), nu doar o cale de fișier pe server.

Decizie deliberată: **fără sesiuni de browser persistente** între request-uri
(spre deosebire de modelul Session→Context→Page al API-ului Python anterior).
Fiecare operație de generare/rulare lansează și închide propriul browser.
Simplifică semnificativ starea server-ului, cu costul unei latențe per-request
mai mari — acceptabil pentru un PoC, de reevaluat dacă un client va face
apeluri foarte frecvente.

## 7. Securitate

Două vulnerabilități reale au fost identificate și reparate în timpul
dezvoltării, ambele cu aceeași rădăcină — input extern nesanitizat folosit
direct în generare de cod și în căi de fișiere:

1. **Injecție de cod C#** — un `className`/`namespace` liber, controlat de
   apelant, ajungea nesanitizat în `public sealed class {className}` din
   codul generat.
2. **Path traversal** — același `className` ajungea nesanitizat în
   `Path.Combine(outDir, $"{className}.cs")`; o valoare de tip
   `../../../secrets` (sau o cale absolută, pe care `Path.Combine` o
   substituie complet) putea scrie/citi fișiere în afara `GeneratedPages/`.

Rezolvare: `className`/`namespace` nu mai sunt câmpuri acceptate la generare
(se derivă mereu din URL); rutele care tot au nevoie de un nume de clasă din
exterior (`GET`/`DELETE`/`run`) validează prin `IdentifierNaming.IsSafeIdentifier`
înainte ca valoarea să atingă sistemul de fișiere. Aceeași validare a fost
extinsă la numele de fișiere de screenshot.

**Rămas neadresat, cunoscut și acceptat pentru un PoC local**: politica CORS e
permisivă (`AllowAnyOrigin`), fără autentificare. De restrâns explicit înainte
ca API-ul să fie expus dincolo de o mașină de dezvoltare locală.

## 8. Ce nu e implementat și de ce

Documentul de propunere cere explicit două componente pe care acest prototip
nu le atinge:

- **Integrare Visual Studio** (Add-in/Extension) — API-ul HTTP + WebSocket din
  secțiunea 6 e construit ca fundație pentru asta, dar nicio extensie VS nu
  există încă. Rămâne obiectiv pentru Etapa 2/3.
- **Componentă AI** — generarea e complet determinist-bazată pe reguli
  (parsare DOM + șabloane de cod). Nu a fost nevoie de AI pentru a rezolva
  identificarea elementelor sau generarea de cod în forma actuală; un punct
  de integrare plauzibil pentru Etapa 2 ar fi denumirea semantică a
  elementelor (numele de metode generate acum vin din `id`/`name`/text HTML,
  nu dintr-o înțelegere a scopului elementului) sau generarea de scenarii de
  test la nivel mai înalt decât un singur element.

Alte limitări cunoscute, descoperite empiric:

- **Un singur snapshot al DOM-ului** — elemente care apar doar după o
  interacțiune (meniuri, modale) nu sunt descoperite; motorul nu explorează
  interactiv pagina, doar o citește o singură dată.
- **Coliziune de nume între domenii diferite** — `className` se derivă din
  ultimul segment al URL-ului; două pagini `/login` de pe domenii diferite ar
  produce ambele `LoginPage` și s-ar suprascrie reciproc.
- **Fără diffing vizual** — capturile de ecran sunt imagini de referință la
  momente fixe (generare, eșec), nu comparații pixel-cu-pixel cu evidențierea
  regiunilor modificate.
- **Fără teste de integrare pentru API** — endpoint-urile au fost validate
  manual (curl/PowerShell) în timpul dezvoltării, nu există suită automată
  care le exercită pe toate.

## 9. Testare — piramidă cu două niveluri

- **Teste unitare** (`Core.Tests`, 29 de teste, <5s) — logică pură: naming,
  escapare de selectori, generare de cod, construite din `ElementDescriptor`
  scrise manual, fără browser.
- **Teste E2E** (`Demo.Tests`, 6 teste, ~25-30s) — compilează și rulează chiar
  fișierele produse de CLI/API, contra unei pagini reale, prin Selenium.

Alegere deliberată de a nu avea un nivel intermediar (teste de integrare fără
browser real, cu mock-uri peste `IWebDriver`) — pentru volumul curent de
logică, cele două niveluri existente acoperă atât corectitudinea generării
cât și corectitudinea execuției reale, fără complexitatea suplimentară a unui
strat de mock-uri.

## 10. Plan pentru etapele următoare

Pentru Etapa 2 ("Proof of Concept" — prototip funcțional, 27.07–21.08), care
în mare parte există deja ca urmare a dezvoltării din 14-20.07, rămân de
adresat, în ordine de prioritate:

1. Coliziunea de nume între domenii (secțiunea 8) — fix ieftin, risc de bug
   tăcut real.
2. Un prim punct de integrare Visual Studio — chiar și un client minimal care
   apelează API-ul existent și afișează rezultatul într-un panou.
3. Restrângerea CORS + un mecanism minimal de autentificare, dacă API-ul
   urmează să fie accesat dincolo de o mașină locală.
4. Un prim punct de integrare AI — cel mai probabil pe denumirea semantică a
   elementelor, unde beneficiul e clar și riscul de a introduce nedeterminism
   în codul generat e limitat la nume de metode, nu la logica de acțiune.
