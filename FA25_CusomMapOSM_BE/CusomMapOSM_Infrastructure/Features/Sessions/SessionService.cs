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

    // TODO: Implement remaining methods
    public Task<Option<bool, Error>> ActivateNextQuestion(Guid sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<Option<bool, Error>> SkipCurrentQuestion(Guid sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<Option<bool, Error>> ExtendTime(Guid sessionQuestionId, int additionalSeconds)
    {
        throw new NotImplementedException();
    }

    public Task<Option<SubmitResponseResponse, Error>> SubmitResponse(Guid participantId, SubmitResponseRequest request)
    {
        throw new NotImplementedException();
    }
}
