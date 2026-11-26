# Security Trainer — Story (Deutsch)

Die deutsche Fassung von `docs/story.md`. Beats, Struktur und Grenzen (Wortzahlen,
keine Spoiler) stehen dort; hier steht nur der deutsche Text. Labs übernehmen ihn
von hier.

## Schreibregeln

- Kein übersetztes Deutsch: so schreiben, als wäre das hier das Original.
  Sätze dürfen umgebaut, Bilder ausgetauscht werden.
- Namen wie in der deutschen Synchronisation: **Zehn Vorne**,
  „Logbuch des Captains, Sternzeit …“, Mr. Crusher, Mr. Data, Fähnrich.
- **Du** in technischen Texten (Szenario, Ziel, Feedback, Erklärung).
  Die Figuren siezen Wesley, nur Lore duzt ihn.
- Stimmen:
  - **Picard:** ruhig, knapp, förmlich.
  - **Data:** präzise, förmlich, **ohne Verkürzungen** („gibt es“, nie „gibt's“).
    Das braucht das Ende von Akt 2.
  - **Lore:** charmant, spöttisch, verkürzt gern („gibt's“, „nehm' ich“).
  - **Barclay:** nervös, stottert („I-ich …“, „V-vielleicht …“).
- **Gut lesbar:** normale, fließende Sätze. Kein Schachtelsatz, aber auch kein
  Telegrammstil. „und“, „aber“ und ein einfacher Nebensatz sind in Ordnung.
- Gängige IT-Begriffe bleiben englisch: Token, Flag, Policy, Request, Header.
- Alltagsdeutsch statt Synchron-Technikjargon: **Bordcomputer** / **Hauptcomputer**
  (nicht „Computerkern“), **Software-Update** (nicht „Überholung“ für den Refit).

## Eröffnungsszene

> In Zehn Vorne, der Bar der Enterprise, wird heute gefeiert. Die neue Software für den
> Bordcomputer ist drei Tage zu früh fertig geworden. Lt. Barclay wurde so oft zugeprostet,
> dass er sich inzwischen hinter einer Zimmerpalme versteckt.
>
> Wesley Crusher, der jüngste Fähnrich an Bord, steht etwas verloren am Rand. Mit ihm
> stößt keiner an.
>
> Auf der anderen Seite des Raums bricht Data mitten im Satz ab und legt den Kopf schief.
>
> „Captain, im Bordcomputer befindet sich eine positronische Signatur. Sie ist mit meiner
> identisch.“
>
> „Dann ist es wohl Ihre, Mr. Data.“
>
> „Nein, Sir. Ich bin hier.“
>
> Die Musik spielt weiter, und sonst hat niemand etwas bemerkt. Wesley stellt sein Glas ab.

---

## Akt 1 — „Ungebetener Gast“

*Lore schnüffelt in fremden Daten herum.*

### 1.1 `bola`: Ein fremdes Logbuch
- Briefing:
  > Logbuch des Captains, Sternzeit 47321.4. Eine Stunde nach dem Software-Update ist im
  > Bordcomputer eine unbekannte Signatur aufgetaucht. Jetzt meldet eine junge Offizierin, dass
  > sie fremde persönliche Logbücher lesen kann. Die sind laut Vorschrift eigentlich
  > vertraulich. Was sie lesen kann, kann auch der Eindringling lesen. Ich habe Mr. Crusher
  > gebeten, das mit seinem eigenen Zugang zu prüfen. Und zwar diskret, bevor Commander Riker
  > davon erfährt.
- Hinweise (Data):
  1. `GET /logs` liefert nur Ihre eigenen Einträge, 1001 und 1002. Mir fällt auf, dass die IDs fortlaufend nummeriert sind. Eine solche Reihe endet selten genau bei den eigenen Einträgen.
  2. `GET /logs/{id}` prüft, ob der Aufrufer angemeldet ist. Weitere Prüfungen habe ich nicht festgestellt.
  3. Commander Rikers Logbücher beginnen direkt nach Ihren. Das erste mit Ihrem eigenen Token abzurufen, wäre aufschlussreich. Bitte sagen Sie dem Commander aber nicht, dass der Vorschlag von mir kommt.
- Visitenkarte (Lore):
  > Hallo, Wesley. Na, im Tagebuch des Commanders gestöbert? Ich auch. Vierzig Minuten über
  > Posaunentechnik, wirklich packend. Aber Logbuch für Logbuch dauert mir zu lange. Irgendwo
  > auf diesem Schiff gibt's doch eine Liste der ganzen Crew. Die nehm' ich mir als Nächstes vor.
  > — L.
- Barclay:
  - `guid-ids`: „U-und wenn die IDs gar keine Zahlen wären, sondern lange Zufallswerte? D-die errät doch keiner. Oder?“
  - `hide-in-ui`: „Die Ansicht zeigt doch sowieso nur die eigenen Logbücher. V-vielleicht sorgen wir einfach dafür, dass das auch so bleibt?“
  - `authorize-attribute`: „Ich k-könnte ein `[Authorize]` draufsetzen, Sir. Dafür ist das doch da, oder? Zum … Autorisieren?“
- Nachbesprechung:
  > Gute Arbeit, Mr. Crusher. Jetzt kann jeder nur noch sein eigenes Logbuch lesen. Commander
  > Riker lässt Ihnen danken und möchte wissen, wie viel Sie von seinem Logbuch gelesen haben.
  > Ich sagte ihm, Sie seien sehr gründlich gewesen. Als Kompliment hat er das nicht verstanden.

### 1.2 `bfla` · 1.3 `sql-injection` · Ende von Akt 1
- TODO

---

## Akt 2 — „Falsche Identitäten“

*Lore gibt sich als jemand anderes aus.*

- TODO. Am Ende von Akt 2 verrät sich Lore, weil er eine Verkürzung benutzt
  („gibt's“, „hab'“), die Data nie benutzen würde.

---

## Akt 3 — „Ins Herz des Schiffs“

*Lore greift nach dem Hauptcomputer.*

- TODO
