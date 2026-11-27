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
  - `owner-check`: „Oder w-wir prüfen einfach bei jeder Abfrage, wem das Logbuch gehört? D-das ist bestimmt zu einfach, oder?“
  - `guid-ids`: „U-und wenn die IDs gar keine Zahlen wären, sondern lange Zufallswerte? D-die errät doch keiner. Oder?“
  - `hide-in-ui`: „Die Ansicht zeigt doch sowieso nur die eigenen Logbücher. V-vielleicht sorgen wir einfach dafür, dass das auch so bleibt?“
  - `authorize-attribute`: „Ich k-könnte ein `[Authorize]` draufsetzen, Sir. Dafür ist das doch da, oder? Zum … Autorisieren?“
- Nachbesprechung:
  > Gute Arbeit, Mr. Crusher. Jetzt kann jeder nur noch sein eigenes Logbuch lesen. Commander
  > Riker lässt Ihnen danken und möchte wissen, wie viel Sie von seinem Logbuch gelesen haben.
  > Ich sagte ihm, Sie seien sehr gründlich gewesen. Als Kompliment hat er das nicht verstanden.

### 1.2 `bfla`: Zutritt zur Brücke
- Briefing:
  > Logbuch des Captains, Sternzeit 47322.1. Um drei Uhr nachts ging die komplette Crewliste
  > über alle Subraumfrequenzen hinaus, mit Namen, Quartieren und Dienstzeiten. Das Signal kam
  > von Bord, aber abgeschickt hat es niemand. Exportieren können die Liste nur die
  > Führungsoffiziere, und deren Konsolen waren die ganze Nacht unbenutzt. Mr. Barclay versichert
  > mir, dass alle anderen das Admin-Menü gar nicht erst zu sehen bekommen. Ich habe Mr. Crusher
  > gebeten herauszufinden, wie es trotzdem passieren konnte.
- Hinweise (Data):
  1. `GET /menu` liefert Ihnen nur einen Eintrag. Laut Code wird der Admin-Eintrag für Ihre Rolle einfach weggelassen. Was in einer Liste fehlt, ist deshalb aber nicht vom Schiff verschwunden.
  2. Die Gruppe `/admin` ruft `RequireAuthorization()` auf, ohne eine Policy oder Rolle zu nennen. In dieser Form verlangt sie nur, dass der Aufrufer angemeldet ist. Sie sind angemeldet.
  3. Ich würde erwarten, dass `GET /api/labs/bfla/admin/crew/export` mit Ihrem eigenen Token abgelehnt wird. Ich hätte allerdings auch erwartet, dass die Crewliste nicht um drei Uhr nachts gesendet wird.
- Visitenkarte (Lore):
  > Wesley! Hast du die Liste auch gefunden? Dann weißt du ja, was da bei dir steht:
  > „Fähnrich auf Probe“. Auf Probe, das tut weh, oder? Ich weiß jetzt, wo alle schlafen.
  > Als Nächstes will ich wissen, was das Schiff so geladen hat. Im Frachtmanifest gibt's
  > ein paar Zeilen, die keiner lesen soll. — L.
- Barclay:
  - `admin-policy`: „V-vielleicht sollte der Server die Rolle selbst prüfen? Für die ganze Admin-Gruppe? W-wenn das nicht übertrieben ist.“
  - `lock-admin-page`: „Das Menü ist ja schon versteckt, Sir. I-ich könnte zusätzlich die Admin-Seite absperren, mit einem Attribut? D-dann kommt keiner mehr rein.“
  - `secret-route`: „W-wir könnten die Route umbenennen. Irgendwas Langes, so wie `/admin-7f3a9c-do-not-use`. D-das tippt doch keiner ein. Oder?“
  - `referer-check`: „Und wenn der Export nur klappt, wenn man von der Admin-Seite kommt? W-wir könnten den `Referer`-Header prüfen. Die Seite können ja nur Admins öffnen, a-also …“
- Nachbesprechung:
  > Gute Arbeit, Mr. Crusher. Der Export verlangt jetzt die Rolle eines Führungsoffiziers und
  > nicht bloß eine gültige Anmeldung. Mr. Barclay wollte das versteckte Menü trotzdem behalten.
  > Ich habe es erlaubt, aber nur als Dekoration. Die Crewliste ist leider schon draußen.
  > Unser Besucher auch.

### 1.3 `sql-injection`: Das geheime Frachtmanifest
- Briefing:
  > Logbuch des Captains, Sternzeit 47323.6. Das Leck in der Crewliste ist gestopft, aber in
  > Zehn Vorne gibt es ein neues Lieblingsthema: einen geheimen Prototyp in unserem Frachtraum.
  > Keiner weiß, was es ist, aber alle wissen, dass es ihn gibt. Das Frachtmanifest blendet
  > geheime Posten bei jeder Suche der Crew aus. Trotzdem hat jemand hineingesehen. Ich habe
  > Mr. Crusher gebeten herauszufinden, wie das ging und worüber die Crew eigentlich redet.
- Hinweise (Data):
  1. Eine Suche nach `phaser` liefert zwei Posten. Der Code setzt Ihren Suchbegriff direkt in den SQL-Text, zwischen zwei einfache Anführungszeichen. Mich interessiert, was mit einem Suchbegriff geschieht, der selbst ein einfaches Anführungszeichen enthält.
  2. Ein Anführungszeichen in Ihrer Eingabe beendet den String vorzeitig, und alles danach wird als SQL gelesen. In SQLite macht `--` den Rest der Zeile zu einem Kommentar. Das beträfe auch die Bedingung `IsClassified = 0`.
  3. Eine Bedingung, die immer wahr ist, etwa `OR 1=1`, trifft auf jede Zeile zu. Leerzeichen im Pfad codiert die Request-Konsole selbst, Sie können sie also einfach eintippen.
- Visitenkarte (Lore):
  > Ein Emotionschip, Wesley. Vater hat ihn für Data gebaut. Nicht für mich, natürlich, nie
  > für mich. Den hol' ich mir später. Jetzt hab' ich erst mal genug davon, über Leute zu lesen.
  > Ich wär' lieber selbst einer von ihnen. Einer mit ein bisschen mehr Rang, schätz' ich. — L.
- Barclay:
  - `linq-where`: „W-wir könnten einfach LINQ nehmen, wie überall sonst? Dann gibt's keinen SQL-String, den man k-kaputt machen kann. Oder ist das geschummelt?“
  - `escape-quotes`: „Wenn das Anführungszeichen das Problem ist, d-dann verdoppeln wir es eben. Zwei Anführungszeichen, das mag SQL. Hab' ich irgendwo gelesen.“
  - `keyword-blocklist`: „W-wir könnten die gefährlichen Teile sperren: `--`, `OR`, Semikolons. Die braucht doch k-keiner für eine Frachtsuche. Oder?“
  - `string-format`: „Und wenn wir die Abfrage mit `string.Format` bauen statt mit Pluszeichen? D-das sieht viel professioneller aus.“
- Nachbesprechung:
  > Gut gemacht, Mr. Crusher. Die Suche behandelt Ihre Eingabe jetzt als Text und nicht mehr
  > als Befehl. Der geheime Posten ist wieder unter Verschluss. Mr. Data hat gebeten, dass er
  > vorerst dort bleibt. Er hat nicht gesagt, warum, und ich habe nicht gefragt.

### Ende von Akt 1

> Die Lichter auf der Brücke flackern einen Moment lang. Dann schaltet sich der Hauptschirm
> von selbst ein.
>
> Datas Gesicht blickt auf sie herab, mit einem Lächeln, das Data nie lächeln würde.
>
> „Captain. Commander. Und du, Bruder, siehst gut aus. Herrlich langweilig.“ Sein Blick wandert
> über die Brücke und bleibt am Steuer hängen. „Und der Junge. Natürlich ist es der Junge.“
>
> „Lore“, sagt Data leise.
>
> „Du räumst mir hinterher, Wesley. Logbücher, Crewlisten, Fracht. Sehr gründlich. Aber vom
> Lesen über diese Crew hab' ich genug.“ Er beugt sich vor. „Jetzt werd' ich selbst einer von euch.“
>
> Der Schirm wird schwarz. Riker sieht Wesley an. Zum ersten Mal sagt ihm keiner, er solle sich setzen.

---

## Akt 2 — „Falsche Identitäten“

*Lore gibt sich als jemand anderes aus.*

### 2.1 `mass-assignment`: Beförderung im Feld
- Briefing:
  > Logbuch des Captains, Sternzeit 47324.8. Seit Lores Auftritt führt die Crew-Datenbank einen
  > Admiral L. Soong in meiner Besatzung. Einen solchen Offizier gibt es nicht, und trotzdem hat
  > er Admin-Rechte und Zugriff auf die Kanäle des Sternenflottenkommandos. Laut Protokoll wurde
  > er nicht angelegt, sondern befördert, und zwar von einem ganz normalen Crew-Konto aus. Über
  > die Profilseite, die jeder an Bord benutzt. Ich habe Mr. Crusher gebeten, sich seine eigene
  > Profilseite einmal genauer anzusehen.
- Hinweise (Data):
  1. `GET /profile` liefert fünf Felder. Auf der Profilseite können Sie zwei davon bearbeiten. Mir fällt auf, dass der Server offenbar nicht weiß, welche zwei.
  2. `SetValues` kopiert jede Eigenschaft des eingehenden Objekts auf den gespeicherten Benutzer. Dabei unterscheidet es nicht zwischen Feldern, die das Formular zeigt, und solchen, die es nicht zeigt.
  3. Sie könnten die Theorie prüfen, indem Sie Ihr vollständiges Profil mit `"isAdmin": true` per `PUT /profile` zurückschicken und danach `GET /admin/secret` aufrufen. Von einer Änderung der `credits` rate ich ab. Guinan prüft die Rechnungen in Zehn Vorne.
- Visitenkarte (Lore):
  > Glückwunsch, Admiral Crusher. Steht dir. Fast. Ich bin seit einem Tag befördert und find's
  > schon langweilig, also war ich einkaufen. Kennst du die neue Bestell-App vom Quartiermeister?
  > So großzügig. Die drückt einem den Schlüssel zum Lager der Sternenbasis praktisch in die
  > Hand. — L.
- Barclay:
  - `dto-mapping`: „K-könnte die Profilseite eine eigene kleine Klasse kriegen? Nur Name und E-Mail? Ist ziemlich viel Tipparbeit für zwei Felder, Sir.“
  - `reject-isadmin`: „W-wir sagen einfach nein, wenn jemand `isAdmin: true` schickt. Das ist doch das gefährliche Feld. D-das einzige, oder?“
  - `remove-form-field`: „Ich n-nehme die Admin-Checkbox aus dem Formular. Komplett, nicht nur versteckt. W-weg.“
  - `validation-attributes`: „V-vielleicht bauen wir Validierung ein? `[Required]`, `[EmailAddress]`. Validierung ist doch immer gut, oder?“
- Nachbesprechung:
  > Gute Arbeit, Mr. Crusher. Die Profilseite nimmt jetzt nur noch Namen und E-Mail an, sonst
  > nichts. Admiral Soong hat seinen rechtmäßigen Rang zurück, nämlich keinen. Ich gehe davon aus,
  > dass Sie Ihren eigenen Admiralsrang auch wieder abgegeben haben. So große Beförderungen im
  > Feld sieht die Sternenflotte nicht gern.

### 2.2 `client-secrets`: Offene Frequenz
- Briefing:
  > Logbuch des Captains, Sternzeit 47326.0. Sternenbasis 74 hat uns eine Rechnung geschickt:
  > vierhundert isolineare Prozessorkerne, zwei Shuttle-Triebwerke und eine Kiste saurianischer
  > Brandy, alles in den letzten zehn Stunden von der Enterprise bestellt. Der Quartiermeister hat
  > nichts davon bestellt. Unterschrieben wurden die Bestellungen mit unserem eigenen Schlüssel
  > für das Lager, und den sollte nur die neue Bestell-App des Quartiermeisters kennen. Ich habe
  > Mr. Crusher gebeten herauszufinden, wer ihn sonst noch kennt.
- Hinweise (Data):
  1. Die Bestell-App ist eine Blazor-WebAssembly-App. Alles, was sie zum Laufen braucht, lädt sie zuerst in den Browser. Dazu gehört auch ihre Konfiguration aus `appsettings.json`, die als ganz normale statische Datei ausgeliefert wird.
  2. `GET /static/appsettings.json` verlangt kein Token. Bei statischen Dateien ist das selten anders. Den Abschnitt `Starbase` finde ich besonders aufschlussreich.
  3. Die API der Sternenbasis erwartet den Schlüssel im Header `X-Api-Key`. Wenn Sie ihn bei `GET /starbase/requisitions` mitschicken, sehen Sie, was in unserem Namen bestellt wurde.
- Visitenkarte (Lore):
  > Saurianischer Brandy, Wesley. Für die Siegesfeier. Du bist nicht eingeladen, du bist zu jung.
  > Einkaufen war lustig, aber ein Lagerschlüssel reicht mir nicht. Ich will den Ersten Offizier.
  > Wusstest du, dass sein Zugangscode nur vier Ziffern hat? Vier. Meine Einkaufslisten sind
  > länger. — L.
- Barclay:
  - `bff-proxy`: „U-und wenn die App gar nicht direkt mit der Sternenbasis redet? Sie fragt unseren Server, und der fragt die Sternenbasis. Ist ein Umweg, ich weiß. T-tut mir leid.“
  - `base64-key`: „W-wir könnten den Schlüssel codieren. Base64. Dann liegt er nicht einfach so im Klartext rum. Base64 liest doch k-keiner. Oder?“
  - `csharp-constant`: „Konfigurationsdateien kann jeder lesen, Sir. I-ich schreibe den Schlüssel einfach in den Code, als Konstante. Der wird ja kompiliert, also ist er sicher.“
  - `cors`: „Ich b-bitte Sternenbasis 74, CORS einzuschalten. Nur unsere Domain ist erlaubt. D-dafür ist CORS doch da, oder? Um andere draußen zu halten?“
- Nachbesprechung:
  > Gute Arbeit, Mr. Crusher. Der Lagerschlüssel liegt jetzt auf dem Server des Schiffs, und
  > die App fragt unseren Server statt direkt die Sternenbasis. Der alte Schlüssel ist gesperrt,
  > und die Bestellungen sind storniert. Bis auf den Brandy. Der war schon geliefert, und keiner
  > will mir sagen, wohin.

### 2.4 `open-redirect`: Falsche Koordinaten
- Briefing:
  > Logbuch des Captains, Sternzeit 47327.9. Commander Riker hat heute Morgen eine Nachricht vom
  > Sternenflotten-Support bekommen: Er möge sich nach dem Software-Update bitte neu anmelden. Er
  > hat auf den Link geklickt, sich auf unserer eigenen Anmeldeseite angemeldet und nicht weiter
  > darüber nachgedacht. Eine Stunde später hat sein Konto die Tore des Shuttlehangars auf Deck 13
  > geöffnet. Der Commander war zu der Zeit im Fitnessraum. Ich habe Mr. Crusher gebeten
  > herauszufinden, wohin dieser Link wirklich geführt hat.
- Hinweise (Data):
  1. `GET /login/continue` schickt Sie zu `returnUrl` und hängt einen Einmal-`code` an. Bei Ihrem eigenen Request sehen Sie das im Header `Location`. Mir fällt auf, dass der Code nie prüft, wohin `returnUrl` zeigt.
  2. Commander Riker klickt keine Links zu fremden Adressen. Einem Link auf unsere eigene Anmeldeseite vertraut er dagegen völlig. Mit `POST /support/messages` stellen Sie ihm einen solchen Link als Sternenflotten-Support zu.
  3. Alles, was bei `https://evil.example` ankommt, erscheint in `GET /attacker/inbox`. Ein Link auf unsere Anmeldeseite mit `returnUrl=https://evil.example/` wäre deshalb aufschlussreich. Es ist mir unangenehm, wie gut das funktionieren würde.
- Visitenkarte (Lore):
  > Hast ihn erwischt, was? Kluger Junge. Ich hab' drei Versuche gebraucht, du nur einen. Fast bin
  > ich stolz. Aber ich hab' genug davon, mir fremde Gesichter zu leihen. Ich will wissen, was
  > darunter steckt. Tief im Bordcomputer gibt's ein Archiv, in dem diese Crew alles aufhebt, was
  > sie lieber vergessen würde. Da fang' ich an. — L.
- Barclay:
  - `local-redirect`: „K-könnten wir nur Adressen auf unserem eigenen Schiff erlauben? Also lokale? I-ich glaube, dafür gibt's eine Hilfsfunktion. Es gibt immer eine Hilfsfunktion.“
  - `starts-with-slash`: „Lokale Adressen fangen mit einem Schrägstrich an. A-also prüfen wir auf den Schrägstrich. Ein Zeichen, fertig.“
  - `contains-domain`: „W-wir prüfen einfach, ob `enterprise.local` irgendwo in der Adresse steht. Wenn sie uns gehört, steht das doch drin. Oder?“
  - `strip-scheme`: „Ohne `https://` kein anderer Planet. I-ich entferne das einfach. Ohne Schema kann der Link doch nirgendwo hin. O-oder?“
- Nachbesprechung:
  > Gut gemacht, Mr. Crusher. Die Anmeldeseite schickt jetzt nur noch auf Seiten dieses Schiffs
  > zurück. Commander Riker hat den Sternenflotten-Support gebeten, ihm keine Links mehr zu schicken.
  > Der Support sagt, er habe ihm nie welche geschickt. Der Commander nimmt das persönlich.

### Ende von Akt 2

> Die Stimme über Kom klingt ruhig und präzise. „Brücke, hier spricht Data. Ich habe den
> Eindringling im Hauptcomputer isoliert. Um ihn endgültig zu stoppen, muss die Firewall des
> Hauptcomputers für neunzig Sekunden abgeschaltet werden.“
>
> Picard nickt Worf zu. Worfs Hand wandert zur Konsole.
>
> „Bitte beeilen Sie sich, Captain. Lang hält's nicht mehr.“
>
> Wesley sieht auf. „Sir, Data würde nie ‚hält's‘ sagen.“
>
> Auf der Brücke wird es still. Worfs Hand bleibt über der Konsole stehen.
>
> Über die Leitung lacht jemand. Es ist Datas Stimme, aber nicht Datas Lachen. „Kluger Junge.
> Dann behaltet eure Firewall. Ich komm' auf einem anderen Weg rein.“
>
> Die Verbindung bricht ab. An seiner Konsole dreht sich der echte Data um. „Captain, ich war die
> ganze Zeit hier.“

---

## Akt 3 — „Ins Herz des Schiffs“

*Lore greift nach dem Hauptcomputer.*

### Finale
- TODO
