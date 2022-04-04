namespace InstanceThreadPool;

public class InstanceThreadPool
{
    private readonly ThreadPriority _priority;
    private readonly string? _name;
    private readonly Thread[] _threads;
    public readonly AutoResetEvent _workingEvent = new(false);

    public InstanceThreadPool(int maxThreadsCount, ThreadPriority priority = ThreadPriority.Normal, string? name =null)
    {
        if (maxThreadsCount <= 0) 
            throw new ArgumentOutOfRangeException(nameof(maxThreadsCount), 
                                                  maxThreadsCount, "Count of pool must be more than 1 or equals.");

        _priority = priority;
        _name = name;
        _threads = new Thread[maxThreadsCount];
    }

    private void Initialize()
    {
        for (var i = 0; i < _threads.Length; i++)
        {
            var name = $"{nameof(InstanceThreadPool)}[{_name??GetHashCode().ToString("x")}]-Thread{i}";
            
            var thread = new Thread(WorkingThread)
            {
                Name = name,
                IsBackground = true,
                Priority = _priority,
            };
            _threads[i] = thread;
            thread.Start();
        }
    }

    public void Execute(Action work) => Execute(null, _=>work());

    private void Execute(object? Parameter, Action<object?> work)
    {
        
    }
    
    private void WorkingThread()
    {
        _workingEvent.WaitOne();
    }
}