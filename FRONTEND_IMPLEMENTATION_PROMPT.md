# Frontend Implementation Prompt - Session Management System

## CONTEXT

Bạn đang triển khai frontend cho một hệ thống học tập tương tác (Interactive Learning Platform) tương tự Kahoot/Quizizz với tích hợp bản đồ. Backend đã hoàn thành 100% với 25 REST API endpoints và SignalR real-time communication.

---

## YÊU CẦU CHỨC NĂNG

### 1. Vai Trò Người Dùng

#### A. Teacher (Giáo viên)
- Tạo Question Bank (bộ câu hỏi)
- Tạo Session từ Question Bank
- Điều khiển Session (Start/Pause/Resume/End)
- Điều khiển câu hỏi (Activate Next/Skip/Extend Time)
- Teacher Focus: Sync map view cho tất cả học sinh
- Xem real-time leaderboard
- Xem analytics (Word Cloud, Map Pins)

#### B. Student (Học sinh)
- Join session bằng 6-digit PIN code
- Có thể join như Guest (không cần login)
- Nhận câu hỏi real-time
- Trả lời câu hỏi (5 loại)
- Xem real-time leaderboard
- Xem kết quả cá nhân

---

## TECH STACK ĐỀ XUẤT

### Frontend Framework
- **React 18+** với TypeScript
- **React Router** v6 cho routing
- **TanStack Query (React Query)** cho API calls
- **Zustand** hoặc **Redux Toolkit** cho state management
- **SignalR Client** (@microsoft/signalr) cho real-time

### UI Libraries
- **Tailwind CSS** hoặc **Material-UI** cho styling
- **Recharts** hoặc **Chart.js** cho leaderboard/charts
- **React-Leaflet** hoặc **Mapbox GL JS** cho map integration
- **Framer Motion** cho animations
- **React Hook Form** + **Zod** cho form validation

### Additional Tools
- **Axios** cho HTTP requests
- **date-fns** cho date formatting
- **react-hot-toast** cho notifications
- **lucide-react** cho icons

---

## BACKEND API OVERVIEW

### Base URL
```
https://your-api-domain.com/api
```

### Authentication
- Bearer JWT token trong header `Authorization: Bearer {token}`
- Guest endpoints không cần token

---

## REST API ENDPOINTS (25 endpoints)

### Session Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /sessions | ✅ | Create new session |
| GET | /sessions/{id} | ✅ | Get session details |
| GET | /sessions/code/{code} | ❌ | Get session by PIN (for students) |
| GET | /sessions/my | ✅ | Get my sessions (teacher) |
| DELETE | /sessions/{id} | ✅ | Delete session |
| POST | /sessions/{id}/start | ✅ | Start session |
| POST | /sessions/{id}/pause | ✅ | Pause session |
| POST | /sessions/{id}/resume | ✅ | Resume session |
| POST | /sessions/{id}/end | ✅ | End session |
| POST | /sessions/join | ❌ | Join session with PIN |
| POST | /sessions/participants/{id}/leave | ❌ | Leave session |
| GET | /sessions/{id}/leaderboard | ❌ | Get leaderboard |
| POST | /sessions/{id}/activate-next | ✅ | Activate next question |
| POST | /sessions/{id}/skip-current | ✅ | Skip current question |
| POST | /sessions/questions/{id}/extend-time | ✅ | Extend time for question |
| POST | /sessions/participants/{id}/submit | ❌ | Submit answer |
| GET | /sessions/questions/{id}/word-cloud | ❌ | Get word cloud data |
| GET | /sessions/questions/{id}/map-pins | ❌ | Get map pins data |

### Question Bank Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /question-banks | ✅ | Create question bank |
| GET | /question-banks/{id} | ✅ | Get question bank |
| GET | /question-banks/my | ✅ | Get my question banks |
| GET | /question-banks/public | ❌ | Get public/template banks |
| POST | /question-banks/{id}/questions | ✅ | Create question |
| DELETE | /question-banks/questions/{id} | ✅ | Delete question |
| GET | /question-banks/{id}/questions | ✅ | Get all questions |

---

## API REQUEST/RESPONSE EXAMPLES

### 1. Create Session

**Request:**
```typescript
POST /api/sessions
{
  "mapId": "uuid",
  "questionBankId": "uuid",
  "sessionName": "Lớp 10A - Bài Kiểm Tra Địa Lý",
  "description": "Bài kiểm tra về các tỉnh thành Việt Nam",
  "sessionType": "LIVE",
  "maxParticipants": 50,
  "allowLateJoin": true,
  "showLeaderboard": true,
  "showCorrectAnswers": true,
  "shuffleQuestions": false,
  "shuffleOptions": true,
  "enableHints": false,
  "pointsForSpeed": true,
  "scheduledStartTime": null
}
```

**Response:**
```typescript
{
  "sessionId": "uuid",
  "sessionCode": "123456", // 6-digit PIN
  "sessionName": "Lớp 10A - Bài Kiểm Tra Địa Lý",
  "status": "DRAFT",
  "totalQuestions": 15,
  "createdAt": "2025-01-20T10:00:00Z"
}
```

### 2. Join Session (Student)

**Request:**
```typescript
POST /api/sessions/join
{
  "sessionCode": "123456",
  "displayName": "Nguyễn Văn A",
  "deviceInfo": "Chrome on Windows"
}
```

**Response:**
```typescript
{
  "sessionParticipantId": "uuid",
  "sessionId": "uuid",
  "sessionName": "Lớp 10A - Bài Kiểm Tra Địa Lý",
  "displayName": "Nguyễn Văn A",
  "message": "Joined session successfully",
  "joinedAt": "2025-01-20T10:05:00Z"
}
```

### 3. Submit Answer

**Request:**
```typescript
POST /api/sessions/participants/{participantId}/submit
{
  "sessionQuestionId": "uuid",
  "questionOptionId": "uuid", // For MCQ/True-False
  "responseText": "Hà Nội", // For Short Answer/Word Cloud
  "responseLatitude": 21.0285, // For Pin on Map
  "responseLongitude": 105.8542, // For Pin on Map
  "responseTimeSeconds": 12.5,
  "usedHint": false
}
```

**Response:**
```typescript
{
  "studentResponseId": "uuid",
  "isCorrect": true,
  "pointsEarned": 1200, // Base 1000 + speed bonus 200
  "totalScore": 5600,
  "currentRank": 3,
  "explanation": "Hà Nội là thủ đô của Việt Nam",
  "message": "Correct answer!",
  "submittedAt": "2025-01-20T10:10:15Z"
}
```

### 4. Get Leaderboard

**Response:**
```typescript
{
  "sessionId": "uuid",
  "leaderboard": [
    {
      "rank": 1,
      "sessionParticipantId": "uuid",
      "displayName": "Trần Thị B",
      "totalScore": 8500,
      "totalCorrect": 8,
      "totalAnswered": 10,
      "averageResponseTime": 8.5,
      "isCurrentUser": false
    },
    {
      "rank": 2,
      "sessionParticipantId": "uuid",
      "displayName": "Lê Văn C",
      "totalScore": 7800,
      "totalCorrect": 7,
      "totalAnswered": 10,
      "averageResponseTime": 10.2,
      "isCurrentUser": true
    }
  ],
  "updatedAt": "2025-01-20T10:15:00Z"
}
```

---

## SIGNALR REAL-TIME INTEGRATION

### Hub URL
```
wss://your-api-domain.com/api/hubs/session
```

### SignalR Setup (React + TypeScript)

```typescript
// hooks/useSignalR.ts
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export function useSessionHub(sessionId: string) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://your-api-domain.com/api/hubs/session', {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    newConnection.onreconnecting(() => {
      setIsConnected(false);
      console.log('Reconnecting...');
    });

    newConnection.onreconnected(() => {
      setIsConnected(true);
      console.log('Reconnected!');
      // Re-join session after reconnect
      newConnection.invoke('JoinSession', sessionId);
    });

    newConnection.onclose(() => {
      setIsConnected(false);
      console.log('Connection closed');
    });

    newConnection
      .start()
      .then(() => {
        setIsConnected(true);
        console.log('SignalR Connected!');
        return newConnection.invoke('JoinSession', sessionId);
      })
      .catch((err) => console.error('SignalR Connection Error:', err));

    setConnection(newConnection);

    return () => {
      newConnection.invoke('LeaveSession', sessionId);
      newConnection.stop();
    };
  }, [sessionId]);

  return { connection, isConnected };
}
```

### SignalR Events to Listen

```typescript
// components/StudentSession.tsx
const { connection } = useSessionHub(sessionId);

useEffect(() => {
  if (!connection) return;

  // 1. Question Activated
  connection.on('QuestionActivated', (data) => {
    console.log('New question:', data);
    setCurrentQuestion({
      sessionQuestionId: data.sessionQuestionId,
      questionText: data.questionText,
      questionType: data.questionType,
      points: data.points,
      timeLimit: data.timeLimit,
      questionNumber: data.questionNumber,
      totalQuestions: data.totalQuestions,
      options: data.options,
      activatedAt: new Date(data.activatedAt),
    });
    startTimer(data.timeLimit);
  });

  // 2. Response Submitted (someone answered)
  connection.on('ResponseSubmitted', (data) => {
    console.log(`${data.displayName} answered!`);
    setTotalResponses(data.totalResponses);
  });

  // 3. Leaderboard Update
  connection.on('LeaderboardUpdate', (data) => {
    console.log('Leaderboard updated:', data.topParticipants);
    setLeaderboard(data.topParticipants);
  });

  // 4. Session Status Changed
  connection.on('SessionStatusChanged', (data) => {
    console.log(`Session ${data.status}: ${data.message}`);
    setSessionStatus(data.status);

    if (data.status === 'COMPLETED') {
      showFinalResults();
    } else if (data.status === 'PAUSED') {
      pauseTimer();
    } else if (data.status === 'IN_PROGRESS') {
      resumeTimer();
    }
  });

  // 5. Participant Joined
  connection.on('ParticipantJoined', (data) => {
    console.log(`${data.displayName} joined!`);
    setTotalParticipants(data.totalParticipants);
    toast.success(`${data.displayName} joined the session`);
  });

  // 6. Participant Left
  connection.on('ParticipantLeft', (data) => {
    console.log(`${data.displayName} left`);
    setTotalParticipants(data.totalParticipants);
  });

  // 7. Time Extended
  connection.on('TimeExtended', (data) => {
    console.log(`Time extended by ${data.additionalSeconds}s`);
    extendTimer(data.additionalSeconds);
    toast.info(`Teacher added ${data.additionalSeconds} more seconds!`);
  });

  // 8. Map State Sync (Teacher Focus)
  connection.on('MapStateSync', (data) => {
    console.log('Teacher is focusing your map...');
    if (mapRef.current) {
      mapRef.current.flyTo({
        center: [data.longitude, data.latitude],
        zoom: data.zoomLevel,
        bearing: data.bearing || 0,
        pitch: data.pitch || 0,
        duration: 2000,
      });
    }
    toast.info('Teacher is focusing your map', {
      icon: '🗺️',
    });
  });

  // 9. Error
  connection.on('Error', (data) => {
    console.error('SignalR Error:', data.message);
    toast.error(data.message);
  });

  return () => {
    connection.off('QuestionActivated');
    connection.off('ResponseSubmitted');
    connection.off('LeaderboardUpdate');
    connection.off('SessionStatusChanged');
    connection.off('ParticipantJoined');
    connection.off('ParticipantLeft');
    connection.off('TimeExtended');
    connection.off('MapStateSync');
    connection.off('Error');
  };
}, [connection]);
```

---

## COMPONENT STRUCTURE ĐỀ XUẤT

```
src/
├── components/
│   ├── teacher/
│   │   ├── QuestionBankList.tsx
│   │   ├── QuestionBankEditor.tsx
│   │   ├── QuestionEditor.tsx
│   │   ├── SessionCreator.tsx
│   │   ├── SessionList.tsx
│   │   ├── TeacherDashboard.tsx
│   │   ├── SessionControl.tsx
│   │   ├── LiveLeaderboard.tsx
│   │   ├── AnalyticsDashboard.tsx
│   │   └── MapFocusButton.tsx
│   ├── student/
│   │   ├── SessionJoin.tsx
│   │   ├── StudentWaitingRoom.tsx
│   │   ├── QuestionDisplay.tsx
│   │   ├── AnswerOptions.tsx
│   │   ├── MapPinQuestion.tsx
│   │   ├── WordCloudQuestion.tsx
│   │   ├── StudentLeaderboard.tsx
│   │   └── ResultScreen.tsx
│   ├── shared/
│   │   ├── Timer.tsx
│   │   ├── ProgressBar.tsx
│   │   ├── PinInput.tsx
│   │   └── Confetti.tsx
│   └── analytics/
│       ├── WordCloudVisualization.tsx
│       └── MapPinsVisualization.tsx
├── hooks/
│   ├── useSignalR.ts
│   ├── useTimer.ts
│   ├── useLeaderboard.ts
│   └── useSessionState.ts
├── services/
│   ├── api.ts
│   ├── sessionService.ts
│   └── questionBankService.ts
├── types/
│   ├── session.types.ts
│   ├── question.types.ts
│   └── signalr.types.ts
└── pages/
    ├── TeacherPage.tsx
    ├── StudentPage.tsx
    └── SessionPage.tsx
```

---

## USER FLOWS

### Flow 1: Teacher Creates and Runs Session

```
1. Teacher logs in
   ↓
2. Create Question Bank
   - Click "New Question Bank"
   - Enter name, description
   - Select map (if applicable)
   ↓
3. Add Questions (5 types)
   - Multiple Choice: Add options, mark correct
   - True/False: Add options
   - Short Answer: Enter correct answer text
   - Word Cloud: Just question text (no correct answer)
   - Pin on Map: Set correct GPS coordinates + radius
   ↓
4. Create Session
   - Select Question Bank
   - Configure settings (shuffle, speed points, etc.)
   - Click "Create" → Get 6-digit PIN
   ↓
5. Share PIN with students
   - Show PIN on screen
   - Students join from their devices
   ↓
6. Start Session
   - Click "Start Session"
   - All students receive notification via SignalR
   ↓
7. Control Questions
   - Click "Activate Next Question"
   - Students see question + timer
   - Students submit answers
   - Real-time response counter updates
   ↓
8. Optional: Teacher Focus (for map questions)
   - Click "Focus Here" on map
   - All students' maps sync to teacher's view
   ↓
9. Optional: Extend Time
   - Click "Add 30s" button
   - All students get extra time
   ↓
10. View Live Results
    - Real-time leaderboard updates
    - Word cloud appears (for word cloud questions)
    - Map with all pins (for map questions)
    ↓
11. End Session
    - Click "End Session"
    - Show final results
    - Export results (optional)
```

### Flow 2: Student Joins and Participates

```
1. Student opens app
   ↓
2. Enter 6-digit PIN
   - Input: "123456"
   - Enter display name (can be guest)
   ↓
3. Join Session
   - Click "Join"
   - SignalR connects automatically
   ↓
4. Waiting Room
   - See session name
   - See participant count (updates real-time)
   - Wait for teacher to start
   ↓
5. Session Starts
   - Receive "SessionStatusChanged" event
   - Show "Get Ready!" animation
   ↓
6. Question Appears
   - Receive "QuestionActivated" event
   - Show question text
   - Show timer (countdown)
   - Show points
   ↓
7. Answer Question (depends on type)

   A. Multiple Choice / True-False:
      - Tap option
      - Confirm

   B. Short Answer:
      - Type answer
      - Submit

   C. Word Cloud:
      - Type words/phrases
      - Submit

   D. Pin on Map:
      - Tap map location
      - Confirm pin
   ↓
8. Submit Answer
   - POST /sessions/participants/{id}/submit
   - Receive feedback (correct/incorrect, points)
   ↓
9. View Results
   - See if correct
   - See points earned
   - See speed bonus (if applicable)
   - See updated leaderboard
   ↓
10. Repeat steps 6-9 until session ends
    ↓
11. Final Results
    - See final rank
    - See total score
    - See accuracy percentage
```

---

## KEY FEATURES TO IMPLEMENT

### 1. Timer Component

```typescript
// components/shared/Timer.tsx
import { useEffect, useState } from 'react';
import { CircularProgressbar, buildStyles } from 'react-circular-progressbar';
import 'react-circular-progressbar/dist/styles.css';

interface TimerProps {
  initialSeconds: number;
  onTimeUp: () => void;
  onExtend?: (additionalSeconds: number) => void;
}

export function Timer({ initialSeconds, onTimeUp, onExtend }: TimerProps) {
  const [seconds, setSeconds] = useState(initialSeconds);
  const [isPaused, setIsPaused] = useState(false);

  useEffect(() => {
    if (isPaused || seconds <= 0) return;

    const interval = setInterval(() => {
      setSeconds((prev) => {
        if (prev <= 1) {
          onTimeUp();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [seconds, isPaused, onTimeUp]);

  useEffect(() => {
    if (onExtend) {
      // Listen for TimeExtended SignalR event
      // When received, call: setSeconds((prev) => prev + additionalSeconds)
    }
  }, [onExtend]);

  const percentage = (seconds / initialSeconds) * 100;
  const color = percentage > 50 ? '#10b981' : percentage > 25 ? '#f59e0b' : '#ef4444';

  return (
    <div className="w-32 h-32">
      <CircularProgressbar
        value={percentage}
        text={`${seconds}s`}
        styles={buildStyles({
          textColor: color,
          pathColor: color,
          trailColor: '#e5e7eb',
        })}
      />
    </div>
  );
}
```

### 2. Pin Input for Session Code

```typescript
// components/shared/PinInput.tsx
import { useState, useRef } from 'react';

interface PinInputProps {
  length?: number;
  onComplete: (pin: string) => void;
}

export function PinInput({ length = 6, onComplete }: PinInputProps) {
  const [pin, setPin] = useState<string[]>(Array(length).fill(''));
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  const handleChange = (index: number, value: string) => {
    if (!/^\d*$/.test(value)) return;

    const newPin = [...pin];
    newPin[index] = value;
    setPin(newPin);

    // Auto-focus next input
    if (value && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }

    // Check if complete
    if (newPin.every((digit) => digit !== '') && newPin.length === length) {
      onComplete(newPin.join(''));
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === 'Backspace' && !pin[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  return (
    <div className="flex gap-2 justify-center">
      {pin.map((digit, index) => (
        <input
          key={index}
          ref={(el) => (inputRefs.current[index] = el)}
          type="text"
          maxLength={1}
          value={digit}
          onChange={(e) => handleChange(index, e.target.value)}
          onKeyDown={(e) => handleKeyDown(index, e)}
          className="w-12 h-16 text-center text-2xl font-bold border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:outline-none"
        />
      ))}
    </div>
  );
}
```

### 3. Map Pin Question Component

```typescript
// components/student/MapPinQuestion.tsx
import { useState } from 'react';
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import { LatLng } from 'leaflet';

interface MapPinQuestionProps {
  questionText: string;
  onSubmit: (lat: number, lng: number) => void;
  timeRemaining: number;
}

function MapClickHandler({ onLocationSelect }: { onLocationSelect: (latlng: LatLng) => void }) {
  useMapEvents({
    click: (e) => {
      onLocationSelect(e.latlng);
    },
  });
  return null;
}

export function MapPinQuestion({ questionText, onSubmit, timeRemaining }: MapPinQuestionProps) {
  const [selectedLocation, setSelectedLocation] = useState<LatLng | null>(null);

  const handleSubmit = () => {
    if (selectedLocation) {
      onSubmit(selectedLocation.lat, selectedLocation.lng);
    }
  };

  return (
    <div className="flex flex-col h-full">
      <div className="p-4 bg-white">
        <h2 className="text-xl font-bold mb-2">{questionText}</h2>
        <p className="text-gray-600">Tap on the map to place your pin</p>
      </div>

      <div className="flex-1 relative">
        <MapContainer
          center={[21.0285, 105.8542]} // Vietnam center
          zoom={6}
          className="h-full w-full"
        >
          <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
          <MapClickHandler onLocationSelect={setSelectedLocation} />
          {selectedLocation && <Marker position={selectedLocation} />}
        </MapContainer>
      </div>

      <div className="p-4 bg-white border-t">
        <button
          onClick={handleSubmit}
          disabled={!selectedLocation || timeRemaining <= 0}
          className="w-full py-3 bg-blue-600 text-white rounded-lg font-semibold disabled:bg-gray-300"
        >
          {selectedLocation ? 'Submit Answer' : 'Select a location first'}
        </button>
      </div>
    </div>
  );
}
```

### 4. Teacher Focus Button

```typescript
// components/teacher/MapFocusButton.tsx
import { useMap } from 'react-leaflet';
import * as signalR from '@microsoft/signalr';

interface MapFocusButtonProps {
  connection: signalR.HubConnection | null;
  sessionId: string;
}

export function MapFocusButton({ connection, sessionId }: MapFocusButtonProps) {
  const map = useMap();

  const handleFocus = async () => {
    if (!connection) return;

    const center = map.getCenter();
    const zoom = Math.round(map.getZoom());
    const bearing = (map as any).getBearing?.() || 0;
    const pitch = (map as any).getPitch?.() || 0;

    try {
      await connection.invoke('SyncMapState', sessionId, {
        latitude: center.lat,
        longitude: center.lng,
        zoomLevel: zoom,
        bearing,
        pitch,
      });

      toast.success('Map synced to all students!');
    } catch (error) {
      console.error('Failed to sync map:', error);
      toast.error('Failed to sync map');
    }
  };

  return (
    <button
      onClick={handleFocus}
      className="absolute top-4 right-4 z-[1000] bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg shadow-lg flex items-center gap-2"
    >
      <span>📍</span>
      <span>Focus Students Here</span>
    </button>
  );
}
```

### 5. Live Leaderboard Component

```typescript
// components/teacher/LiveLeaderboard.tsx
import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';

interface LeaderboardEntry {
  rank: number;
  sessionParticipantId: string;
  displayName: string;
  totalScore: number;
  totalCorrect: number;
  totalAnswered: number;
  averageResponseTime: number;
}

interface LiveLeaderboardProps {
  connection: signalR.HubConnection | null;
  sessionId: string;
}

export function LiveLeaderboard({ connection, sessionId }: LiveLeaderboardProps) {
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);

  useEffect(() => {
    // Fetch initial leaderboard
    fetch(`/api/sessions/${sessionId}/leaderboard`)
      .then((res) => res.json())
      .then((data) => setLeaderboard(data.leaderboard));

    // Listen for real-time updates
    if (connection) {
      connection.on('LeaderboardUpdate', (data) => {
        setLeaderboard(data.topParticipants);
      });

      return () => {
        connection.off('LeaderboardUpdate');
      };
    }
  }, [connection, sessionId]);

  return (
    <div className="bg-white rounded-lg shadow-lg p-6">
      <h2 className="text-2xl font-bold mb-4 flex items-center gap-2">
        <span>🏆</span>
        <span>Live Leaderboard</span>
      </h2>

      <AnimatePresence mode="popLayout">
        {leaderboard.map((entry, index) => (
          <motion.div
            key={entry.sessionParticipantId}
            layout
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            transition={{ duration: 0.3 }}
            className={`flex items-center gap-4 p-4 rounded-lg mb-2 ${
              index === 0
                ? 'bg-yellow-100 border-2 border-yellow-400'
                : index === 1
                ? 'bg-gray-100 border-2 border-gray-400'
                : index === 2
                ? 'bg-orange-100 border-2 border-orange-400'
                : 'bg-gray-50'
            }`}
          >
            <div className="text-2xl font-bold w-8">
              {index === 0 ? '🥇' : index === 1 ? '🥈' : index === 2 ? '🥉' : entry.rank}
            </div>

            <div className="flex-1">
              <div className="font-semibold">{entry.displayName}</div>
              <div className="text-sm text-gray-600">
                {entry.totalCorrect}/{entry.totalAnswered} correct • {entry.averageResponseTime.toFixed(1)}s avg
              </div>
            </div>

            <div className="text-2xl font-bold text-blue-600">
              {entry.totalScore.toLocaleString()}
            </div>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  );
}
```

---

## STYLING GUIDELINES

### Color Palette
```css
/* Primary Colors */
--primary-blue: #3b82f6;
--primary-green: #10b981;
--primary-red: #ef4444;
--primary-yellow: #f59e0b;

/* Question Types */
--mcq-color: #6366f1; /* Indigo */
--true-false-color: #8b5cf6; /* Purple */
--short-answer-color: #0ea5e9; /* Sky */
--word-cloud-color: #ec4899; /* Pink */
--pin-map-color: #10b981; /* Green */

/* Status Colors */
--draft: #9ca3af;
--waiting: #f59e0b;
--in-progress: #10b981;
--paused: #f59e0b;
--completed: #3b82f6;
```

### Animations
- **Question Appears**: Slide in from bottom with scale
- **Timer Warning**: Pulse when < 10s
- **Answer Feedback**: Confetti for correct, shake for wrong
- **Leaderboard**: Smooth transitions with layout animations
- **Loading**: Skeleton screens instead of spinners

---

## ERROR HANDLING

### API Errors
```typescript
// services/api.ts
import axios from 'axios';

export const api = axios.create({
  baseURL: 'https://your-api-domain.com/api',
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      // Server responded with error
      const { status, data } = error.response;

      if (status === 401) {
        // Unauthorized - redirect to login
        window.location.href = '/login';
      } else if (status === 403) {
        toast.error('You do not have permission for this action');
      } else if (status === 404) {
        toast.error(data.message || 'Resource not found');
      } else if (status === 409) {
        toast.error(data.message || 'Conflict occurred');
      } else {
        toast.error(data.message || 'An error occurred');
      }
    } else if (error.request) {
      // No response from server
      toast.error('Cannot connect to server. Please check your internet connection.');
    } else {
      toast.error('An unexpected error occurred');
    }

    return Promise.reject(error);
  }
);
```

### SignalR Errors
```typescript
connection.on('Error', (data) => {
  toast.error(data.message, {
    duration: 5000,
    icon: '❌',
  });
});

connection.onclose((error) => {
  if (error) {
    console.error('SignalR connection closed with error:', error);
    toast.error('Lost connection to server. Attempting to reconnect...');
  }
});

connection.onreconnecting((error) => {
  console.warn('SignalR reconnecting:', error);
  toast.info('Connection lost. Reconnecting...', {
    duration: 3000,
    icon: '🔄',
  });
});

connection.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId);
  toast.success('Reconnected successfully!');

  // Re-join session after reconnect
  connection.invoke('JoinSession', sessionId);
});
```

---

## PERFORMANCE OPTIMIZATION

### 1. Code Splitting
```typescript
// Lazy load heavy components
const TeacherDashboard = lazy(() => import('./components/teacher/TeacherDashboard'));
const StudentSession = lazy(() => import('./components/student/StudentSession'));
const MapPinQuestion = lazy(() => import('./components/student/MapPinQuestion'));

// Usage
<Suspense fallback={<LoadingSpinner />}>
  <TeacherDashboard />
</Suspense>
```

### 2. React Query Caching
```typescript
// hooks/useSession.ts
import { useQuery } from '@tanstack/react-query';

export function useSession(sessionId: string) {
  return useQuery({
    queryKey: ['session', sessionId],
    queryFn: () => fetch(`/api/sessions/${sessionId}`).then(res => res.json()),
    staleTime: 30000, // 30 seconds
    refetchOnWindowFocus: false,
  });
}
```

### 3. Debounce Word Cloud Input
```typescript
import { useDebouncedCallback } from 'use-debounce';

const handleWordInput = useDebouncedCallback((text: string) => {
  // Update preview
  setPreview(text);
}, 500);
```

---

## TESTING GUIDELINES

### Unit Tests
```typescript
// __tests__/Timer.test.tsx
import { render, screen, act } from '@testing-library/react';
import { Timer } from '@/components/shared/Timer';

describe('Timer Component', () => {
  jest.useFakeTimers();

  it('counts down from initial seconds', () => {
    const onTimeUp = jest.fn();
    render(<Timer initialSeconds={10} onTimeUp={onTimeUp} />);

    expect(screen.getByText('10s')).toBeInTheDocument();

    act(() => {
      jest.advanceTimersByTime(1000);
    });

    expect(screen.getByText('9s')).toBeInTheDocument();
  });

  it('calls onTimeUp when timer reaches 0', () => {
    const onTimeUp = jest.fn();
    render(<Timer initialSeconds={3} onTimeUp={onTimeUp} />);

    act(() => {
      jest.advanceTimersByTime(3000);
    });

    expect(onTimeUp).toHaveBeenCalled();
  });
});
```

### Integration Tests
```typescript
// __tests__/SessionFlow.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SessionJoin } from '@/components/student/SessionJoin';

describe('Session Join Flow', () => {
  it('allows student to join with valid PIN', async () => {
    const user = userEvent.setup();
    render(<SessionJoin />);

    // Enter PIN
    const pinInputs = screen.getAllByRole('textbox');
    await user.type(pinInputs[0], '1');
    await user.type(pinInputs[1], '2');
    await user.type(pinInputs[2], '3');
    await user.type(pinInputs[3], '4');
    await user.type(pinInputs[4], '5');
    await user.type(pinInputs[5], '6');

    // Enter name
    const nameInput = screen.getByPlaceholderText('Your name');
    await user.type(nameInput, 'Test Student');

    // Click join
    const joinButton = screen.getByText('Join Session');
    await user.click(joinButton);

    // Verify redirect to waiting room
    await waitFor(() => {
      expect(screen.getByText(/waiting for teacher/i)).toBeInTheDocument();
    });
  });
});
```

---

## DEPLOYMENT CHECKLIST

### Environment Variables
```env
VITE_API_BASE_URL=https://your-api-domain.com/api
VITE_SIGNALR_HUB_URL=wss://your-api-domain.com/api/hubs/session
VITE_MAP_TILE_URL=https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png
```

### Build Optimization
```json
// vite.config.ts
export default defineConfig({
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'signalr': ['@microsoft/signalr'],
          'map': ['react-leaflet', 'leaflet'],
          'charts': ['recharts'],
        },
      },
    },
  },
});
```

---

## DELIVERABLES

### Phải có (Must Have)
1. ✅ Teacher Dashboard: Create/manage sessions
2. ✅ Student Join: PIN input + guest support
3. ✅ Question Display: All 5 types supported
4. ✅ Timer: Countdown with extend support
5. ✅ Answer Submission: All question types
6. ✅ Live Leaderboard: Real-time updates
7. ✅ SignalR Integration: All 10 events
8. ✅ Map Integration: Pin questions + Teacher Focus
9. ✅ Responsive Design: Mobile + Desktop

### Nên có (Should Have)
1. ⚠️ Word Cloud Visualization: Live word cloud
2. ⚠️ Map Pins Visualization: Show all student pins
3. ⚠️ Session Analytics: Charts and stats
4. ⚠️ Question Bank Templates: Pre-made questions
5. ⚠️ Export Results: CSV/PDF export

### Có thể có (Nice to Have)
1. ❌ Session Recording: Replay sessions
2. ❌ Team Mode: Group competitions
3. ❌ Multimedia Questions: Images/videos
4. ❌ AI Hints: Smart hints for students
5. ❌ Push Notifications: PWA notifications

---

## EXAMPLE PAGES MOCKUP

### Teacher Dashboard
```
┌─────────────────────────────────────────────────┐
│  📚 My Question Banks                    [+ New] │
├─────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐              │
│  │ Geography   │  │ History     │              │
│  │ 15 questions│  │ 10 questions│              │
│  └─────────────┘  └─────────────┘              │
│                                                  │
│  🎯 My Sessions                          [+ New] │
├─────────────────────────────────────────────────┤
│  [LIVE] Lớp 10A - Geography Test                │
│  PIN: 123456 | 25 students | Question 5/15      │
│  [View] [End]                                    │
│                                                  │
│  [COMPLETED] Lớp 10B - History Quiz             │
│  Finished 2 hours ago | 30 students              │
│  [Results]                                       │
└─────────────────────────────────────────────────┘
```

### Student Join Screen
```
┌─────────────────────────────────────────────────┐
│                                                  │
│              🎓 Join Session                     │
│                                                  │
│         Enter your session code:                 │
│                                                  │
│         ┌───┬───┬───┬───┬───┬───┐              │
│         │ 1 │ 2 │ 3 │ 4 │ 5 │ 6 │              │
│         └───┴───┴───┴───┴───┴───┘              │
│                                                  │
│         Your name:                               │
│         ┌─────────────────────────┐             │
│         │ Nguyễn Văn A            │             │
│         └─────────────────────────┘             │
│                                                  │
│         [        Join Session       ]            │
│                                                  │
└─────────────────────────────────────────────────┘
```

### Question Screen (MCQ)
```
┌─────────────────────────────────────────────────┐
│  Question 5/15                    ⏱️ 20s        │
│  🎯 1000 points                                  │
├─────────────────────────────────────────────────┤
│                                                  │
│  What is the capital of Vietnam?                │
│                                                  │
│  ┌───────────────────────────────────────────┐  │
│  │  A. Ho Chi Minh City                      │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │  B. Hanoi                                 │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │  C. Da Nang                               │  │
│  └───────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────┐  │
│  │  D. Can Tho                               │  │
│  └───────────────────────────────────────────┘  │
│                                                  │
│  [         Submit Answer          ]              │
│                                                  │
│  👥 12/25 students answered                      │
└─────────────────────────────────────────────────┘
```

### Leaderboard Screen
```
┌─────────────────────────────────────────────────┐
│           🏆 Leaderboard                         │
├─────────────────────────────────────────────────┤
│  🥇 1. Trần Thị B          8500 pts             │
│     8/10 correct • 8.5s avg                      │
│                                                  │
│  🥈 2. Lê Văn C            7800 pts   ← YOU     │
│     7/10 correct • 10.2s avg                     │
│                                                  │
│  🥉 3. Phạm Văn D          7200 pts             │
│     7/10 correct • 12.1s avg                     │
│                                                  │
│  4. Nguyễn Thị E          6900 pts              │
│  5. Hoàng Văn F           6500 pts              │
│  ...                                             │
└─────────────────────────────────────────────────┘
```

---

## SUPPORT & RESOURCES

### Documentation
- Backend API Documentation: `/SIGNALR_IMPLEMENTATION.md`
- Database Schema: `/SESSION_MANAGEMENT_DATABASE_DESIGN.md`
- Feature Coverage: `/FEATURE_COVERAGE_CHECK.md`

### Libraries Documentation
- React: https://react.dev/
- SignalR Client: https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
- TanStack Query: https://tanstack.com/query/latest
- React Leaflet: https://react-leaflet.js.org/
- Tailwind CSS: https://tailwindcss.com/docs

---

## SUCCESS CRITERIA

### Functionality
- ✅ Teacher can create and run sessions
- ✅ Students can join with PIN (guest or logged in)
- ✅ All 5 question types work correctly
- ✅ Real-time updates work (SignalR)
- ✅ Teacher Focus syncs maps
- ✅ Leaderboard updates in real-time
- ✅ Timer works with extend functionality
- ✅ Results display correctly

### Performance
- ✅ Initial load < 3 seconds
- ✅ Question transition < 500ms
- ✅ SignalR events < 100ms latency
- ✅ Smooth animations (60fps)

### UX
- ✅ Intuitive navigation
- ✅ Clear visual feedback
- ✅ Mobile responsive
- ✅ Accessible (WCAG 2.1 AA)

---

## ESTIMATED TIMELINE

| Phase | Tasks | Duration |
|-------|-------|----------|
| Setup | Project setup, dependencies | 1 day |
| Core Components | Timer, PinInput, Question types | 2 days |
| Teacher Features | Dashboard, Session control | 2 days |
| Student Features | Join, Answer, Leaderboard | 2 days |
| SignalR | Real-time integration | 1 day |
| Map Integration | Leaflet, Pin questions, Focus | 1 day |
| Analytics | Word cloud, Map pins viz | 1 day |
| Testing | Unit + Integration tests | 1 day |
| Polish | Animations, Error handling | 1 day |
| **TOTAL** | | **12 days** |

---

## FINAL NOTES

1. **Prioritize Core Flow**: Focus on Teacher Create → Student Join → Answer → Results first
2. **Test with Real Users**: Get feedback early from teachers and students
3. **Mobile First**: Many students will join on phones
4. **Handle Offline**: Show clear error when internet drops
5. **Performance**: SignalR events must be instant for good UX

**Good luck with the implementation! 🚀**

If you need clarification on any API endpoint, SignalR event, or component implementation, refer to the backend documentation files or contact the backend team.
