# Tap Stack

A one-finger tower stacking game made with Unity. Tap the screen to drop the sliding block; the part that hangs over the block below is cut off and falls. The game gets harder as the block gets smaller.

## Requirements

- Unity 6000.5.9f1
- Scene: `Assets/Scenes/Main.unity`

## Running

1. Open the project from Unity Hub.
2. Open the `Assets/Scenes/Main.unity` scene.
3. Set the Game window to a portrait aspect ratio (for example 9:16) and press Play.

The scene only contains the `Game` object. The camera, light, UI and sounds are created from code when the game starts, so the scene looks empty before pressing Play.

The Unity menu item `TapStack > Projeyi Kur` rebuilds the scene and player settings. `TapStack > Kesim Hesabını Test Et` checks the block cutting math.

## Gameplay

- Tap the screen to drop the sliding block.
- The part of the block that does not overlap the block below is cut off.
- If the block is placed perfectly, it is not cut and grows a little.
- If the block misses the block below completely, the tower falls.
- The level ends when the target block count is reached. You earn 1 to 3 stars based on your perfect placement ratio.

## Features

- Main menu, level select and theme screens
- Unlimited levels: levels are generated from their number and each level is harder than the previous one
- Five block themes unlocked with stars
- Sound and vibration settings
- AdMob ads: watch an ad to continue after the tower falls, plus interstitial ads at intervals
- Cloud save with Unity Cloud Save

## Project structure

Files in the `Assets/Scripts` folder:

- `StackGame.cs`: main game flow, block cutting, camera and effects
- `LevelSystem.cs`: level generation and progress saving
- `ThemeSystem.cs`: block themes
- `GameUI.cs`, `MainMenu.cs`: in-game UI and menus
- `AdManager.cs`: ads
- `CloudSave.cs`, `SaveData.cs`: cloud save
- `SoundManager.cs`, `Haptics.cs`, `TapInput.cs`: sound, vibration and touch input
- `GuvenliAlan.cs`: keeps the UI inside the safe area on notched screens
- `MateryalTemizleyici.cs`: frees the materials of destroyed blocks from memory

## Settings

Values such as block size, speed limit and perfect placement margin are at the top of `StackGame.cs`, and level difficulty is at the top of `LevelSystem.cs`. Both can be changed from the Inspector.

With each new level the target block count goes up by 1. Starting speed goes up by 0.2 and stops at 6.5. Starting block width goes down by 0.06 and stops at 1.4.

## Notes

- Ads use Google's test IDs.
- Cloud save needs the project to be linked to Unity Cloud with Cloud Save and anonymous sign-in enabled. Without a connection the game keeps working with the local save.
- The `Handheld.Vibrate()` line in `Haptics.cs` adds the vibration permission on Android and should not be removed.
- If unrelated compile errors such as `UnityEngine.UI` not being found appear after changing packages, closing Unity and deleting the `Library` folder fixes it.

## Credits

Graphics and sounds are from Kenney (CC0), the font is Poppins from Google Fonts (OFL). The full list is in `Assets/Resources/LICENSES.txt`.
