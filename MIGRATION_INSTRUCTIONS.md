# Migration Instructions for Session Management Feature

## Overview
All database entities, configurations, and DbContext updates have been completed for the Session Management feature. You now need to create and apply the database migration.

## Prerequisites
- .NET SDK installed (version compatible with your project)
- Access to your development environment
- Database connection configured in `appsettings.json`

## Step 1: Create Migration

Run the following command from the solution root directory:

```bash
cd /home/user/FA25_CapstoneProject_BE/FA25_CusomMapOSM_BE
make new-migration name=AddSessionManagementEntities
```

Or use the full command:

```bash
dotnet ef migrations add AddSessionManagementEntities \
  --project CusomMapOSM_Infrastructure \
  --startup-project CusomMapOSM_API \
  --verbose
```

This will create a new migration file in:
`CusomMapOSM_Infrastructure/Migrations/`

## Step 2: Review Migration

Before applying, review the generated migration file to ensure:
1. All 8 new tables are being created:
   - `question_banks`
   - `questions`
   - `question_options`
   - `sessions`
   - `session_questions`
   - `session_participants`
   - `student_responses`
   - `session_map_states`

2. All foreign key relationships are correct
3. All indexes are created (especially the unique constraints)

## Step 3: Apply Migration

Run the following command to update the database:

```bash
cd /home/user/FA25_CapstoneProject_BE/FA25_CusomMapOSM_BE
make migration
```

Or use the full command:

```bash
dotnet ef database update \
  --project CusomMapOSM_Infrastructure \
  --startup-project CusomMapOSM_API
```

## Step 4: Verify Tables

After migration, verify the tables were created correctly:

```sql
-- Check all new tables exist
SHOW TABLES LIKE '%question%';
SHOW TABLES LIKE '%session%';

-- Verify structure of key tables
DESCRIBE question_banks;
DESCRIBE sessions;
DESCRIBE session_participants;

-- Check indexes
SHOW INDEX FROM sessions WHERE Key_name = 'UX_Session_SessionCode';
SHOW INDEX FROM session_participants WHERE Key_name = 'UX_SessionParticipant_SessionId_UserId';
```

## Expected Table Structure

### question_banks
- Primary Key: `question_bank_id` (GUID)
- Foreign Keys: `user_id`, `workspace_id`, `map_id`
- Indexes: UserId, WorkspaceId, MapId, Category, IsActive

### questions
- Primary Key: `question_id` (GUID)
- Foreign Keys: `question_bank_id`, `location_id`
- Enum: `question_type` (1-5)
- Indexes: QuestionBankId, LocationId, QuestionType, IsActive

### question_options
- Primary Key: `question_option_id` (GUID)
- Foreign Key: `question_id`
- Indexes: QuestionId, IsCorrect

### sessions
- Primary Key: `session_id` (GUID)
- Foreign Keys: `map_id`, `question_bank_id`, `host_user_id`
- **Unique Index**: `session_code` (10 chars)
- Indexes: MapId, QuestionBankId, HostUserId, Status, SessionType

### session_questions
- Primary Key: `session_question_id` (GUID)
- Foreign Keys: `session_id`, `question_id`
- **Unique Composite**: (session_id, queue_order)
- Indexes: SessionId, QuestionId, Status

### session_participants
- Primary Key: `session_participant_id` (GUID)
- Foreign Keys: `session_id`, `user_id` (nullable)
- **Unique Composite**: (session_id, user_id) - filtered for non-null user_id
- Indexes: SessionId, UserId, (SessionId, TotalScore), IsActive

### student_responses
- Primary Key: `student_response_id` (GUID)
- Foreign Keys: `session_question_id`, `session_participant_id`, `question_option_id`
- **Unique Composite**: (session_question_id, session_participant_id)
- Indexes: SessionQuestionId, SessionParticipantId, QuestionOptionId, IsCorrect

### session_map_states
- Primary Key: `session_map_state_id` (GUID)
- Foreign Keys: `session_id`, `session_question_id`, `highlighted_location_id`, `highlighted_layer_id`
- Indexes: SessionId, SessionQuestionId, (SessionId, CreatedAt), IsLocked

## Troubleshooting

### Migration Creation Fails
If you encounter errors during migration creation:

1. **Check Entity References**: Ensure all navigation properties reference existing entities
2. **Verify Namespaces**: Check all `using` statements in entity files
3. **Rebuild Solution**: Run `dotnet build` before creating migration
4. **Check Configuration**: Ensure all entity configurations are in the correct namespace

### Migration Application Fails

1. **Foreign Key Constraints**: Check if referenced tables exist
   - Ensure `users`, `maps`, `workspaces`, `locations`, `layers` tables exist

2. **Index Conflicts**: If you have existing data that violates unique constraints:
   ```sql
   -- Check for duplicate session codes
   SELECT session_code, COUNT(*) FROM sessions GROUP BY session_code HAVING COUNT(*) > 1;
   ```

3. **Decimal Precision**: MySQL may have issues with decimal types:
   - Latitude/Longitude: `decimal(10,7)`
   - ResponseTime: `decimal(10,2)`

## Rollback (if needed)

If you need to rollback the migration:

```bash
# Revert to previous migration
dotnet ef database update <PreviousMigrationName> \
  --project CusomMapOSM_Infrastructure \
  --startup-project CusomMapOSM_API

# Remove the migration file
dotnet ef migrations remove \
  --project CusomMapOSM_Infrastructure \
  --startup-project CusomMapOSM_API
```

## Next Steps

After successful migration:

1. **Test Entity Creation**: Try creating a QuestionBank and Questions
2. **Test Session Creation**: Create a test session with a PIN code
3. **Verify Relationships**: Ensure foreign key relationships work correctly
4. **Test Unique Constraints**: Try creating duplicate session codes (should fail)
5. **Check Cascade Deletes**: Verify that deleting a Session deletes related SessionQuestions

## Additional Notes

- All entities use **GUID** as primary keys for distributed scalability
- Timestamps use **UTC** timezone (`DateTime.UtcNow`)
- Session codes are **unique** across all sessions
- One participant can only join a session **once** (composite unique constraint)
- Student can only respond to a question **once** (composite unique constraint)

## Contact

If you encounter any issues during migration, please review:
1. The design document: `SESSION_MANAGEMENT_DATABASE_DESIGN.md`
2. Entity configurations in: `CusomMapOSM_Infrastructure/Databases/Configurations/`
3. Entity classes in: `CusomMapOSM_Domain/Entities/`
