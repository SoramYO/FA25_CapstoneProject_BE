# Session Management Database Design

## Overview
Thiết kế database cho hệ thống tương tác real-time giữa giáo viên và học sinh trên bản đồ, tương tự Kahoot/Quizizz nhưng tích hợp với Maps.

## Core Concepts

### 1. Map vs Session
- **Map**: Bản đồ gốc, có thể tái sử dụng cho nhiều lớp
- **Session**: Phiên học cụ thể cho một lớp/khóa học (VD: Lớp10A-2024, Lớp10B-2024)
- Một Map có thể có nhiều Session
- Mỗi Session có dữ liệu tương tác riêng biệt

### 2. Workflow
```
Teacher creates QuestionBank → Assign to Map
Teacher starts Session (generates PIN) → Students join via PIN
Teacher controls Session → Presents questions in queue
Students respond in real-time → System tracks all responses
Teacher views analytics → Leaderboard, Charts, Statistics
```

---

## Entity Relationship Diagram

```
User ─┬─── QuestionBank ──── Question ──── QuestionOption
      │
      ├─── Map ──┬─── Session ──┬─── SessionQuestion ──── StudentResponse
      │          │               │
      │          │               ├─── SessionParticipant
      │          │               │
      │          │               └─── SessionMapState
      │          │
      │          └─── Location (existing)
      │
      └─── Organization (existing)
```

---

## New Entities

### 1. QuestionBank (Kho câu hỏi)
**Purpose**: Tổ chức và quản lý tập hợp câu hỏi theo chủ đề

| Field | Type | Description |
|-------|------|-------------|
| QuestionBankId | GUID (PK) | Primary key |
| UserId | GUID (FK) | Người tạo (Teacher) |
| WorkspaceId | GUID (FK, Nullable) | Workspace chứa QuestionBank |
| MapId | GUID (FK, Nullable) | Map được gắn sẵn (optional) |
| BankName | String(200) | Tên bộ câu hỏi |
| Description | Text | Mô tả |
| Category | String(100) | Phân loại (Geography, History, Math...) |
| Tags | String(500) | Tags phân loại, cách nhau dấu phẩy |
| TotalQuestions | Int | Số câu hỏi (denormalized) |
| IsTemplate | Bool | Có phải template không? |
| IsPublic | Bool | Chia sẻ công khai? |
| IsActive | Bool | Đang hoạt động? |
| CreatedAt | DateTime | Ngày tạo |
| UpdatedAt | DateTime | Ngày cập nhật |

**Indexes**:
- `IX_QuestionBank_UserId`
- `IX_QuestionBank_WorkspaceId`
- `IX_QuestionBank_MapId`
- `IX_QuestionBank_Category`

---

### 2. Question (Câu hỏi)
**Purpose**: Chi tiết từng câu hỏi với đa dạng loại câu hỏi

| Field | Type | Description |
|-------|------|-------------|
| QuestionId | GUID (PK) | Primary key |
| QuestionBankId | GUID (FK) | Thuộc QuestionBank nào |
| LocationId | GUID (FK, Nullable) | Gắn với Location trên Map (optional) |
| QuestionType | Enum | MULTIPLE_CHOICE, TRUE_FALSE, SHORT_ANSWER, WORD_CLOUD, PIN_ON_MAP |
| QuestionText | Text | Nội dung câu hỏi |
| QuestionImageUrl | String(500) | Hình ảnh đính kèm (optional) |
| QuestionAudioUrl | String(500) | Audio đính kèm (optional) |
| Points | Int | Điểm số (default: 100) |
| TimeLimit | Int | Thời gian giới hạn (giây) - 0 = không giới hạn |
| CorrectAnswerText | String(500) | Đáp án đúng (for SHORT_ANSWER) |
| CorrectLatitude | Decimal(10,7) | Vĩ độ đúng (for PIN_ON_MAP) |
| CorrectLongitude | Decimal(10,7) | Kinh độ đúng (for PIN_ON_MAP) |
| AcceptanceRadiusMeters | Int | Bán kính chấp nhận (meters, for PIN_ON_MAP) |
| HintText | Text | Gợi ý |
| Explanation | Text | Giải thích đáp án |
| DisplayOrder | Int | Thứ tự hiển thị |
| IsActive | Bool | Đang hoạt động? |
| CreatedAt | DateTime | Ngày tạo |
| UpdatedAt | DateTime | Ngày cập nhật |

**QuestionType Enum**:
```csharp
public enum QuestionTypeEnum
{
    MULTIPLE_CHOICE,    // Trắc nghiệm A,B,C,D
    TRUE_FALSE,         // Đúng/Sai
    SHORT_ANSWER,       // Trả lời ngắn (text)
    WORD_CLOUD,         // Đám mây từ khóa
    PIN_ON_MAP          // Ghim trên bản đồ
}
```

**Indexes**:
- `IX_Question_QuestionBankId`
- `IX_Question_LocationId`
- `IX_Question_QuestionType`

---

### 3. QuestionOption (Đáp án cho trắc nghiệm)
**Purpose**: Các lựa chọn cho câu hỏi MULTIPLE_CHOICE và TRUE_FALSE

| Field | Type | Description |
|-------|------|-------------|
| QuestionOptionId | GUID (PK) | Primary key |
| QuestionId | GUID (FK) | Thuộc câu hỏi nào |
| OptionText | String(500) | Nội dung đáp án |
| OptionImageUrl | String(500) | Hình ảnh đáp án (optional) |
| IsCorrect | Bool | Đáp án đúng? |
| DisplayOrder | Int | Thứ tự hiển thị (A=1, B=2, C=3, D=4) |
| CreatedAt | DateTime | Ngày tạo |

**Indexes**:
- `IX_QuestionOption_QuestionId`

---

### 4. Session (Phiên học)
**Purpose**: Phiên tương tác cho một lớp học cụ thể

| Field | Type | Description |
|-------|------|-------------|
| SessionId | GUID (PK) | Primary key |
| MapId | GUID (FK) | Bản đồ sử dụng |
| QuestionBankId | GUID (FK) | Bộ câu hỏi sử dụng |
| HostUserId | GUID (FK) | Giáo viên chủ trì |
| SessionCode | String(10) | Mã PIN (unique, ví dụ: 123456) |
| SessionName | String(200) | Tên phiên (VD: "Lớp 10A - Địa lý Việt Nam") |
| Description | Text | Mô tả |
| SessionType | Enum | LIVE, SELF_PACED, PRACTICE |
| Status | Enum | DRAFT, WAITING, IN_PROGRESS, PAUSED, COMPLETED, CANCELLED |
| MaxParticipants | Int | Số học sinh tối đa (0 = unlimited) |
| AllowLateJoin | Bool | Cho phép join muộn? |
| ShowLeaderboard | Bool | Hiển thị bảng xếp hạng? |
| ShowCorrectAnswers | Bool | Hiển thị đáp án đúng sau khi trả lời? |
| ShuffleQuestions | Bool | Xáo trộn thứ tự câu hỏi? |
| ShuffleOptions | Bool | Xáo trộn thứ tự đáp án? |
| EnableHints | Bool | Cho phép gợi ý? |
| PointsForSpeed | Bool | Cộng điểm cho tốc độ? |
| ScheduledStartTime | DateTime (Nullable) | Thời gian bắt đầu dự kiến |
| ActualStartTime | DateTime (Nullable) | Thời gian bắt đầu thực tế |
| EndTime | DateTime (Nullable) | Thời gian kết thúc |
| TotalParticipants | Int | Tổng số người tham gia (denormalized) |
| TotalResponses | Int | Tổng số câu trả lời (denormalized) |
| CreatedAt | DateTime | Ngày tạo |
| UpdatedAt | DateTime | Ngày cập nhật |

**SessionType Enum**:
```csharp
public enum SessionTypeEnum
{
    LIVE,          // Giáo viên điều khiển real-time
    SELF_PACED,    // Học sinh tự làm theo tốc độ riêng
    PRACTICE       // Chế độ luyện tập
}
```

**SessionStatus Enum**:
```csharp
public enum SessionStatusEnum
{
    DRAFT,          // Đang soạn
    WAITING,        // Sảnh chờ (students joining)
    IN_PROGRESS,    // Đang diễn ra
    PAUSED,         // Tạm dừng
    COMPLETED,      // Hoàn thành
    CANCELLED       // Đã hủy
}
```

**Indexes**:
- `UX_Session_SessionCode` (Unique)
- `IX_Session_MapId`
- `IX_Session_QuestionBankId`
- `IX_Session_HostUserId`
- `IX_Session_Status`

---

### 5. SessionQuestion (Câu hỏi trong phiên)
**Purpose**: Quản lý queue câu hỏi và trạng thái trong session

| Field | Type | Description |
|-------|------|-------------|
| SessionQuestionId | GUID (PK) | Primary key |
| SessionId | GUID (FK) | Thuộc phiên nào |
| QuestionId | GUID (FK) | Câu hỏi gốc |
| QueueOrder | Int | Thứ tự trong queue |
| Status | Enum | QUEUED, ACTIVE, SKIPPED, COMPLETED |
| PointsOverride | Int (Nullable) | Ghi đè điểm số (nếu khác Question.Points) |
| TimeLimitOverride | Int (Nullable) | Ghi đè thời gian (nếu khác Question.TimeLimit) |
| TimeLimitExtensions | Int | Số lần đã extend time (default: 0) |
| StartedAt | DateTime (Nullable) | Thời điểm bắt đầu câu hỏi |
| EndedAt | DateTime (Nullable) | Thời điểm kết thúc câu hỏi |
| TotalResponses | Int | Số lượng trả lời (denormalized) |
| CorrectResponses | Int | Số lượng trả lời đúng (denormalized) |
| CreatedAt | DateTime | Ngày tạo |
| UpdatedAt | DateTime | Ngày cập nhật |

**SessionQuestionStatus Enum**:
```csharp
public enum SessionQuestionStatusEnum
{
    QUEUED,      // Đang chờ trong queue
    ACTIVE,      // Đang hiển thị
    SKIPPED,     // Bỏ qua
    COMPLETED    // Đã hoàn thành
}
```

**Indexes**:
- `IX_SessionQuestion_SessionId`
- `IX_SessionQuestion_QuestionId`
- `IX_SessionQuestion_QueueOrder`

---

### 6. SessionParticipant (Người tham gia phiên)
**Purpose**: Tracking học sinh trong session

| Field | Type | Description |
|-------|------|-------------|
| SessionParticipantId | GUID (PK) | Primary key |
| SessionId | GUID (FK) | Thuộc phiên nào |
| UserId | GUID (FK, Nullable) | Nếu user đã đăng nhập |
| DisplayName | String(100) | Tên hiển thị (cho guest) |
| IsGuest | Bool | Là khách không đăng nhập? |
| JoinedAt | DateTime | Thời gian tham gia |
| LeftAt | DateTime (Nullable) | Thời gian rời đi |
| IsActive | Bool | Còn trong phiên? |
| TotalScore | Int | Tổng điểm |
| TotalCorrect | Int | Tổng số câu đúng |
| TotalAnswered | Int | Tổng số câu đã trả lời |
| AverageResponseTime | Decimal(10,2) | Thời gian trả lời trung bình (giây) |
| Rank | Int | Thứ hạng hiện tại |
| DeviceInfo | String(500) | Thông tin thiết bị (optional) |
| IpAddress | String(50) | IP address (optional) |
| CreatedAt | DateTime | Ngày tạo |
| UpdatedAt | DateTime | Ngày cập nhật |

**Indexes**:
- `IX_SessionParticipant_SessionId`
- `IX_SessionParticipant_UserId`
- `IX_SessionParticipant_TotalScore` (For leaderboard)

---

### 7. StudentResponse (Câu trả lời)
**Purpose**: Lưu trữ tất cả câu trả lời của học sinh

| Field | Type | Description |
|-------|------|-------------|
| StudentResponseId | GUID (PK) | Primary key |
| SessionQuestionId | GUID (FK) | Câu hỏi trong phiên |
| SessionParticipantId | GUID (FK) | Người trả lời |
| QuestionOptionId | GUID (FK, Nullable) | Đáp án được chọn (for MULTIPLE_CHOICE) |
| ResponseText | String(1000) | Câu trả lời dạng text (for SHORT_ANSWER, WORD_CLOUD) |
| ResponseLatitude | Decimal(10,7) | Vĩ độ (for PIN_ON_MAP) |
| ResponseLongitude | Decimal(10,7) | Kinh độ (for PIN_ON_MAP) |
| IsCorrect | Bool | Trả lời đúng? |
| PointsEarned | Int | Điểm nhận được |
| ResponseTimeSeconds | Decimal(10,2) | Thời gian trả lời (giây từ lúc câu hỏi xuất hiện) |
| UsedHint | Bool | Có dùng gợi ý? |
| DistanceErrorMeters | Decimal(10,2) | Khoảng cách sai lệch (for PIN_ON_MAP) |
| SubmittedAt | DateTime | Thời gian submit |
| CreatedAt | DateTime | Ngày tạo |

**Indexes**:
- `IX_StudentResponse_SessionQuestionId`
- `IX_StudentResponse_SessionParticipantId`
- `IX_StudentResponse_QuestionOptionId`
- `IX_StudentResponse_IsCorrect`

---

### 8. SessionMapState (Trạng thái bản đồ trong phiên)
**Purpose**: Đồng bộ camera view giữa teacher và students (Teacher Focus)

| Field | Type | Description |
|-------|------|-------------|
| SessionMapStateId | GUID (PK) | Primary key |
| SessionId | GUID (FK) | Thuộc phiên nào |
| SessionQuestionId | GUID (FK, Nullable) | Gắn với câu hỏi nào (optional) |
| Latitude | Decimal(10,7) | Vĩ độ center |
| Longitude | Decimal(10,7) | Kinh độ center |
| ZoomLevel | Decimal(4,2) | Mức zoom |
| Bearing | Decimal(5,2) | Góc xoay bản đồ (degrees) |
| Pitch | Decimal(4,2) | Góc nghiêng (degrees) |
| TransitionDuration | Int | Thời gian transition (ms) |
| HighlightedLocationId | GUID (Nullable) | Location đang highlight |
| HighlightedLayerId | GUID (Nullable) | Layer đang highlight |
| IsLocked | Bool | Khóa view của học sinh? |
| CreatedAt | DateTime | Thời điểm tạo |

**Indexes**:
- `IX_SessionMapState_SessionId`
- `IX_SessionMapState_SessionQuestionId`

---

## Relationships Summary

### QuestionBank Relationships
```csharp
QuestionBank (1) ──── (N) Question
QuestionBank (1) ──── (N) Session
QuestionBank (N) ──── (1) User (Creator)
QuestionBank (N) ──── (1) Workspace (Optional)
QuestionBank (N) ──── (1) Map (Optional, default map)
```

### Question Relationships
```csharp
Question (1) ──── (N) QuestionOption
Question (1) ──── (N) SessionQuestion
Question (N) ──── (1) QuestionBank
Question (N) ──── (1) Location (Optional, for map-based questions)
```

### Session Relationships
```csharp
Session (1) ──── (N) SessionQuestion
Session (1) ──── (N) SessionParticipant
Session (1) ──── (N) SessionMapState
Session (N) ──── (1) Map
Session (N) ──── (1) QuestionBank
Session (N) ──── (1) User (Host/Teacher)
```

### SessionQuestion Relationships
```csharp
SessionQuestion (1) ──── (N) StudentResponse
SessionQuestion (1) ──── (1) SessionMapState (Optional)
SessionQuestion (N) ──── (1) Session
SessionQuestion (N) ──── (1) Question
```

### StudentResponse Relationships
```csharp
StudentResponse (N) ──── (1) SessionQuestion
StudentResponse (N) ──── (1) SessionParticipant
StudentResponse (N) ──── (1) QuestionOption (Optional, for multiple choice)
```

---

## Delete Behaviors

### Cascade Deletes
- QuestionBank deleted → Delete all Questions
- Question deleted → Delete all QuestionOptions
- Session deleted → Delete all SessionQuestions, SessionParticipants, SessionMapState
- SessionQuestion deleted → Delete all StudentResponses

### Set Null
- User deleted → QuestionBank.UserId = NULL
- Map deleted → Session.MapId = NULL (hoặc restrict)
- Location deleted → Question.LocationId = NULL

---

## Indexes Strategy

### Performance Indexes
1. **Leaderboard Queries**: `IX_SessionParticipant_SessionId_TotalScore`
2. **Analytics Queries**: `IX_StudentResponse_SessionQuestionId_IsCorrect`
3. **Real-time Updates**: `IX_SessionMapState_SessionId_CreatedAt`

### Unique Constraints
- `Session.SessionCode` UNIQUE
- Composite: `(SessionId, UserId)` for SessionParticipant (một user chỉ join 1 lần)
- Composite: `(SessionId, QueueOrder)` for SessionQuestion (không trùng thứ tự)

---

## Additional Features to Consider

### 1. Analytics Tables (Optional - Future Enhancement)
```
SessionAnalytics:
- SessionId, AverageScore, CompletionRate, TotalDuration
- QuestionDifficultyScore (% trả lời đúng)
```

### 2. Word Cloud Processing (Optional)
```
WordCloudData:
- SessionQuestionId, Word, Frequency, Sentiment (positive/negative)
```

### 3. Session Recording (Optional)
```
SessionEvent:
- EventType (QUESTION_STARTED, ANSWER_SUBMITTED, TIME_EXTENDED)
- Timestamp, Metadata (JSON)
```

---

## Migration Strategy

### Phase 1: Core Entities
1. QuestionBank
2. Question
3. QuestionOption

### Phase 2: Session Management
4. Session
5. SessionQuestion
6. SessionParticipant

### Phase 3: Response Tracking
7. StudentResponse
8. SessionMapState

---

## Sample Data Flow

### Teacher Creates Session
```sql
1. INSERT INTO question_bank (bank_name, user_id) VALUES ('Geography Vietnam', '{userId}');
2. INSERT INTO question (question_text, question_type, ...) VALUES ('What is capital?', 'MULTIPLE_CHOICE', ...);
3. INSERT INTO question_option (question_id, option_text, is_correct) VALUES ('{qId}', 'Hanoi', true);
4. INSERT INTO session (map_id, question_bank_id, session_code, ...) VALUES ('{mapId}', '{bankId}', '123456', ...);
```

### Student Joins
```sql
1. INSERT INTO session_participant (session_id, display_name, ...) VALUES ('{sessionId}', 'John Doe', ...);
```

### Question Presented
```sql
1. INSERT INTO session_question (session_id, question_id, queue_order, status) VALUES ('{sId}', '{qId}', 1, 'ACTIVE');
2. INSERT INTO session_map_state (session_id, latitude, longitude, zoom_level, is_locked) VALUES ('{sId}', 21.0285, 105.8542, 12, true);
```

### Student Responds
```sql
1. INSERT INTO student_response (session_question_id, session_participant_id, question_option_id, is_correct, points_earned, response_time_seconds)
   VALUES ('{sqId}', '{spId}', '{optionId}', true, 100, 5.2);
2. UPDATE session_participant SET total_score = total_score + 100, total_correct = total_correct + 1;
```

---

## JSON Schema Examples

### Session.Metadata (Optional field for flexibility)
```json
{
  "classCode": "10A",
  "semester": "Fall 2024",
  "subject": "Geography",
  "customSettings": {
    "backgroundMusic": true,
    "soundEffects": true
  }
}
```

### SessionEvent.Metadata (For session recording)
```json
{
  "eventType": "TIME_EXTENDED",
  "questionId": "abc123",
  "extensionSeconds": 10,
  "triggeredBy": "teacher"
}
```

---

## Performance Considerations

### Expected Load
- **Concurrent Sessions**: 100-1000 sessions simultaneously
- **Students per Session**: 30-50 students
- **Real-time Updates**: WebSocket broadcasts every 100ms for map state

### Optimization Strategies
1. **Denormalization**: Store TotalScore, TotalResponses directly in parent tables
2. **Caching**: Redis for active sessions, leaderboard data
3. **Partitioning**: Partition StudentResponse by SessionId for large datasets
4. **Archiving**: Move completed sessions to archive tables after 30 days

---

## Security Considerations

1. **Session Code**: 6-digit PIN, regenerate if duplicate
2. **Rate Limiting**: Limit response submissions (1 per question per participant)
3. **Authorization**: Only session host can control session
4. **Data Privacy**: Option to anonymize student data after session completion

---

This design supports all requested features:
✅ Session-based isolation (each class has separate data)
✅ Multiple question types (MCQ, Word Cloud, Pin on Map)
✅ Real-time map synchronization (SessionMapState)
✅ Leaderboard & analytics (SessionParticipant scores)
✅ Question queue management (SessionQuestion with status)
✅ Control features (Skip, Pause, Extend time via SessionQuestion)
