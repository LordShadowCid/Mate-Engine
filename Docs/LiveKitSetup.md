# LiveKit Setup for Mate Engine (Unity)

This guide walks you through enabling the LiveKit voice provider in Mate Engine, installing the LiveKit Unity SDK, adding the compile define, and validating end-to-end with a local token server and a simple React client.

## Prerequisites
- Unity 6 project opened (this repo)
- A LiveKit Cloud project (URL like `wss://<your>.livekit.cloud`)
- Ability to mint Room Access Tokens (Cloud dashboard, or the dev token server provided here)

## 1) Select the LiveKit provider
- Preferred (not committed): per-user config
  - File: `%AppData%/MateEngine/mateengine.json`
  - Minimal example:
```
{
  "voice": { "provider": "livekit" },
  "livekit": {
    "url": "wss://your-host.livekit.cloud",
    "token": "<your-room-access-token>",
    "room": "mate-engine-dev",
    "identity": "MateEngineUser"
  }
}
```
- Or set env vars (PowerShell):
```
setx MATEENGINE_VOICE_PROVIDER livekit
setx MATEENGINE_LK_URL "wss://your-host.livekit.cloud"
setx MATEENGINE_LK_TOKEN "<your-room-access-token>"
```

Tip: Instead of a static token, you can set a token endpoint and let Mate Engine fetch dynamically:
```
setx MATEENGINE_LK_TOKEN_ENDPOINT "http://127.0.0.1:8787/token"
```
The config loader also ships a sample at `Assets/StreamingAssets/mateengine.sample.json` and can write a per-user sample the first time you run.

## 2) Install the LiveKit Unity SDK and add the define
Mate Engine includes an SDK-backed adapter that compiles under the `LIVEKIT_SDK` define. Install the official LiveKit Unity SDK, then add the define:

1) Install the SDK (choose one):
   - Unity Package Manager → Add package from Git URL…
     - Use the Git URL from the official LiveKit Unity docs.
     - Example (subject to change): `https://github.com/livekit/client-unity.git#upm`
   - Or import a `.unitypackage` if LiveKit provides one.

   Notes:
   - Follow LiveKit’s Unity prerequisites (e.g., Unity WebRTC dependencies) if documented.
   - Ensure the package resolves to the expected namespace/types per LiveKit’s docs.

2) Add the compile define:
   - Unity: Edit → Project Settings → Player → Other Settings → Scripting Define Symbols
   - Add: `LIVEKIT_SDK`
   - Apply for your active platform target (e.g., PC, Windows).

3) Recompile scripts. In the Unity Console you should see `[LiveKitAdapterSdk]` logs when the LiveKit path is active.

## 3) Consent and permissions
- On first run, the Consent overlay appears.
- Enable Microphone (required for publishing your voice) and any optional integrations (e.g., Google).
- Reopen the overlay anytime with `F10`.

## 4) Run and test in Unity
- Press Play. With `voice.provider=livekit`, the app spawns `LiveKitVoiceManager`.
- The overlay (top-left) shows connection status and participants.
- Hotkeys:
  - Toggle connect: `F9`
  - Push-to-talk (publish mic): hold `V`
  - TTS test trigger: `T` (if your agent wiring includes it)

## 5) Optional: Local token server and web client
You can validate your setup with a local token server and a simple React client that joins the same room.

### Token server
- Path: `Mate-Engine/Tools/livekit-token-server`
- Commands (PowerShell):
```
cd "c:\Users\blakd\FirdayMateEngine\Mate-Engine\Tools\livekit-token-server"
copy .env.example .env
notepad .env   # fill LIVEKIT_API_KEY, LIVEKIT_API_SECRET, LIVEKIT_URL
npm install
npm run dev
```
- Health check: `http://127.0.0.1:8787/health` → should return `ok`
- Token endpoint: `http://127.0.0.1:8787/token?identity=User&room=mate-engine-dev`

Tip: We deliberately bind to `127.0.0.1` to avoid IPv6/localhost ambiguity.

### React client
- Path: `Mate-Engine/Tools/livekit-react-client`
- Commands (PowerShell):
```
cd "c:\Users\blakd\FirdayMateEngine\Mate-Engine\Tools\livekit-react-client"
copy .env.example .env.local
notepad .env.local  # set VITE_TOKEN_ENDPOINT and VITE_LIVEKIT_URL
npm install
npm run dev
```
- Open the printed URL and join the room.

If using VS Code in this workspace, you can also run the predefined task: “Start LiveKit React dev server”.

## 6) Smoke test checklist
1) Token server:
   - Visit `http://127.0.0.1:8787/health` → `ok`
   - Visit `http://127.0.0.1:8787/token?identity=SmokeTest&room=mate-engine-dev` → JSON with `{ token, url }`
2) React client:
   - Page retrieves a token successfully (or shows the full error including URL/status/body if it fails)
   - After “Connect”, you appear as a participant in the room
3) Unity:
   - Console shows `[LiveKitVoiceManager]` and `[LiveKitAdapterSdk]` logs
   - Overlay shows connected state after `F9`
   - Holding `V` publishes the mic; the React client should hear you

## Troubleshooting
- Overlay says `LIVEKIT_SDK not defined`:
  - Ensure the LiveKit Unity SDK is installed
  - Add `LIVEKIT_SDK` to Scripting Define Symbols for the active platform
- Connection issues:
  - Verify `livekit.url` and token are correct; try a newly minted token
  - If using the local server, confirm `/health` and `/token` work on `127.0.0.1`
  - Consider setting `MATEENGINE_LK_TOKEN_ENDPOINT` instead of a static token
- “Failed to fetch” in the React client:
  - The UI shows the exact request URL and server response; ensure your `.env.local` values are correct
  - Prefer `http://127.0.0.1:8787` over `http://localhost:8787`
- No audio publish:
  - Open Consent with `F10` and enable Microphone
  - Confirm your default input device in the OS

## Security tips
- Never commit API secrets or tokens.
- Prefer tokens minted by your backend (or LiveKit dashboard for dev).
- Keep secrets in `%AppData%/MateEngine/mateengine.json` or environment variables.

## Next steps
- Subscribe to your agent’s audio track for TTS playback.
- Switch to dynamic tokens by setting `livekit.tokenEndpoint` and removing static tokens from local files.
- Add a small UI to select room/identity at runtime.

## Appendix: SDK versions and namespaces
Because Unity packages and APIs can change over time, confirm your LiveKit SDK matches the adapter expectations:

- Source you installed
  - Git URL: <paste the URL you used, e.g., https://github.com/livekit/client-unity.git>
  - Branch/Tag/Commit: <e.g., upm, vX.Y.Z, or a specific SHA>

- Expected namespaces and core types (examples; verify against LiveKit docs):
  - Namespace: `LiveKit` (or similar per SDK)
  - Room type: `Room`
  - Connection: `await room.ConnectAsync(url, token)` or equivalent
  - Local track: `LocalAudioTrack` creation from microphone
  - Publish: `await room.LocalParticipant.PublishTrackAsync(track)` or similar

- Where this is used in Mate Engine
  - `Assets/MATE ENGINE - Scripts/Settings/LiveKit/LiveKitAdapterSdk.cs`
  - Compiles only when `LIVEKIT_SDK` is defined

- Quick verification
  1) In a C# script, type `using LiveKit;` and confirm there are no red squiggles
  2) Check that `Room`, `LocalParticipant`, and `LocalAudioTrack` (or analogous types) resolve
  3) Press Play with `voice.provider=livekit` and `LIVEKIT_SDK` defined; look for `[LiveKitAdapterSdk]` logs

If your SDK version uses different namespaces or method names, update `LiveKitAdapterSdk` accordingly (search for TODOs in that file). Once the types align, the adapter will bridge Unity coroutines to async SDK calls for connect/disconnect/publish.
