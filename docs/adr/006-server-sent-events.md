# 006. Server-Sent Events for Real-Time Updates

Date: 2024-02-10
Status: Accepted

## Context

Aura Video Studio needs real-time updates for video generation progress, job status changes, and long-running operations. Users need to see:

- Progress percentage and stage updates
- Log messages during video generation
- Job completion and error notifications
- Queue position updates

Key requirements:
- Real-time updates from server to client
- Support for long-running operations (several minutes)
- Reliable delivery of events
- Simple client implementation
- Works through corporate proxies and firewalls

Options for real-time communication:
- Server-Sent Events (SSE)
- WebSockets
- Long polling
- Short polling

## Decision

We will use **Server-Sent Events (SSE)** for server-to-client real-time updates.

Implementation details:
- SSE endpoints for job progress: `/api/v1/jobs/{jobId}/events`
- Event types: `progress`, `stage`, `log`, `complete`, `error`
- JSON payload for each event
- Automatic reconnection with exponential backoff
- Correlation IDs for event tracking
- 60-second keepalive to prevent timeout

## Consequences

### Positive Consequences

- **Simple protocol**: Built on HTTP, easy to understand and debug
- **Browser native**: EventSource API in all modern browsers
- **Automatic reconnection**: Browser handles reconnection automatically
- **Firewall friendly**: Works through proxies that block WebSockets
- **One-way communication**: Perfect fit for server → client updates
- **Text-based**: Easy to debug with curl or browser dev tools
- **Efficient**: HTTP/2 multiplexing for multiple SSE connections
- **No polling overhead**: Single persistent connection instead of repeated requests

### Negative Consequences

- **One-way only**: Cannot send messages from client to server (must use separate POST requests)
- **Text only**: Binary data requires base64 encoding
- **HTTP/1.1 connection limit**: Browsers limit concurrent connections per domain (6 connections)
- **No automatic compression**: Must enable gzip at server level
- **Limited IE support**: Not supported in Internet Explorer (not a concern for modern browsers)

## Alternatives Considered

### Alternative 1: WebSockets

**Description:** Full-duplex communication over a single TCP connection.

**Pros:**
- Bidirectional communication (client ↔ server)
- Binary data support
- Lower latency for high-frequency updates
- More efficient for two-way communication

**Cons:**
- More complex protocol and implementation
- Requires WebSocket server infrastructure
- More difficult to debug
- Often blocked by corporate proxies
- Manual reconnection handling required
- Overkill for one-way communication

**Why Rejected:** Aura doesn't need bidirectional communication. Commands from client use regular POST requests. SSE is simpler, more reliable through proxies, and perfectly suited for progress updates.

### Alternative 2: Long Polling

**Description:** Client polls server, server holds request open until data is available.

**Pros:**
- Works everywhere (basic HTTP)
- No special browser or server support needed
- Simple fallback mechanism

**Cons:**
- Inefficient (repeated connection overhead)
- Complex server implementation for connection management
- Higher latency
- More server resources (connection per client)
- Request timeout issues

**Why Rejected:** More complex and less efficient than SSE. SSE provides all the benefits of long polling with native browser support and better performance.

### Alternative 3: Short Polling

**Description:** Client periodically polls server for updates (e.g., every 2 seconds).

**Pros:**
- Extremely simple to implement
- Works everywhere
- No special infrastructure needed

**Cons:**
- Inefficient (many unnecessary requests)
- Higher latency (up to polling interval)
- Wasted bandwidth and server resources
- Poor user experience for progress updates
- Not true real-time

**Why Rejected:** Unacceptable latency for progress updates. Users expect smooth, real-time progress indicators, not stuttering updates every few seconds.

## References

- [Server-Sent Events Specification](https://html.spec.whatwg.org/multipage/server-sent-events.html)
- [EventSource API (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/EventSource)
- [Using Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events)
- [SSE vs WebSockets](https://ably.com/blog/websockets-vs-sse)

## Notes

SSE is an excellent fit for Aura's needs:

1. **Progress updates are unidirectional**: Server pushes updates to client; client sends commands via POST
2. **Firewall friendly**: Important for enterprise users who may have restrictive network policies
3. **Simple to debug**: Can test with curl: `curl -N http://localhost:5005/api/v1/jobs/123/events`
4. **HTTP/2 benefits**: With HTTP/2, connection limits are less of a concern

Example SSE event format:

```
event: progress
data: {"jobId":"abc123","progress":45,"stage":"Generating video"}

event: log
data: {"jobId":"abc123","level":"info","message":"Processing frame 100/200"}

event: complete
data: {"jobId":"abc123","status":"completed","outputPath":"output/video.mp4"}
```

The decision to use SSE keeps the architecture simple while providing excellent real-time updates for users. The automatic reconnection and text-based protocol make it reliable and easy to troubleshoot.
