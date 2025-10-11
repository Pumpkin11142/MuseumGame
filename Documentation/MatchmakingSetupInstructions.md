# How To Set Up Matchmaking (Super Simple Steps)

These directions use easy words so anyone can follow along. Read each step in order. Do not skip. You can ask a grown-up for help with the mouse and keyboard if needed.

## 1. Put the scripts in the right folders
1. Open the Project window in Unity.
2. Find the `Assets` folder.
3. Inside `Assets`, make two new folders if they do not exist yet:
   * A folder named `Scripts`.
   * Inside `Scripts`, make another folder named `Networking`.
   * Inside `Scripts`, make another folder named `UI`.
4. Drag these three C# files from your computer into Unity and drop them into the matching folders:
   * `MatchmakingNetworkManager.cs` goes in `Assets/Scripts/Networking`.
   * `MatchmakingRoomPlayer.cs` goes in `Assets/Scripts/Networking`.
   * `MainMenuMatchmakingUI.cs` goes in `Assets/Scripts/UI`.

## 2. Add the Matchmaking Network Manager to the scene
1. Open your main menu scene (the one that shows when the game starts).
2. In the Hierarchy window, click the `+` button (Create).
3. Choose **Create Empty**. A new empty object appears.
4. With the new object selected, go to the Inspector and rename it to **MatchmakingNetworkManager**.
5. Important: there should only be **one** NetworkManager in the whole scene. If you already have a different NetworkManager, delete it or swap its component for this new one so there are no duplicates when the game runs.
6. In the Inspector, click **Add Component**.
7. Search for `Matchmaking Network Manager` and click it. (It comes from the `MatchmakingNetworkManager.cs` script.)
8. Still in Add Component, search for your transport (for example `KCP Transport` or `Telepathy Transport`) and add it. Put the transport settings you want.
9. In the `Matchmaking Network Manager` component, set these fields:
   * **Room Player Prefab** – drag the prefab you will create in the next section.
   * **Game Player Prefab** – drag the player prefab that runs in the match.
   * **Online Scene** – choose the scene the match uses.
   * **Offline Scene** – choose the menu scene. Only this menu scene should contain the manager object; do **not** put another NetworkManager in the online scene.
   * **Players Per Match** – type how many players must be ready before a match starts (for example, `2`).
   * **Match Start Delay Seconds** – type how long the countdown should wait (for example, `3`).
   * **Require All Players Ready** – turn this on if every player must press Ready, or off if just the first few need to.

## 3. Make the room player prefab
1. In the Project window, right-click in `Assets` and choose **Create → Prefab Variant** or **Create → Prefab**.
2. Name the prefab **MatchmakingRoomPlayer**.
3. Double-click the prefab to open it in Prefab Mode.
4. In the Inspector, click **Add Component** and search for `Network Identity`. Add it.
5. Click **Add Component** again and search for `Matchmaking Room Player`. Add it.
6. If you want a visual object, add a model or UI child objects here (not required for networking to work).
7. Click the **Back** arrow above the Scene view to exit Prefab Mode.

## 4. Hook up the UI controller
1. Open your main menu scene again if you left it.
2. In the Hierarchy, find the Canvas that holds your menu buttons.
3. Select the button you want players to press to ready up. Make sure it has a **Button** component and a **Text** child that shows the word "Ready".
4. Add another button for canceling if you do not already have one. Name it `CancelButton`.
5. Add a Text object for showing status messages. Name it `StatusLabel`.
6. Add a Text object for the countdown numbers. Name it `CountdownLabel`.
7. Select the Canvas (or another object that makes sense) and click **Add Component**.
8. Search for `Main Menu Matchmaking UI` and add it.
9. Drag the `MatchmakingNetworkManager` object from the Hierarchy into the **Matchmaking Manager** slot so the script knows which manager to talk to. (If you forget, the script will try to find it automatically, but dragging it in keeps everything tidy.)
10. In the other fields, drag the UI objects to the matching slots:
   * Drag the Ready button into **Ready Button**.
   * Drag the Cancel button into **Cancel Button**.
   * Drag the Ready button's Text component into **Ready Button Label**.
   * Drag `StatusLabel` into **Status Label**.
   * Drag `CountdownLabel` into **Countdown Label**.
11. Change the words in the `Copy` section if you want different text.
12. If you want the game to host by itself when it cannot find another host, keep **Auto Host When Alone** checked. Turn it off if you do not want that.
13. The **Connection Timeout Seconds** box controls how long the Ready button waits before it gives up looking for another host and starts its own. A number like `3` means "wait three seconds, then host".
14. If you turn **Auto Host When Alone** off, make sure someone presses a separate "Host" button or runs a dedicated server before anyone hits Ready. Otherwise the Ready button will keep looking and never connect.
15. Double-check that the **Matchmaking Manager** slot points at the same object you set up in Step 2. If the object ever gets destroyed (for example because a duplicate manager was in the loaded scene), reassign this slot after you clean up the duplicate.

## 5. Test it
1. Press the **Play** button in Unity.
2. Click the Ready button in the Game view. The script will look for an existing host.
3. If no server is already running and **Auto Host When Alone** is on, wait a few seconds. You will see "Hosting match..." for a moment while the game becomes the server. As soon as your room player appears, it will press Ready for you.
4. Watch the status text. It should say it is waiting for more players (for example, "Waiting for players (1/2)").
5. Start a second copy of the game (another editor play mode or a built player) and join as a client if you want to test multiple people.
6. When enough players are Ready (or the Ready count reaches your **Players Per Match** number), the countdown will appear and then the game will switch to the Online scene.
7. If the scene changes but you only see a frozen lobby frame, make sure the `Game Player Prefab` is assigned and that the online scene does **not** contain another NetworkManager (that can break the swap to the gameplay player).
8. If you want to stop, press the Cancel button to leave the queue.

Take your time with each step. If something looks different, read the step again and check that the correct object is selected. You got this!
