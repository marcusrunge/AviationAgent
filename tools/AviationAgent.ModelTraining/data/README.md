# Trainingsdaten

Die echten Trainings- und Validierungsdaten werden aus Datenschutz- und Repository-Gründen nicht mitgeliefert. Lege `train-v2.jsonl` und `validation-v2.jsonl` in diesem Verzeichnis ab. Jede Zeile muss ein JSON-Objekt mit `user` und `assistant` enthalten. `assistant` ist das exakt erwartete JSON-Objekt als Zeichenfolge.

Beispiel:

```json
{"user":"METAR von ETHS","assistant":"{\"action\":\"get_metar\",\"station\":\"ETHS\",\"focus\":\"full\"}"}
```

Zulässige Felder der Agentenantwort:

```json
{"action":"get_metar|get_taf|get_metar_and_taf|explain_previous_result|filter_previous_result|unsupported_operational_decision|unknown","station":"ICAO oder null","focus":"full|wind|visibility|clouds|weather|temperature|pressure|validity|changes|worst_conditions|none"}
```
