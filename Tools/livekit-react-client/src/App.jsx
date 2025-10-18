import React, { useCallback, useEffect, useMemo, useState } from 'react'
import { LiveKitRoom, useToken } from '@livekit/components-react'

// Config is provided via Vite env vars at build-time.
const TOKEN_ENDPOINT = import.meta.env.VITE_TOKEN_ENDPOINT || 'http://localhost:3001/token'
const LIVEKIT_URL = import.meta.env.VITE_LIVEKIT_URL || 'ws://localhost:7880'
const DEFAULT_ROOM = import.meta.env.VITE_ROOM || 'mate-dev'

async function fetchToken(identity, room = DEFAULT_ROOM) {
  const url = new URL(TOKEN_ENDPOINT)
  url.searchParams.set('identity', identity)
  url.searchParams.set('room', room)
  const u = url.toString()
  try {
    const res = await fetch(u)
    if (!res.ok) {
      const text = await res.text().catch(() => '')
      throw new Error(`Token server error ${res.status} ${res.statusText} at ${u}\n${text}`)
    }
    const data = await res.json()
    return data.token || data.accessToken
  } catch (e) {
    throw new Error(`Failed to fetch token from ${u}: ${e.message || e}`)
  }
}

export default function App() {
  const [identity, setIdentity] = useState(() =>
    `react-${Math.random().toString(36).slice(2, 8)}`,
  )
  const [room, setRoom] = useState(DEFAULT_ROOM)
  const [token, setToken] = useState('')
  const [error, setError] = useState('')

  const requestToken = useCallback(async () => {
    setError('')
    try {
      const t = await fetchToken(identity, room)
      setToken(t)
    } catch (e) {
      setError(String(e))
    }
  }, [identity, room])

  useEffect(() => {
    // auto request token on load for convenience
    requestToken()
  }, [requestToken])

  const onDisconnected = useCallback(() => {
    setToken('')
  }, [])

  const content = useMemo(() => {
    if (!token) {
      return (
        <div style={{ padding: 16 }}>
          <h2>LiveKit React Client</h2>
          <p style={{ color: '#666' }}>
            Configure your token server and LiveKit URL via
            <code> .env </code> variables.
          </p>
          <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
            <input
              value={identity}
              onChange={(e) => setIdentity(e.target.value)}
              placeholder="identity"
            />
            <input
              value={room}
              onChange={(e) => setRoom(e.target.value)}
              placeholder="room"
            />
            <button onClick={requestToken}>Get Token</button>
          </div>
          {error && (
            <pre style={{ color: 'crimson', whiteSpace: 'pre-wrap' }}>{error}</pre>
          )}
        </div>
      )
    }

    return (
      <LiveKitRoom
        token={token}
        serverUrl={LIVEKIT_URL}
        connect
        video={false}
        audio
        onDisconnected={onDisconnected}
        style={{ height: '100vh' }}
      >
        {/* Prefab UI: simple connection status and local tracks controls */}
        <div className="lk-grid-layout">
          <div className="lk-video-conference">
            <div className="lk-participant-grid"></div>
            <div className="lk-participant-sidebar"></div>
            <div className="lk-control-bar"></div>
          </div>
        </div>
      </LiveKitRoom>
    )
  }, [token, error, identity, room, requestToken, onDisconnected])

  return content
}
