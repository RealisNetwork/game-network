namespace PdNetwork.LagMonitor
{
    public interface IPingMonitor
    {
        void Start();

        void Stop();

        void Destroy();

        bool IsRunning { get; }

        long AverageRoundPing { get; }
    }
}