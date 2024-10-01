namespace NetPad;

public enum ProgramExitCode
{
    Success = 0,
    UnexpectedError = 1,
    SwaggerGenError = 2,
    PortUnavailable = 3,
    InvalidParentProcessPid = 4,
    ParentProcessIsNotRunning = 5,
}
