# SignalR Real-Time Implementation for Session Management

## Overview

This document describes the SignalR implementation for real-time updates in the interactive learning session system. SignalR enables bidirectional communication between the server and clients, allowing instant notifications for session events, participant actions, and leaderboard updates.

## Hub Endpoint

**SessionHub URL**: `/api/hubs/session`

**Full URL**: `https://your-domain.com/api/hubs/session`

**Authentication**: Not required (allows guest participants)

**CORS**: Enabled for frontend origins

---

## Architecture

### Hub Location
- **File**: `FA25_CusomMapOSM_BE/CusomMapOSM_Infrastructure/Hubs/SessionHub.cs`
- **Registration**: `FA25_CusomMapOSM_BE/CusomMapOSM_API/Program.cs` (line 145)

### Service Integration
- **File**: `FA25_CusomMapOSM_BE/CusomMapOSM_Infrastructure/Features/Sessions/SessionService.cs`
- **Injection**: `IHubContext<SessionHub>` injected into SessionService
- **Broadcasting**: Automatic broadcasting on key events

### Event DTOs
- **Location**: `FA25_CusomMapOSM_BE/CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/`
- **8 Event Types**: QuestionActivated, ResponseSubmitted, LeaderboardUpdate, SessionStatusChanged, ParticipantJoined, ParticipantLeft, TimeExtended, MapStateSync

---

## Client-to-Server Methods

These methods are called by clients to interact with the hub:

### 1. `JoinSession`
Join a session room to receive real-time updates.

**Parameters:**
- `sessionId` (Guid): The session ID to join

**Response Events:**
- `JoinedSession`: Confirmation with session details
- `Error`: If session not found

**Example:**
```javascript
await connection.invoke("JoinSession", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
```

---

### 2. `LeaveSession`
Leave a session room.

**Parameters:**
- `sessionId` (Guid): The session ID to leave

**Response Events:**
- `LeftSession`: Confirmation message
- `Error`: If operation fails

**Example:**
```javascript
await connection.invoke("LeaveSession", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
```

---

### 3. `SyncMapState`
**[Teacher Only]** Synchronize map view to all participants (Teacher Focus feature).

**Parameters:**
- `sessionId` (Guid): The session ID
- `request` (MapStateSyncRequest):
  - `latitude` (decimal): Map center latitude
  - `longitude` (decimal): Map center longitude
  - `zoomLevel` (int): Map zoom level
  - `bearing` (decimal, optional): Map bearing/rotation
  - `pitch` (decimal, optional): Map pitch/tilt

**Response Events:**
- `MapStateSync`: Broadcast to all other participants
- `Error`: If not authorized (only host can sync)

**Example:**
```javascript
await connection.invoke("SyncMapState", "3fa85f64-5717-4562-b3fc-2c963f66afa6", {
  latitude: 21.0285,
  longitude: 105.8542,
  zoomLevel: 15,
  bearing: 0,
  pitch: 0
});
```

---

## Server-to-Client Events

These events are broadcast by the server to notify clients:

### 1. `JoinedSession`
Sent to caller when they successfully join a session.

**Event Data:**
```typescript
{
  sessionId: string,
  sessionCode: string,
  status: string,
  message: string
}
```

---

### 2. `QuestionActivated`
Broadcast when teacher activates a new question.

**Event Data:**
```typescript
{
  sessionQuestionId: string,
  questionId: string,
  questionText: string,
  questionType: string, // "MULTIPLE_CHOICE" | "TRUE_FALSE" | "SHORT_ANSWER" | "WORD_CLOUD" | "PIN_ON_MAP"
  points: number,
  timeLimit: number, // seconds
  questionNumber: number,
  totalQuestions: number,
  options: [
    {
      questionOptionId: string,
      optionText: string
    }
  ],
  activatedAt: string // ISO 8601 datetime
}
```

**Triggered By:** `SessionService.ActivateNextQuestion()`

---

### 3. `ResponseSubmitted`
Broadcast when a participant submits an answer.

**Event Data:**
```typescript
{
  sessionQuestionId: string,
  participantId: string,
  displayName: string,
  isCorrect: boolean,
  pointsEarned: number,
  responseTimeSeconds: number,
  totalResponses: number, // How many have answered
  submittedAt: string
}
```

**Triggered By:** `SessionService.SubmitResponse()`

---

### 4. `LeaderboardUpdate`
Broadcast updated leaderboard rankings.

**Event Data:**
```typescript
{
  sessionId: string,
  topParticipants: [
    {
      sessionParticipantId: string,
      displayName: string,
      totalScore: number,
      totalCorrect: number,
      totalAnswered: number,
      averageResponseTime: number,
      rank: number
    }
  ],
  updatedAt: string
}
```

**Triggered By:** `SessionService.SubmitResponse()` (after each answer)

---

### 5. `SessionStatusChanged`
Broadcast when session status changes (start, pause, resume, end).

**Event Data:**
```typescript
{
  sessionId: string,
  status: string, // "IN_PROGRESS" | "PAUSED" | "COMPLETED"
  message: string,
  changedAt: string
}
```

**Triggered By:**
- `SessionService.StartSession()`
- `SessionService.PauseSession()`
- `SessionService.ResumeSession()`
- `SessionService.EndSession()`

---

### 6. `ParticipantJoined`
Broadcast when a new participant joins the session.

**Event Data:**
```typescript
{
  sessionParticipantId: string,
  displayName: string,
  isGuest: boolean,
  totalParticipants: number, // Updated count
  joinedAt: string
}
```

**Triggered By:** `SessionService.JoinSession()`

---

### 7. `ParticipantLeft`
Broadcast when a participant leaves the session.

**Event Data:**
```typescript
{
  sessionParticipantId: string,
  displayName: string,
  totalParticipants: number, // Updated count
  leftAt: string
}
```

**Triggered By:** `SessionService.LeaveSession()`

---

### 8. `TimeExtended`
Broadcast when teacher extends time for current question.

**Event Data:**
```typescript
{
  sessionQuestionId: string,
  additionalSeconds: number,
  newTimeLimit: number, // Total time limit after extension
  extendedAt: string
}
```

**Triggered By:** `SessionService.ExtendTime()`

---

### 9. `MapStateSync`
Broadcast when teacher syncs map view (Teacher Focus).

**Event Data:**
```typescript
{
  sessionId: string,
  latitude: number,
  longitude: number,
  zoomLevel: number,
  bearing?: number,
  pitch?: number,
  syncedBy: string, // Teacher email/name
  syncedAt: string
}
```

**Triggered By:** `SessionHub.SyncMapState()` (called by teacher)

---

### 10. `Error`
Sent to caller when an error occurs.

**Event Data:**
```typescript
{
  message: string
}
```

---

## Usage Examples

### JavaScript/TypeScript Client Implementation

#### 1. Setup Connection

```typescript
import * as signalR from "@microsoft/signalr";

// Create connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://your-api.com/api/hubs/session", {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Start connection
await connection.start();
console.log("SignalR Connected!");
```

---

#### 2. Join Session and Listen for Events

```typescript
const sessionId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

// Register event handlers BEFORE joining
connection.on("JoinedSession", (data) => {
  console.log("Joined session:", data.sessionCode);
  console.log("Status:", data.status);
});

connection.on("QuestionActivated", (data) => {
  console.log("New question:", data.questionText);
  console.log(`Question ${data.questionNumber} of ${data.totalQuestions}`);
  console.log(`Time limit: ${data.timeLimit} seconds`);
  console.log(`Points: ${data.points}`);

  // Display question UI
  displayQuestion(data);
  startTimer(data.timeLimit);
});

connection.on("ResponseSubmitted", (data) => {
  console.log(`${data.displayName} answered ${data.isCorrect ? "correctly" : "incorrectly"}`);
  console.log(`Points earned: ${data.pointsEarned}`);
  console.log(`Total responses: ${data.totalResponses}`);

  // Update response counter UI
  updateResponseCounter(data.totalResponses);
});

connection.on("LeaderboardUpdate", (data) => {
  console.log("Updated leaderboard:");
  data.topParticipants.forEach(p => {
    console.log(`${p.rank}. ${p.displayName} - ${p.totalScore} points`);
  });

  // Update leaderboard UI
  updateLeaderboard(data.topParticipants);
});

connection.on("SessionStatusChanged", (data) => {
  console.log(`Session ${data.status}: ${data.message}`);

  if (data.status === "COMPLETED") {
    showFinalResults();
  } else if (data.status === "PAUSED") {
    pauseUI();
  }
});

connection.on("ParticipantJoined", (data) => {
  console.log(`${data.displayName} joined! Total: ${data.totalParticipants}`);
  updateParticipantCount(data.totalParticipants);
});

connection.on("ParticipantLeft", (data) => {
  console.log(`${data.displayName} left. Total: ${data.totalParticipants}`);
  updateParticipantCount(data.totalParticipants);
});

connection.on("TimeExtended", (data) => {
  console.log(`Time extended by ${data.additionalSeconds} seconds`);
  console.log(`New time limit: ${data.newTimeLimit} seconds`);

  // Update timer UI
  extendTimer(data.additionalSeconds);
});

connection.on("MapStateSync", (data) => {
  console.log("Teacher is focusing your map...");

  // Sync map view
  map.flyTo({
    center: [data.longitude, data.latitude],
    zoom: data.zoomLevel,
    bearing: data.bearing || 0,
    pitch: data.pitch || 0,
    duration: 2000
  });
});

connection.on("Error", (data) => {
  console.error("SignalR Error:", data.message);
  alert(data.message);
});

// Join the session
await connection.invoke("JoinSession", sessionId);
```

---

#### 3. Teacher-Specific: Sync Map State

```typescript
// Teacher controls map synchronization
async function syncMapToStudents() {
  const sessionId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

  const currentMapState = {
    latitude: map.getCenter().lat,
    longitude: map.getCenter().lng,
    zoomLevel: Math.round(map.getZoom()),
    bearing: map.getBearing(),
    pitch: map.getPitch()
  };

  try {
    await connection.invoke("SyncMapState", sessionId, currentMapState);
    console.log("Map synced to all students!");
  } catch (error) {
    console.error("Failed to sync map:", error);
  }
}

// Add button click handler
document.getElementById("syncMapButton").addEventListener("click", syncMapToStudents);
```

---

#### 4. Leave Session on Cleanup

```typescript
// Clean up when leaving page
window.addEventListener("beforeunload", async () => {
  await connection.invoke("LeaveSession", sessionId);
  await connection.stop();
});

// Or explicit leave
async function leaveSession() {
  try {
    await connection.invoke("LeaveSession", sessionId);
    await connection.stop();
    console.log("Left session successfully");
  } catch (error) {
    console.error("Error leaving session:", error);
  }
}
```

---

### React Hook Example

```typescript
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

export function useSessionHub(sessionId: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [currentQuestion, setCurrentQuestion] = useState(null);
  const [leaderboard, setLeaderboard] = useState([]);
  const [participantCount, setParticipantCount] = useState(0);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("https://your-api.com/api/hubs/session")
      .withAutomaticReconnect()
      .build();

    // Register handlers
    newConnection.on("QuestionActivated", setCurrentQuestion);
    newConnection.on("LeaderboardUpdate", (data) => setLeaderboard(data.topParticipants));
    newConnection.on("ParticipantJoined", (data) => setParticipantCount(data.totalParticipants));
    newConnection.on("ParticipantLeft", (data) => setParticipantCount(data.totalParticipants));

    // Start and join
    newConnection.start()
      .then(() => newConnection.invoke("JoinSession", sessionId))
      .then(() => console.log("Connected to session"))
      .catch(console.error);

    setConnection(newConnection);

    // Cleanup
    return () => {
      newConnection.invoke("LeaveSession", sessionId);
      newConnection.stop();
    };
  }, [sessionId]);

  return { connection, currentQuestion, leaderboard, participantCount };
}
```

---

## Testing SignalR Connection

### Using Browser Console

```javascript
// 1. Install SignalR client library (add to HTML)
// <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>

// 2. Create connection
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:5001/api/hubs/session")
  .build();

// 3. Register handlers
connection.on("QuestionActivated", (data) => console.log("Question:", data));
connection.on("LeaderboardUpdate", (data) => console.log("Leaderboard:", data));

// 4. Connect
await connection.start();
console.log("Connected!");

// 5. Join session
await connection.invoke("JoinSession", "your-session-id-here");

// 6. Test (leave for cleanup)
await connection.invoke("LeaveSession", "your-session-id-here");
```

---

## Event Flow Diagrams

### Teacher Starts Session and Activates Question

```
Teacher Client              Server (SessionService)              All Clients
     |                              |                                  |
     |-- POST /api/sessions/start ->|                                  |
     |                              |-- StartSession() -------->       |
     |<---- 200 OK ----------------|                                  |
     |                              |--Broadcast: SessionStatusChanged->|
     |                              |                                  |-- UI: "Session Started"
     |                              |                                  |
     |-- POST /activate-next ------>|                                  |
     |                              |-- ActivateNextQuestion()-->      |
     |<---- 200 OK ----------------|                                  |
     |                              |--Broadcast: QuestionActivated--->|
     |                              |                                  |-- UI: Show Question
     |                              |                                  |-- Start Timer
```

### Student Submits Answer

```
Student Client             Server (SessionService)              All Clients
     |                              |                                  |
     |-- POST /submit-response ---->|                                  |
     |                              |-- SubmitResponse() ------>       |
     |                              |-- Calculate score               |
     |                              |-- Update stats                  |
     |                              |-- Update leaderboard            |
     |<---- 200 OK ----------------|                                  |
     |                              |--Broadcast: ResponseSubmitted--->|
     |                              |                                  |-- UI: "+1 Response"
     |                              |--Broadcast: LeaderboardUpdate--->|
     |                              |                                  |-- UI: Update Rankings
```

### Teacher Focus (Map Sync)

```
Teacher Client             SessionHub                           Student Clients
     |                              |                                  |
     |-- Invoke: SyncMapState ----->|                                  |
     |                              |-- Validate: IsHost? ----->       |
     |<---- Acknowledged -----------|                                  |
     |                              |--Broadcast: MapStateSync-------->|
     |                              |                                  |-- Map.flyTo(coords)
     |                              |                                  |-- UI: "Teacher is focusing..."
```

---

## Best Practices

### 1. **Connection Management**
- Always use `withAutomaticReconnect()` to handle temporary disconnections
- Implement exponential backoff for reconnection attempts
- Store connection state in React context or Vuex/Redux store

### 2. **Event Handlers**
- Register ALL event handlers BEFORE calling `connection.start()`
- Use TypeScript interfaces for strong typing on events
- Implement debouncing for frequent events (e.g., MapStateSync)

### 3. **Error Handling**
```typescript
connection.onclose((error) => {
  console.error("Connection closed:", error);
  showNotification("Connection lost. Attempting to reconnect...");
});

connection.onreconnecting((error) => {
  console.warn("Reconnecting:", error);
  showNotification("Reconnecting...");
});

connection.onreconnected((connectionId) => {
  console.log("Reconnected:", connectionId);
  // Re-join session
  connection.invoke("JoinSession", sessionId);
});
```

### 4. **Memory Leaks**
- Always call `connection.stop()` when unmounting components
- Remove event listeners before re-registering
- Use `connection.off("EventName", handler)` if needed

### 5. **Teacher Authorization**
- Check `isHost` flag on client before showing teacher controls
- Server validates all teacher-only methods (SyncMapState, ActivateNextQuestion, etc.)
- Show error toast if unauthorized action attempted

---

## Troubleshooting

### Connection Fails

**Issue:** `Error: Failed to start the connection: Error: WebSocket failed to connect.`

**Solutions:**
- Check CORS configuration in `Program.cs`
- Verify SignalR is registered: `builder.Services.AddSignalR()`
- Ensure WebSocket support is enabled on server
- Check firewall/proxy settings

### Events Not Received

**Issue:** Not receiving broadcasts from server

**Solutions:**
- Verify you called `JoinSession(sessionId)` before expecting events
- Check that event handler is registered before connection starts
- Verify session ID is correct
- Check server logs for errors during broadcast

### "Session not found" Error

**Issue:** `Error: Session not found`

**Solutions:**
- Verify session exists in database
- Check session ID format (must be valid Guid)
- Ensure session status allows joining (WAITING or IN_PROGRESS)
- Check if session has reached max participants

---

## Performance Considerations

### Scalability
- **Group Size**: Each session is a SignalR group. Supports 100+ participants per session.
- **Broadcast Frequency**: Leaderboard updates on every response. Consider throttling for 500+ participants.
- **Message Size**: Event payloads average 1-5 KB. Total bandwidth scales linearly with participant count.

### Optimization Tips
1. **Leaderboard**: Only broadcast top 10 instead of all participants
2. **Response Events**: Batch multiple responses if submitted within 100ms
3. **Map Sync**: Debounce teacher map movements (max 2 syncs per second)
4. **Redis Backplane**: Use Redis for multi-server deployments

---

## Security Considerations

### Authentication
- Session hub allows anonymous connections (for guest participants)
- Teacher-only methods validate `isHost` on server side
- Use JWT authentication for logged-in users

### Authorization Checks
```csharp
// Server-side validation (already implemented)
var isHost = await _sessionRepository.CheckUserIsHost(sessionId, userId);
if (!isHost) {
    await Clients.Caller.SendAsync("Error", new { Message = "Only host can sync map" });
    return;
}
```

### Rate Limiting
- Consider implementing rate limits on `SyncMapState` (max 10/minute)
- Throttle `JoinSession` to prevent spam (max 5/minute per IP)

---

## Files Created/Modified

### New Files
1. `CusomMapOSM_Infrastructure/Hubs/SessionHub.cs` - Main hub implementation
2. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/QuestionActivatedEvent.cs`
3. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/ResponseSubmittedEvent.cs`
4. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/LeaderboardUpdateEvent.cs`
5. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/SessionStatusChangedEvent.cs`
6. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/ParticipantJoinedEvent.cs`
7. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/ParticipantLeftEvent.cs`
8. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/TimeExtendedEvent.cs`
9. `CusomMapOSM_Application/Models/DTOs/Features/Sessions/Events/MapStateSyncEvent.cs`

### Modified Files
1. `CusomMapOSM_Infrastructure/Features/Sessions/SessionService.cs` - Added Hub broadcasting
2. `CusomMapOSM_API/Program.cs` - Registered SessionHub

---

## Summary

The SignalR implementation provides:
- ✅ **Real-time question delivery** (Teacher → All Students)
- ✅ **Live response tracking** (Students → Teacher dashboard)
- ✅ **Auto-updating leaderboard** (Server → All)
- ✅ **Session status sync** (Start, Pause, Resume, End)
- ✅ **Participant join/leave notifications**
- ✅ **Teacher Focus map synchronization** (Map PIN questions)
- ✅ **Time extension broadcasts**

**Total Events**: 10 (9 broadcast events + 1 error event)

**Total Hub Methods**: 3 (JoinSession, LeaveSession, SyncMapState)

**Supported Scenarios**: Kahoot-like interactive learning with real-time collaboration and map integration.

---

## Next Steps

1. **Frontend Implementation**: Integrate SignalR client in React/Vue/Angular app
2. **Testing**: Write integration tests for hub methods and broadcasts
3. **Monitoring**: Add Application Insights for SignalR metrics
4. **Redis Backplane**: Configure for multi-server scaling
5. **Mobile Apps**: Implement SignalR in iOS/Android using native libraries

---

**Last Updated**: 2025-01-20
**Version**: 1.0
**Author**: Claude AI Assistant
