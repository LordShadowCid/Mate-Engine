# LiveKit Token Server (Local Dev)

This tiny server mints Room Access Tokens for LiveKit using `livekit-server-sdk`.

## Setup
1. Copy `.env.example` to `.env` and fill values:
```
LIVEKIT_API_KEY=YOUR_API_KEY
LIVEKIT_API_SECRET=YOUR_API_SECRET
LIVEKIT_URL=wss://your-host.livekit.cloud
PORT=8787
```
2. Install deps and run (choose one):
```
# with npm
npm install
npm run dev

# or with yarn
yarn install
yarn dev
```

## Usage
- Request a token:
```
GET http://localhost:8787/token?identity=MateEngineUser&room=mate-engine-dev
```
- Response:
```
{ "token": "<JWT>", "url": "wss://your-host.livekit.cloud" }
```

## Unity wiring
- In `%AppData%/MateEngine/mateengine.json` add:
```
{
  "voice": { "provider": "livekit" },
  "livekit": {
    "url": "wss://your-host.livekit.cloud",
    "tokenEndpoint": "http://localhost:8787/token",
    "room": "mate-engine-dev",
    "identity": "MateEngineUser"
  }
}
```
- On Play, `LiveKitVoiceManager` will fetch the token from `tokenEndpoint` and proceed.

Security: never commit real keys; this is for local development only.