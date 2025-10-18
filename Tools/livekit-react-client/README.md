# LiveKit React Client

A tiny React client to join a LiveKit room for quick local testing alongside the Unity app.

## Prereqs
- Node.js 18+
- A running token server from `Tools/livekit-token-server` (or any token endpoint that returns `{ token }`).
- A LiveKit server (Cloud or self-hosted). Defaults assume local dev `ws://localhost:7880`.

## Setup
1. `cd Mate-Engine/Tools/livekit-react-client`
2. Install deps: `npm install`
3. Copy env:
   - `copy .env.example .env.local` (Windows PowerShell)
   - Then edit `.env.local` to point to your token endpoint and LiveKit URL.

## Run
- Dev server: `npm run dev`
- Open the printed URL (usually http://localhost:5173)

The client will auto-request a token on load. If it fails, adjust identity/room and click `Get Token`.

## Notes
- Uses `@livekit/components-react` prefabs; audio-only by default.
- Styles come from `@livekit/components-styles/prefabs.css` imported in `src/main.jsx`.
- Token endpoint must accept `identity` and `room` query params and return `{ token }` or `{ accessToken }`.
