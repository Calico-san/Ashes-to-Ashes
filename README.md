# Ashes to Ashes

2D strateška igra preživljavanja u stvarnom vremenu. Vulkan na otoku New Haven
uskoro će eruptirati — kao guverner kolonije upravljaš resursima i radnom snagom
da izgradiš brodove i evakuiraš što više od 1100 stanovnika prije erupcije.

Studentski projekt na kolegiju *Dizajn i programiranje računalnih igara*,
Sveučilište Jurja Dobrile u Puli, 2025./2026.

## Pokretanje

1. Otvori projekt u **Unity 6 (6000.3.11f1)**.
2. Otvori scenu `Assets/Scenes/MainMenu.unity`.
3. Pritisni **Play**.

Scene: `MainMenu` → `Intro` → `AshesToAshes` (igra) → `TheEnd`.

## Kontrole

| Tipka | Radnja |
| --- | --- |
| Lijevi klik | Odabir građevine, gradilišta ili broda; postavljanje u načinu gradnje |
| Desni klik | Izlazak iz načina gradnje |
| WASD / strelice | Pomicanje kamere |
| Kotačić miša | Zoom |
| Esc | Izlaz iz načina gradnje, inače pauza i spremanje |

## Struktura koda

`Assets/Scripts/` — 50 skripti podijeljenih po odgovornosti:

| Mapa | Sadržaj |
| --- | --- |
| `Balance/` | Sve brojke i formule ekonomije, bez ovisnosti o Unityju |
| `Core/` | Stanje igre, vrijeme, validacija postavljanja, spremanje |
| `Buildings/` | Građevine, gradilišta, brodovi |
| `Workers/` | Radnici i inženjeri |
| `Tilemap/` | Podaci o karti i iscrtavanje terena |
| `Input/` | Kamera i odabir objekata |
| `UI/` | HUD, paneli, događaji, ciljevi |
| `Bootstrap/` | Inicijalizacija fiksnim redoslijedom |

Ovisnosti idu u jednom smjeru — od sučelja prema simulaciji, nikad obrnuto.

## Balans

Sve brojke su u `Assets/Scripts/Balance/BalanceConfig.cs`. Nijedna vrijednost
nije upisana u logiku, pa se balans mijenja bez diranja koda.

Karta se uređuje kroz vlastiti alat u Inspectoru na
`Assets/Resources/IslandMap.asset`, a grafika se povezuje u
`Assets/Resources/SpriteRegistry.asset`.

## Spremljene igre

Tri mjesta, JSON, u `Application.persistentDataPath/saves/`.
Na Windowsu: `%USERPROFILE%\AppData\LocalLow\<tvrtka>\<igra>\saves\`.

## Tim

| Član | Dio |
| --- | --- |
| Mislav Balaž | Sučelje i scena |
| Laura Đurinec | Događaji, nada, ciljevi, tutorial |
| Igor Pavlić | Simulacijska jezgra: vrijeme, ekonomija, kamera, gradnja, spremanje |
| Tomislav Rakuljić | Grafika, animacije, zvuk |

## Načela

Projekt slijedi KISS, YAGNI, SRP i LCP. Dokument dizajna objašnjava gdje je
svako od njih promijenilo konkretnu odluku.