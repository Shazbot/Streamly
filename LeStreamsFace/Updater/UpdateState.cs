namespace LeStreamsFace.Updater
{
    public enum UpdateState
    {
        Unchecked,
        Checking,
        UpToDate,
        UpdatePending,
        Error,
        Downloading
    }
}