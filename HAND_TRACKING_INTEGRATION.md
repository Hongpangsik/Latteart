# tracking_agent — XR Hands integration checklist

This branch is the only integration target. Do not merge into main until verified in Unity and on Quest.

## Implemented
- `Assets/Scripts/LatteHandPitcherBridge.cs`: follows a selected XR hand palm, smooths position/rotation, detects thumb/index pinch, and disables pouring when tracking is lost.
- `Assets/Code/MilkPourController.cs`: optional `Require Pinch To Pour` flag; when off, the original tilt-driven editor workflow remains unchanged.
- Existing painter, heart controller, surface mesh and scoring scripts remain unchanged.

## Unity setup (LattePlay)
1. Confirm the Console has no compilation errors.
2. The current LattePlay scene contains the SteamPitcher and existing Main Camera, but **does not yet contain an XR Origin**. Add/configure the XR Origin (VR) from the HandTracking_Test scene, and avoid two active AudioListeners/cameras.
3. Add `LatteHandPitcherBridge` to a scene object (e.g. SteamPitcher). Set **Pitcher** to SteamPitcher, **Milk Pour Controller** to its MilkPourController, and **Xr Origin** to the XR Origin (VR) root transform. The bridge's own transform is not used as tracking origin.
4. Choose **Use Right Hand** (or left hand). Tune **Local Position Offset** and **Local Euler Offset** so the virtual pitcher spout points correctly when held.
5. On SteamPitcher's MilkPourController enable **Require Pinch To Pour** only when ready to test XR. Disable it for the original editor tilt-only demo.
6. On Quest, verify tracked hand moves the pitcher; tilt + pinch produces a milk stream, release pinch or lose tracking stops the stream; the painter still paints and the heart finish is detected.
7. Verify the scoring button or `A` key calls PatternAnalyzer.Analyze. Quest needs a VR-accessible UI trigger; keyboard `A` is editor-only.

## Known limitations
- Not yet compiled or tested in Unity/Quest.
- No XR Origin or hand visualizer is wired into LattePlay automatically; importing the test-scene setup requires Unity scene/prefab verification.
- No cup grab/hold implementation yet.
- The bridge reads XR Hands directly rather than consuming HandTrackingLogger's LPF data; HandTrackingLogger is preserved unchanged.
- XR joint tracking coordinates require a correct XR Origin reference and may require device-specific calibration.
