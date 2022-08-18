namespace NAPS2.Escl.Server;

internal class JobState
{
    public static JobState CreateNewJob()
    {
        return new JobState
        {
            Id = Guid.NewGuid().ToString("D"),
            Status = JobStatus.Pending
        };
    }

    public string Id { get; init; }

    public JobStatus Status { get; set; }
}

internal enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Canceled,
    Aborted
}