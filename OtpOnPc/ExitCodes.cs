namespace OtpOnPc;

public enum ExitCodes
{
    // Settings
    FailedToChangeRepository = -001,

    // Accounts
    FailedToAddAccount = -011,
    FailedToMoveAccount = -012,
    FailedToDeleteAccount = -013,

    // Startup
    FailedToRestore = -021
}
