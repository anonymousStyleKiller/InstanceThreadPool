using InstanceThreadPool;

var messages = Enumerable.Range(1, 100).Select(i=>$"Message - {i}");

var theadPool = new InstanceThreadPoolMethod(10);


foreach (var message in messages)
    theadPool.Execute(message, obj =>
    {
        var msg = (string)obj!;
        Console.WriteLine($">> Processing of messages {msg} has started...");
        Thread.Sleep(100);
        Console.WriteLine($">>  Processing of messages {msg} has finished!");
    });

Console.ReadLine();
