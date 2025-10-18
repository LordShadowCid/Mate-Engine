import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import { AccessToken } from 'livekit-server-sdk';

dotenv.config();

const {
  LIVEKIT_API_KEY,
  LIVEKIT_API_SECRET,
  LIVEKIT_URL,
  PORT = 8787,
} = process.env;

if (!LIVEKIT_API_KEY || !LIVEKIT_API_SECRET) {
  console.warn('[token-server] Missing LIVEKIT_API_KEY or LIVEKIT_API_SECRET in env');
}

const app = express();
app.use(cors());
app.use(express.json());

app.get('/health', (req, res) => {
  res.status(200).send('ok');
});

app.get('/token', async (req, res) => {
  const identity = req.query.identity || 'MateEngineUser';
  const room = req.query.room || 'mate-engine-dev';

  try {
    console.log(`[token-server] /token request from ${req.ip} identity=${identity} room=${room}`);
    const at = new AccessToken(LIVEKIT_API_KEY, LIVEKIT_API_SECRET, {
      identity,
      ttl: '1h',
    });
    at.addGrant({ roomJoin: true, room });
  const jwt = await at.toJwt();
  const token = typeof jwt === 'string' ? jwt : (Buffer.isBuffer(jwt) ? jwt.toString('utf8') : String(jwt));
  res.json({ token, url: LIVEKIT_URL });
  } catch (e) {
    console.error(e);
    res.status(500).json({ error: 'failed to mint token' });
  }
});

app.listen(PORT, '127.0.0.1', () => {
  console.log(`[token-server] listening on http://127.0.0.1:${PORT}`);
});
