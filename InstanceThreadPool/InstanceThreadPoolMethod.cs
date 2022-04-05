using System.Diagnostics;

namespace InstanceThreadPool;

public class InstanceThreadPoolMethod : IDisposable
{
    private readonly ThreadPriority _priority;
    private readonly string? _name;
    private readonly Thread[] _threads;
    private readonly Queue<(Action<object?> work, object? Parameter)> _works = new();
    private readonly AutoResetEvent _workingEvent = new(false);
    private readonly AutoResetEvent _executeEvent = new(true);
    private const int DisposeThreadJoinTimeout = 1000;
    private volatile bool _canWork = true;
    public InstanceThreadPoolMethod(int maxThreadsCount, ThreadPriority priority = ThreadPriority.Normal,
        string? name = null)
    {
        if (maxThreadsCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxThreadsCount),
                                                  maxThreadsCount, "Count of pool must be more than 1 or equals.");

        _priority = priority;
        _name = name;
        _threads = new Thread[maxThreadsCount];
        Initialize();
    }

    private void Initialize()
    {
        for (var i = 0; i < _threads.Length; i++)
        {
            var name = $"{nameof(InstanceThreadPoolMethod)}[{_name ?? GetHashCode().ToString("x")}]-Thread{i}";

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

    public void Execute(Action work)
    {
        Execute(null, _ => work());
    }

    public void Execute(object parameter, Action<object> work)
    {
        if (_canWork) throw new InvalidOperationException("Trying to pass a task to pull of thread");
        
        // asking an access to the queue
        _executeEvent.WaitOne();
        if (_canWork) throw new InvalidOperationException("Trying to pass a task to pull of thread");
        _works.Enqueue((work, parameter));
        // allow an access to the queue 
        _executeEvent.Set();

        _workingEvent.Set();
    }

    private void WorkingThread()
    {
        var threadName = Thread.CurrentThread.Name;
        Trace.TraceInformation($"Thread {threadName} with id :{Environment.CurrentManagedThreadId} was started");
        try
        {
            while (_canWork)
            {
                // asking an access to the queue
                _workingEvent.WaitOne();
                if (!_canWork) break;
                _executeEvent.WaitOne();

                // if the queue hasn't a task
                while (_works.Count == 0)
                {
                    // clear the queue
                    _executeEvent.Set();
                    // waiting for allow to execute
                    _workingEvent.WaitOne();
                    if (!_canWork) break;
                    // asking an access to the queue
                    _executeEvent.WaitOne();
                }

                // get tasks from the queue
                var (work, parameter) = _works.Dequeue();
                // If something remain it will start thread again 
                if (_works.Count > 0)
                    _workingEvent.Set();


                _executeEvent.Set(); // 
                Trace.TraceInformation($"Thread {threadName} with id :{Environment.CurrentManagedThreadId} is doing a task ");
                try
                {
                    var timer = Stopwatch.StartNew();
                    work(parameter);
                    Trace.TraceInformation($"Thread {threadName} with id :{Environment.CurrentManagedThreadId} has done the task. " +
                                           $"Done it in {timer.ElapsedMilliseconds}mc");
                }
                catch (Exception e)
                {
                    Trace.TraceError("Error executing tasks in thread {0}:{1}", threadName, e);
                }
            }
        }
        catch (ThreadInterruptedException e)
        {
            Trace.TraceWarning($"Thread {threadName} was interrupted.");
        }
        finally
        {
            Trace.TraceInformation($"Thread {threadName} was finished.");
            _workingEvent.Set();
        }
    }

 
    public void Dispose()
    {
        _canWork = false;
        _workingEvent.Set();
        
        foreach (var thread in _threads) 
            if (!thread.Join(DisposeThreadJoinTimeout)) 
                thread.Interrupt();
        
        _executeEvent.Dispose();
       _workingEvent.Dispose();
    }
}