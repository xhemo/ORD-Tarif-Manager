# ORD Tarif Manager

Moderne, native Windows-WPF-Desktopanwendung zur Erstellung, Bearbeitung, Validierung und Bereitstellung von Tariftabellen und XML-Tarifdateien im ORD-Format.

## Highlights & Funktionen
- **100% Native Vanilla-WPF:** Modernes, performantes Dark-Theme ohne externe UI-Bibliotheken oder Abhängigkeiten.
- **Out-of-the-Box Lauffähig:** Basiert auf dem nativ in Windows 10/11 vorinstallierten .NET Framework 4.8.1 (kompatibel mit C# 5.0).
- **Single-File-Executable:** Vollständig portabel als eigenständige .exe – kein Setup oder Installer notwendig.
- **Alle Tarifspezifikationen:**
  - SteppedWeightDistanceConsolidation (Umsatz, Gewicht kg)
  - SteppedVolumeDistanceConsolidation (Umsatz, Volumen m³)
  - (TC)SteppedWeightDistanceConsolidation (Kosten, Gewicht kg)
  - (TC)SteppedVolumeDistanceConsolidation (Kosten, Volumen m³)
- **Leistungsfähige Tabellen-Werkzeuge:**
  - Direkte Zellbearbeitung mit automatischem Pre-Export Commit
  - Sammelanpassung (prozentuale Zu- und Abschläge)
  - Matrix-Import (z. B. aus Excel / TSV)
  - Spaltenfilterung & Schnellsuche
  - Live-Umschaltung von Spezifikationen & Auftragsarten (Lieferung / Retoure)
- **ORD-Outbound-Integration:**
  - Schneller Direkt-Export auf Test- und Produktionsverzeichnisse
  - Pre-Flight Überschreibwarnung mit Zeitstempel
  - Übersicht der zuletzt hochgeladenen Tarife mit Schnell-Ladefunktion per Doppelklick

## Build-Anleitung
Kompilierung mit dem integrierten Build-Skript:
`cmd
build.cmd
`
Es wird der native Windows-Compiler csc.exe verwendet. Alle XAML-Dateien und Ressourcen werden direkt in die EXE eingebettet.

## Lizenz
(c) 2026 Xhemajl Dvorani. Alle Rechte vorbehalten.
