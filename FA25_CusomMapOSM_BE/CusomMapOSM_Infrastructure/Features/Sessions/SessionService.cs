using CusomMapOSM_Application.Common.Errors;
using CusomMapOSM_Application.Interfaces.Features.Sessions;
using CusomMapOSM_Application.Interfaces.Services.Auth;
using CusomMapOSM_Application.Models.DTOs.Features.Sessions.Request;
using CusomMapOSM_Application.Models.DTOs.Features.Sessions.Response;
using CusomMapOSM_Domain.Entities.QuestionBanks;
using CusomMapOSM_Domain.Entities.Sessions;
using CusomMapOSM_Domain.Entities.Sessions.Enums;
using CusomMapOSM_Infrastructure.Databases.Repositories.Interfaces.QuestionBanks;
using CusomMapOSM_Infrastructure.Databases.Repositories.Interfaces.Sessions;
using Optional;

namespace CusomMapOSM_Infrastructure.Features.Sessions;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionParticipantRepository _participantRepository;
    private readonly ISessionQuestionRepository _sessionQuestionRepository;
    private readonly IStudentResponseRepository _responseRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly ICurrentUserService _currentUserService;

    public SessionService(
        ISessionRepository sessionRepository,
        ISessionParticipantRepository participantRepository,
        ISessionQuestionRepository sessionQuestionRepository,
        IStudentResponseRepository responseRepository,
        IQuestionRepository questionRepository,
        ICurrentUserService currentUserService)
    {
        _sessionRepository = sessionRepository;
        _participantRepository = participantRepository;
        _sessionQuestionRepository = sessionQuestionRepository;
        _responseRepository = responseRepository;
        _questionRepository = questionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Option<CreateSessionResponse, Error>> CreateSession(CreateSessionRequest request)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<CreateSessionResponse, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        // Generate unique session code
        var sessionCode = await _sessionRepository.GenerateUniqueSessionCode();

        // Create session
        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            MapId = request.MapId,
            QuestionBankId = request.QuestionBankId,
            HostUserId = currentUserId.Value,
            SessionCode = sessionCode,
            SessionName = request.SessionName,
            Description = request.Description,
            SessionType = request.SessionType,
            Status = SessionStatusEnum.DRAFT,
            MaxParticipants = request.MaxParticipants,
            AllowLateJoin = request.AllowLateJoin,
            ShowLeaderboard = request.ShowLeaderboard,
            ShowCorrectAnswers = request.ShowCorrectAnswers,
            ShuffleQuestions = request.ShuffleQuestions,
            ShuffleOptions = request.ShuffleOptions,
            EnableHints = request.EnableHints,
            PointsForSpeed = request.PointsForSpeed,
            ScheduledStartTime = request.ScheduledStartTime,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _sessionRepository.CreateSession(session);
        if (!created)
        {
            return Option.None<CreateSessionResponse, Error>(
                Error.Failure("Session.CreateFailed", "Failed to create session"));
        }

        // Create session questions from question bank
        var questions = await _questionRepository.GetQuestionsByQuestionBankId(request.QuestionBankId);
        if (request.ShuffleQuestions)
        {
            questions = questions.OrderBy(_ => Guid.NewGuid()).ToList();
        }

        var sessionQuestions = questions.Select((q, index) => new SessionQuestion
        {
            SessionQuestionId = Guid.NewGuid(),
            SessionId = session.SessionId,
            QuestionId = q.QuestionId,
            QueueOrder = index + 1,
            Status = SessionQuestionStatusEnum.QUEUED,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _sessionQuestionRepository.CreateSessionQuestions(sessionQuestions);

        return Option.Some<CreateSessionResponse, Error>(new CreateSessionResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            SessionName = session.SessionName,
            Message = "Session created successfully",
            CreatedAt = session.CreatedAt
        });
    }

    public async Task<Option<GetSessionResponse, Error>> GetSessionById(Guid sessionId)
    {
        var session = await _sessionRepository.GetSessionById(sessionId);
        if (session == null)
        {
            return Option.None<GetSessionResponse, Error>(
                Error.NotFound("Session.NotFound", "Session not found"));
        }

        return Option.Some<GetSessionResponse, Error>(new GetSessionResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            SessionName = session.SessionName,
            Description = session.Description,
            SessionType = session.SessionType,
            Status = session.Status,
            MapId = session.MapId,
            MapName = session.Map?.MapName ?? string.Empty,
            QuestionBankId = session.QuestionBankId,
            QuestionBankName = session.QuestionBank?.BankName ?? string.Empty,
            HostUserId = session.HostUserId,
            HostUserName = session.HostUser?.FullName ?? string.Empty,
            MaxParticipants = session.MaxParticipants,
            TotalParticipants = session.TotalParticipants,
            TotalResponses = session.TotalResponses,
            AllowLateJoin = session.AllowLateJoin,
            ShowLeaderboard = session.ShowLeaderboard,
            ShowCorrectAnswers = session.ShowCorrectAnswers,
            ScheduledStartTime = session.ScheduledStartTime,
            ActualStartTime = session.ActualStartTime,
            EndTime = session.EndTime,
            CreatedAt = session.CreatedAt
        });
    }

    public async Task<Option<GetSessionResponse, Error>> GetSessionByCode(string sessionCode)
    {
        var session = await _sessionRepository.GetSessionByCode(sessionCode);
        if (session == null)
        {
            return Option.None<GetSessionResponse, Error>(
                Error.NotFound("Session.NotFound", "Session not found"));
        }

        return await GetSessionById(session.SessionId);
    }

    public async Task<Option<List<GetSessionResponse>, Error>> GetMySessionsAsHost()
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<List<GetSessionResponse>, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var sessions = await _sessionRepository.GetSessionsByHostUserId(currentUserId.Value);

        var response = sessions.Select(s => new GetSessionResponse
        {
            SessionId = s.SessionId,
            SessionCode = s.SessionCode,
            SessionName = s.SessionName,
            Description = s.Description,
            SessionType = s.SessionType,
            Status = s.Status,
            MapId = s.MapId,
            MapName = s.Map?.MapName ?? string.Empty,
            QuestionBankId = s.QuestionBankId,
            QuestionBankName = s.QuestionBank?.BankName ?? string.Empty,
            HostUserId = s.HostUserId,
            HostUserName = s.HostUser?.FullName ?? string.Empty,
            TotalParticipants = s.TotalParticipants,
            TotalResponses = s.TotalResponses,
            CreatedAt = s.CreatedAt
        }).ToList();

        return Option.Some<List<GetSessionResponse>, Error>(response);
    }

    public async Task<Option<bool, Error>> DeleteSession(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can delete the session"));
        }

        var deleted = await _sessionRepository.DeleteSession(sessionId);
        return deleted
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.DeleteFailed", "Failed to delete session"));
    }

    public async Task<Option<bool, Error>> StartSession(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can start the session"));
        }

        var started = await _sessionRepository.StartSession(sessionId);
        return started
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.StartFailed", "Failed to start session"));
    }

    public async Task<Option<bool, Error>> PauseSession(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can pause the session"));
        }

        var paused = await _sessionRepository.PauseSession(sessionId);
        return paused
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.PauseFailed", "Failed to pause session"));
    }

    public async Task<Option<bool, Error>> ResumeSession(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can resume the session"));
        }

        var resumed = await _sessionRepository.ResumeSession(sessionId);
        return resumed
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.ResumeFailed", "Failed to resume session"));
    }

    public async Task<Option<bool, Error>> EndSession(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can end the session"));
        }

        var ended = await _sessionRepository.EndSession(sessionId);
        if (!ended)
        {
            return Option.None<bool, Error>(
                Error.Failure("Session.EndFailed", "Failed to end session"));
        }

        // Mark all participants as left
        await _participantRepository.MarkAllParticipantsAsLeft(sessionId);

        return Option.Some<bool, Error>(true);
    }

    public async Task<Option<JoinSessionResponse, Error>> JoinSession(JoinSessionRequest request)
    {
        var session = await _sessionRepository.GetSessionByCode(request.SessionCode);
        if (session == null)
        {
            return Option.None<JoinSessionResponse, Error>(
                Error.NotFound("Session.NotFound", "Session not found with this code"));
        }

        // Check if session allows joining
        if (session.Status != SessionStatusEnum.WAITING && session.Status != SessionStatusEnum.IN_PROGRESS)
        {
            return Option.None<JoinSessionResponse, Error>(
                Error.ValidationError("Session.NotJoinable", "Session is not accepting participants"));
        }

        if (!session.AllowLateJoin && session.Status == SessionStatusEnum.IN_PROGRESS)
        {
            return Option.None<JoinSessionResponse, Error>(
                Error.ValidationError("Session.LateJoinDisabled", "Late join is not allowed for this session"));
        }

        // Check max participants
        if (session.MaxParticipants > 0 && session.TotalParticipants >= session.MaxParticipants)
        {
            return Option.None<JoinSessionResponse, Error>(
                Error.ValidationError("Session.Full", "Session has reached maximum participants"));
        }

        var currentUserId = _currentUserService.GetUserId();

        // Check if user already joined
        if (currentUserId != null)
        {
            var alreadyJoined = await _participantRepository.CheckUserAlreadyJoined(session.SessionId, currentUserId.Value);
            if (alreadyJoined)
            {
                return Option.None<JoinSessionResponse, Error>(
                    Error.Conflict("Session.AlreadyJoined", "You have already joined this session"));
            }
        }

        // Create participant
        var participant = new SessionParticipant
        {
            SessionParticipantId = Guid.NewGuid(),
            SessionId = session.SessionId,
            UserId = currentUserId,
            DisplayName = request.DisplayName,
            IsGuest = currentUserId == null,
            DeviceInfo = request.DeviceInfo,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _participantRepository.CreateParticipant(participant);
        if (!created)
        {
            return Option.None<JoinSessionResponse, Error>(
                Error.Failure("Session.JoinFailed", "Failed to join session"));
        }

        // Update session participant count
        await _sessionRepository.UpdateParticipantCount(session.SessionId);

        return Option.Some<JoinSessionResponse, Error>(new JoinSessionResponse
        {
            SessionParticipantId = participant.SessionParticipantId,
            SessionId = session.SessionId,
            SessionName = session.SessionName,
            DisplayName = participant.DisplayName,
            Message = "Joined session successfully",
            JoinedAt = participant.JoinedAt
        });
    }

    public async Task<Option<bool, Error>> LeaveSession(Guid sessionParticipantId)
    {
        var participant = await _participantRepository.GetParticipantById(sessionParticipantId);
        if (participant == null)
        {
            return Option.None<bool, Error>(
                Error.NotFound("Participant.NotFound", "Participant not found"));
        }

        var left = await _participantRepository.MarkParticipantAsLeft(sessionParticipantId);
        if (!left)
        {
            return Option.None<bool, Error>(
                Error.Failure("Session.LeaveFailed", "Failed to leave session"));
        }

        // Update session participant count
        await _sessionRepository.UpdateParticipantCount(participant.SessionId);

        return Option.Some<bool, Error>(true);
    }

    public async Task<Option<LeaderboardResponse, Error>> GetLeaderboard(Guid sessionId, int limit = 10)
    {
        var sessionExists = await _sessionRepository.CheckSessionExists(sessionId);
        if (!sessionExists)
        {
            return Option.None<LeaderboardResponse, Error>(
                Error.NotFound("Session.NotFound", "Session not found"));
        }

        var participants = await _participantRepository.GetLeaderboard(sessionId, limit);
        var currentUserId = _currentUserService.GetUserId();

        var leaderboard = participants.Select((p, index) => new LeaderboardEntry
        {
            Rank = index + 1,
            SessionParticipantId = p.SessionParticipantId,
            DisplayName = p.DisplayName,
            TotalScore = p.TotalScore,
            TotalCorrect = p.TotalCorrect,
            TotalAnswered = p.TotalAnswered,
            AverageResponseTime = p.AverageResponseTime,
            IsCurrentUser = currentUserId != null && p.UserId == currentUserId.Value
        }).ToList();

        return Option.Some<LeaderboardResponse, Error>(new LeaderboardResponse
        {
            SessionId = sessionId,
            Leaderboard = leaderboard,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public async Task<Option<bool, Error>> ActivateNextQuestion(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can control questions"));
        }

        // Complete current active question if exists
        var activeQuestion = await _sessionQuestionRepository.GetActiveQuestion(sessionId);
        if (activeQuestion != null)
        {
            await _sessionQuestionRepository.CompleteQuestion(activeQuestion.SessionQuestionId);
        }

        // Get next queued question
        var nextQuestion = await _sessionQuestionRepository.GetNextQueuedQuestion(sessionId);
        if (nextQuestion == null)
        {
            return Option.None<bool, Error>(
                Error.NotFound("Session.NoMoreQuestions", "No more questions in queue"));
        }

        // Activate next question
        var activated = await _sessionQuestionRepository.ActivateQuestion(nextQuestion.SessionQuestionId);
        return activated
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.ActivateFailed", "Failed to activate question"));
    }

    public async Task<Option<bool, Error>> SkipCurrentQuestion(Guid sessionId)
    {
        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can skip questions"));
        }

        var activeQuestion = await _sessionQuestionRepository.GetActiveQuestion(sessionId);
        if (activeQuestion == null)
        {
            return Option.None<bool, Error>(
                Error.NotFound("Session.NoActiveQuestion", "No active question to skip"));
        }

        var skipped = await _sessionQuestionRepository.SkipQuestion(activeQuestion.SessionQuestionId);
        return skipped
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.SkipFailed", "Failed to skip question"));
    }

    public async Task<Option<bool, Error>> ExtendTime(Guid sessionQuestionId, int additionalSeconds)
    {
        var sessionQuestion = await _sessionQuestionRepository.GetSessionQuestionById(sessionQuestionId);
        if (sessionQuestion == null)
        {
            return Option.None<bool, Error>(
                Error.NotFound("SessionQuestion.NotFound", "Session question not found"));
        }

        var currentUserId = _currentUserService.GetUserId();
        if (currentUserId == null)
        {
            return Option.None<bool, Error>(
                Error.Unauthorized("Session.Unauthorized", "User not authenticated"));
        }

        var isHost = await _sessionRepository.CheckUserIsHost(sessionQuestion.SessionId, currentUserId.Value);
        if (!isHost)
        {
            return Option.None<bool, Error>(
                Error.Forbidden("Session.NotHost", "Only the host can extend time"));
        }

        if (additionalSeconds <= 0 || additionalSeconds > 120)
        {
            return Option.None<bool, Error>(
                Error.ValidationError("Session.InvalidTimeExtension", "Time extension must be between 1 and 120 seconds"));
        }

        var extended = await _sessionQuestionRepository.ExtendTimeLimit(sessionQuestionId, additionalSeconds);
        return extended
            ? Option.Some<bool, Error>(true)
            : Option.None<bool, Error>(Error.Failure("Session.ExtendFailed", "Failed to extend time"));
    }

    public async Task<Option<SubmitResponseResponse, Error>> SubmitResponse(Guid participantId, SubmitResponseRequest request)
    {
        // Get participant
        var participant = await _participantRepository.GetParticipantById(participantId);
        if (participant == null)
        {
            return Option.None<SubmitResponseResponse, Error>(
                Error.NotFound("Participant.NotFound", "Participant not found"));
        }

        // Get session question with question details
        var sessionQuestion = await _sessionQuestionRepository.GetSessionQuestionById(request.SessionQuestionId);
        if (sessionQuestion == null)
        {
            return Option.None<SubmitResponseResponse, Error>(
                Error.NotFound("SessionQuestion.NotFound", "Session question not found"));
        }

        // Validate question belongs to participant's session
        if (sessionQuestion.SessionId != participant.SessionId)
        {
            return Option.None<SubmitResponseResponse, Error>(
                Error.ValidationError("Session.QuestionMismatch", "Question does not belong to this session"));
        }

        // Check if question is active
        if (sessionQuestion.Status != SessionQuestionStatusEnum.ACTIVE)
        {
            return Option.None<SubmitResponseResponse, Error>(
                Error.ValidationError("SessionQuestion.NotActive", "Question is not currently active"));
        }

        // Check if already answered
        var alreadyAnswered = await _responseRepository.CheckParticipantAlreadyAnswered(
            request.SessionQuestionId, participantId);
        if (alreadyAnswered)
        {
            return Option.None<SubmitResponseResponse, Error>(
                Error.Conflict("Response.AlreadySubmitted", "You have already submitted a response for this question"));
        }

        var question = sessionQuestion.Question!;
        bool isCorrect = false;
        decimal? distanceError = null;

        // Validate and score based on question type
        switch (question.QuestionType)
        {
            case QuestionTypeEnum.MULTIPLE_CHOICE:
            case QuestionTypeEnum.TRUE_FALSE:
                if (request.QuestionOptionId == null)
                {
                    return Option.None<SubmitResponseResponse, Error>(
                        Error.ValidationError("Response.MissingOption", "Question option is required"));
                }
                var option = question.QuestionOptions?.FirstOrDefault(o => o.QuestionOptionId == request.QuestionOptionId);
                if (option == null)
                {
                    return Option.None<SubmitResponseResponse, Error>(
                        Error.ValidationError("Response.InvalidOption", "Invalid question option"));
                }
                isCorrect = option.IsCorrect;
                break;

            case QuestionTypeEnum.SHORT_ANSWER:
                if (string.IsNullOrWhiteSpace(request.ResponseText))
                {
                    return Option.None<SubmitResponseResponse, Error>(
                        Error.ValidationError("Response.MissingText", "Response text is required"));
                }
                isCorrect = string.Equals(
                    request.ResponseText?.Trim(),
                    question.CorrectAnswerText?.Trim(),
                    StringComparison.OrdinalIgnoreCase);
                break;

            case QuestionTypeEnum.WORD_CLOUD:
                if (string.IsNullOrWhiteSpace(request.ResponseText))
                {
                    return Option.None<SubmitResponseResponse, Error>(
                        Error.ValidationError("Response.MissingText", "Response text is required"));
                }
                // Word cloud doesn't have right/wrong answers
                isCorrect = true;
                break;

            case QuestionTypeEnum.PIN_ON_MAP:
                if (request.ResponseLatitude == null || request.ResponseLongitude == null)
                {
                    return Option.None<SubmitResponseResponse, Error>(
                        Error.ValidationError("Response.MissingCoordinates", "Latitude and longitude are required"));
                }
                if (question.CorrectLatitude == null || question.CorrectLongitude == null)
                {
                    return Option.None<SubmitResponseResponse, Error>(
                        Error.ValidationError("Question.NoCorrectLocation", "Question does not have a correct location set"));
                }

                // Calculate distance using Haversine formula
                distanceError = CalculateDistance(
                    (double)question.CorrectLatitude.Value,
                    (double)question.CorrectLongitude.Value,
                    (double)request.ResponseLatitude.Value,
                    (double)request.ResponseLongitude.Value);

                var acceptanceRadius = question.AcceptanceRadiusMeters ?? 1000; // Default 1km
                isCorrect = distanceError <= acceptanceRadius;
                break;
        }

        // Calculate points
        var basePoints = sessionQuestion.PointsOverride ?? question.Points;
        var pointsEarned = 0;

        if (isCorrect)
        {
            pointsEarned = basePoints;

            // Bonus points for speed if enabled
            var session = await _sessionRepository.GetSessionById(participant.SessionId);
            if (session?.PointsForSpeed == true && request.ResponseTimeSeconds > 0)
            {
                var timeLimit = sessionQuestion.TimeLimitOverride ?? question.TimeLimit;
                if (timeLimit > 0)
                {
                    // Bonus: 0-50% based on speed (faster = more bonus)
                    var speedRatio = 1 - (request.ResponseTimeSeconds / timeLimit);
                    if (speedRatio > 0)
                    {
                        var bonus = (int)(basePoints * 0.5m * (decimal)speedRatio);
                        pointsEarned += bonus;
                    }
                }
            }
        }

        // Create response
        var response = new StudentResponse
        {
            StudentResponseId = Guid.NewGuid(),
            SessionQuestionId = request.SessionQuestionId,
            SessionParticipantId = participantId,
            QuestionOptionId = request.QuestionOptionId,
            ResponseText = request.ResponseText,
            ResponseLatitude = request.ResponseLatitude,
            ResponseLongitude = request.ResponseLongitude,
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            ResponseTimeSeconds = request.ResponseTimeSeconds,
            UsedHint = request.UsedHint,
            DistanceErrorMeters = distanceError,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _responseRepository.CreateResponse(response);
        if (!created)
        {
            return Option.None<SubmitResponseResponse, Error>(
                Error.Failure("Response.CreateFailed", "Failed to submit response"));
        }

        // Update session question stats
        await _sessionQuestionRepository.IncrementResponseCount(request.SessionQuestionId, isCorrect);

        // Update participant stats and score
        await _participantRepository.UpdateParticipantScore(participantId, pointsEarned);
        await _participantRepository.UpdateParticipantStats(participantId);

        // Update rankings
        await _participantRepository.UpdateParticipantRankings(participant.SessionId);

        // Get updated rank
        var currentRank = await _participantRepository.GetParticipantRank(participantId);

        // Get updated participant for total score
        var updatedParticipant = await _participantRepository.GetParticipantById(participantId);

        return Option.Some<SubmitResponseResponse, Error>(new SubmitResponseResponse
        {
            StudentResponseId = response.StudentResponseId,
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            TotalScore = updatedParticipant?.TotalScore ?? 0,
            CurrentRank = currentRank,
            Explanation = question.Explanation,
            Message = isCorrect ? "Correct answer!" : "Incorrect answer",
            SubmittedAt = response.SubmittedAt
        });
    }

    // Helper method to calculate distance between two points (Haversine formula)
    private decimal CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        return (decimal)distance;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
