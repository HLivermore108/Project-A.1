# Potion Panic - assignment version

## Open the correct Unity project

This folder is the assignment project, using **Unity 2022.1.7f1**. In Unity Hub, add this folder as a project and select 2022.1.7f1. Open `Assets/Scenes/PotionPanic.unity` and press Play.

The required editor is installed on this computer at `C:/Users/speed/AppData/Local/Unity/Editors/2022.1.7f1/Editor/Unity.exe`. Use Hub's Locate option if it is not listed. The workspace's outer project retains its original Unity 6 settings and existing scenes. Its game scripts have also been updated, but use the separate 2022 project for the assignment.

To play without the editor, open **Builds/Windows/Potion Panic.exe**. Keep its data folder and supporting files beside it.

## How to play

1. Choose Play, select a shift length and customer arrival rate, then Start shift. Guided practice is an untimed introduction.
2. Take a customer's order and read the ticket. Click tickets to switch between active potions.
3. Select an ingredient and hold Pour. Release at the requested amount. Empty mixture lets you retry before brewing.
4. Start brewing and stop in the 8-10 second green zone. Other cauldrons keep brewing while you work elsewhere.
5. Hold Stir to 70, release, and click Finish.
6. Match the bottle, garnish, and ticket number, then serve.
7. Serve all five customers before closing to win. If the countdown reaches zero first, the shift ends in a loss.

There are three active ticket spaces and two cauldrons. Stirring occupies a cauldron until finished. Ingredients are unlimited. Inaccurate potions can still be served for fewer coins; the deadline is the lose condition.

Escape opens Pause. Resume continues all timers; Options also keeps an active shift paused. Losing application focus pauses gameplay.

## Menus and options

Ten named Canvas panels are created under `Potion Panic Canvas / Design board` during Play: Start, Modes, Orders, Ingredients, Brewing, Finishing, Pause, Options, Credits, and Results. These use Unity UI Images, Text, Buttons, Slider, GraphicRaycaster and an EventSystem. There is no OnGUI interface.

Options has a master volume slider, a sound test button, four window resolutions, and a fullscreen toggle. CanvasScaler fits the design to different aspect ratios. Settings persist between launches; current-shift progress does not.

## Music

- Main menu and pre-game menus: **Tabletop Jazz Cafe**, from LudoLoon Studio's Tabletop Jazz Cafe pack.
- Gameplay, pause, and in-game options: **Forest**, from Casual & Relaxing Game Music.
- Results screen, including a lost shift: **RedsenGameMusic_Afternoon_Cute_Casual_Loopable**, from Redsen Game Music.

All three tracks loop. Switching stations keeps the same song running. Music and sound effects share the master volume control. `PotionMusic.asset` in the Resources folder holds references to the imported audio; the source tracks remain in their original asset folders.

The Credits screen includes the music packs. To add your display name, select `Potion Panic Game` in the submission scene and edit Creator Name in the Inspector.

## Build, tests, and source

Use **Potion Panic > Build Windows game** in Unity. Inside the submission project, this builds to its own `Builds/Windows` folder. The delivered executable in the outer workspace's `Builds/Windows` folder was built using Unity 2022.1.7f1.

- `PotionGame.cs`: orders, measurements, timing, scoring, win/loss rules.
- `PotionPanicApp.cs`: menu flow and connections between controls and rules.
- `PotionUI.cs`: Canvas UI creation and simple artwork.
- `PotionHoldButton.cs`: press-and-hold input for pouring and stirring.
- `PotionMusic.cs` and `PotionMusicLibrary.cs`: track selection and audio references.
- `Editor/PotionPanicBuild.cs`: build command and audio setup.

`Tests/Run-Checks.ps1` runs 43 rule checks using Unity 2022's bundled compiler. An optional built-player check can be run with `-potion-capture "C:/absolute/output/folder"`; it uses actual Canvas controls, captures screens, verifies options and pause behavior, exercises both win/loss flows, checks music playback, then exits. This helper does not run during normal play.

See `RUBRIC.md` for the requirement mapping. WebGL/Itch publishing is not used as one of the optional features.

