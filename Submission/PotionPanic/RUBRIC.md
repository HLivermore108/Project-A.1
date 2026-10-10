# Assignment requirement checklist

Use this folder as the Unity project and open it in **2022.1.7f1**.

| Required item | Implementation | How to demonstrate |
| --- | --- | --- |
| Required Unity version | Unity 2022.1.7f1, recorded in ProjectSettings/ProjectVersion.txt | Open in 2022.1.7f1; Windows build was produced by that editor. |
| At least five distinct Unity UI menus | Ten Canvas menu panels: Start, Modes, Orders, Ingredients, Brewing, Finishing, Pause, Options, Credits, Results | Play the scene and inspect Potion Panic Canvas / Design board. The menus use uGUI components, not OnGUI. |
| At least two gameplay mechanics | Measure ingredients by holding Pour; time a brewing meter and stirring; match bottle/garnish/ticket | Follow a guided practice order. |
| Goal structure | Serve five customers before closing; earn coins and tips based on four equally weighted ratings | Finish a shift and inspect the customer ledger. |
| At least one lose condition | The closing-time countdown reaching zero before all five customers are served | Start a shift and allow the clock to run out. Results identifies the shift as lost and offers retry. |
| Start screen leads to play and credits | Play opens mode selection; Credits opens the credits screen | Use both buttons from Start and return. |

## Additional features (five implemented; three required)

| Optional feature | Implementation |
| --- | --- |
| Crafting through a UI system | Ingredient preparation, brewing, stirring and bottling all use Unity UI controls. |
| Game mode selection with multiple variables | Select a 240, 180 or 135 second shift, independently of 22, 14 or 8 second customer arrivals. Untimed guided practice is also available. |
| Options menu with at least two working options | Master volume, window resolution and fullscreen. Settings are saved between launches. |
| Adaptable UI for multiple resolutions | Four selectable resolutions, including 16:9 and 4:3. CanvasScaler uses Scale With Screen Size / Expand. |
| Gameplay pause in menus | Pause and in-game Options freeze the closing timer, customer wait times and both cauldrons. Resume continues the shift. |

Music: Tabletop Jazz Cafe plays in pre-game menus, Forest during gameplay and its pause/options menus, and Redsen Game Music Afternoon on results. Tracks loop without restarting when changing stations. Master volume affects music and sound effects.

There is no claim of health, named high scores, an upgrade shop, saved-shift loading, equipment inventory, or an Itch deployment. The features above fulfill the three-or-more choice.

## Validation

- The project has been compiled and built with Unity 2022.1.7f1.
- 43 gameplay checks cover exact recipes, simultaneous cauldrons, capacity limits, overheating, correct tickets, payouts, deadline boundaries, post-loss input rejection, configurable arrivals, and practice behavior.
- The built-player UI check exercises actual Canvas buttons, a hold control and the volume slider; captures the menus at multiple resolutions; verifies pause/resume and mode settings; completes practice and a five-customer win; and verifies the loss screen.
- Screen captures and build logs are kept in the delivered workspace's validation outputs, outside the Unity source assets.
