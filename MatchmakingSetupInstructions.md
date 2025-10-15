# Matchmaking Setup Guide

These steps walk through everything you need to add to Unity so the matchmaking scripts work. Follow them in order. Every step uses plain words and explains where to click.

## 1. Add the scripts to your project
1. Open your Unity project.
2. In the **Project** window, create a folder named `Scripts/Networking` (any folder name is fine; this just keeps things tidy).
3. Drag the following files from this repository into that folder:
   - `MatchmakingNetworkManager.cs`
   - `MatchmakingDiscovery.cs`
   - `MainMenuMatchmakingUI.cs`

## 2. Prepare the Network Manager prefab
1. In the **Hierarchy**, create an empty GameObject and call it `NetworkManager`.
2. With `NetworkManager` selected, click **Add Component** and search for `Matchmaking Network Manager`. Add it.
3. Add another component named `Kcp Transport` (this transport is bundled with Mirror and works for LAN and internet play). Keep the default values for now.
4. Expand the `Matchmaking Network Manager` component:
   - **Gameplay Scene Name**: type the name of the scene that should load when the match begins (for example `RoundScene`). Make sure that scene is listed in **File → Build Settings… → Scenes In Build**.
   - **Required Players**: leave this at `2` if you want two players per match. Change the number if your game needs a different team size.
   - **Discovery Window Seconds**: how long we look for an existing host before we host ourselves. Three seconds is a good default.
5. Under the same `NetworkManager` GameObject, click **Add Component** again and add `Matchmaking Discovery`.
6. On the `Matchmaking Discovery` component you can edit **Advertised Server Name**. This is the friendly label other players will see in the Unity Console when a host is found.
7. Still on `NetworkManager`, find the field labelled **Discovery** inside the `Matchmaking Network Manager` component. Drag the `Matchmaking Discovery` component onto this slot.
8. (Optional) If you already have a `NetworkManager` prefab in your project, add the `Matchmaking Network Manager` and `Matchmaking Discovery` components to that prefab instead of making a new GameObject.

## 3. Create the main menu UI bindings
1. Open your main menu scene (the one that shows the Ready button).
2. In the **Hierarchy**, create an empty GameObject named `MatchmakingUI` and reset its position (right–click the Transform component and choose **Reset**).
3. With `MatchmakingUI` selected, click **Add Component** and add `Main Menu Matchmaking UI`.
4. In the Inspector, you will see fields that need references:
   - **Matchmaking Manager**: drag the `NetworkManager` GameObject from the Hierarchy onto this slot.
   - **Status Label**: drag the TextMeshPro element that displays status text (for example, a `TMP_Text` labelled `StatusLabel`).
   - **Ready Button**: drag your existing Ready button.
   - **Cancel Button**: create a UI Button named `CancelButton` if you do not already have one. This button appears while searching so players can stop matchmaking. Drag it into the field.
5. The script already fills in friendly default phrases. Feel free to edit the text boxes (Idle/Searching/Connecting/Hosting/Match Ready) so they match your tone.

## 4. Wire the buttons
1. Select the Ready button in the Hierarchy.
2. In the Button component, scroll to **On Click ()** and press the `+` button to add a listener.
3. Drag the `MatchmakingUI` GameObject into the empty object field that appears.
4. Click the dropdown that currently shows `No Function`, go to `MainMenuMatchmakingUI`, and pick `OnReadyPressed`.
5. Repeat the same steps for the Cancel button, but choose `MainMenuMatchmakingUI → OnCancelPressed`.
6. Make sure the Cancel button is hidden by default (uncheck the GameObject in the Hierarchy or remove the `active` check box). The script will show it only while searching.

## 5. Assign player prefabs
1. Select the `NetworkManager` GameObject again.
2. In the `Matchmaking Network Manager` (Network Manager) component, set **Player Prefab** to the player object you already use in your game (for example, the prefab that contains `PlayerParkourController`).
3. If your game uses custom spawn logic, fill in **Start Positions** or keep the default spawn system.

## 6. Add scenes to build settings
1. Open **File → Build Settings…**.
2. Make sure both your main menu scene and your gameplay scene are in the **Scenes In Build** list. Drag them from the Project window if needed.
3. Move the main menu scene to the top of the list so it loads first.

## 7. Playtest the matchmaking flow
1. Press **Play** in the Unity Editor.
2. Click the Ready button. After a short search the Editor should start hosting and show the hosting message.
3. Build a standalone player (or open a second Editor window using **File → Build And Run**) and click Ready. The second instance should discover the host within a few seconds, connect, and then both clients should load the gameplay scene.
4. Use the Cancel button to confirm matchmaking stops correctly.

## 8. Prepare for online tests
- When testing over the internet (not just on your local network), forward the port listed in the `Kcp Transport` component (default 7777) on the host’s router, or replace the transport with Mirror’s `SimpleWebTransport` if you plan to route through web sockets.
- For cross-network play, make sure both players run the same build of the game so the discovery messages match.

You now have a full ready-up flow: the first player hosts, later players automatically join, and the gameplay scene loads once enough players are present.
