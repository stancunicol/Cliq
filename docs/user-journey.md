# 🗺️ User Journey: Cliq

**Rol:** QA Automation / .NET Developer  
**Obiectiv:** Crearea, rularea și menținerea testelor Selenium C# & NUnit fără efort manual și fără riscul de a pierde cod.

---

### 1. Deschidere Workspace
* Deschizi proiectul în **Visual Studio** și accesezi panoul **Cliq Workbench**.
* Conexiunea la backend se face automat, fără configurări manuale de drivere.

---

### 2. Scanare Pagină Web
* Introduci URL-ul aplicației și apeși **Scan**.
* Cliq descoperă toate elementele interactive și alege cei mai stabili selectori (`data-testid` > XPath > CSS).

---

### 3. Compoziție Test
* Adaugi pașii doriți direct din interfață (`navigate`, `fill`, `click`, `assertVisible`, `assertText`).
* Asociezi secrete securizate pentru parole și token-uri.

---

### 4. Rulare & Debug Vizual (Replay)
* Apeși **Run Test** direct din Visual Studio.
* Testul rulează în Selenium. Dacă un pas eșuează, primești un **screenshot cu un chenar roșu desenat peste elementul cu probleme**.

---

### 5. Generare Cod C#
* Apeși **Generate Code**.
* Cliq creează automat clasele de **Page Object** și testele **NUnit** gata de rulare prin `dotnet test` sau în pipeline-ul de CI.

---

### 6. Extindere cu Metode Custom
* Deschizi fișierele C# generate în Visual Studio și adaugi metode sau validări proprii de business.

---

### 7. Re-scan & Review Sigur (Staged Scan Review)
* Când UI-ul aplicației se schimbă, lansezi o nouă scanare.
* **Ce se întâmplă:**
  * **Nu se suprascrie nimic direct**: modificările intră în *Staged Review*.
  * Vezi diferențele (elemente noi vs. lipsă) și alegi ce modificări aprobi.
  * **Protecție**: dacă vrei să ștergi un element folosit într-un test existent, sistemul te avertizează și blochează ștergerea.
  * **Metodele tale custom de C# sunt păstrate intacte** la regenerare.

---
