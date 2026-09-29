using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TicketApi.Tests.Support;

public sealed class FakeSmtpServer : IDisposable
{
    private readonly TcpListener listener;
    private readonly List<string> messages = [];
    private readonly Task acceptLoop;
    private readonly CancellationTokenSource stopping = new();

    public FakeSmtpServer()
    {
        listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        acceptLoop = Task.Run(AcceptUntilStoppedAsync);
    }

    public int Port { get; }

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (messages)
                return messages.ToArray();
        }
    }

    private async Task AcceptUntilStoppedAsync()
    {
        while (!stopping.IsCancellationRequested)
        {
            TcpClient connection;
            try
            {
                connection = await listener.AcceptTcpClientAsync(stopping.Token);
            }
            catch (Exception)
            {
                return;
            }

            using (connection)
            {
                try
                {
                    await ConverseAsync(connection);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    private async Task ConverseAsync(TcpClient connection)
    {
        using var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        using var writer = new StreamWriter(stream, Encoding.ASCII)
        {
            AutoFlush = true,
            NewLine = "\r\n",
        };

        await writer.WriteLineAsync("220 fake-smtp ready");

        while (await reader.ReadLineAsync() is { } command)
        {
            if (command.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("250-fake-smtp");
                await writer.WriteLineAsync("250 AUTH PLAIN LOGIN");
            }
            else if (command.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("235 authenticated");
            else if (command.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 send data");
                await CollectMessageAsync(reader);
                await writer.WriteLineAsync("250 queued");
            }
            else if (command.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 bye");
                return;
            }
            else
                await writer.WriteLineAsync("250 ok");
        }
    }

    private async Task CollectMessageAsync(StreamReader reader)
    {
        var message = new StringBuilder();
        while (await reader.ReadLineAsync() is { } line && line != ".")
            message.AppendLine(line);

        lock (messages)
            messages.Add(message.ToString());
    }

    public void Dispose()
    {
        stopping.Cancel();
        listener.Dispose();
        try
        {
            acceptLoop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
        stopping.Dispose();
    }
}
