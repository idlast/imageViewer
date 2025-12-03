using System;
using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImgViewer.Services;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly string _mutexName;
    private readonly string _pipeName;
    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;

    public SingleInstanceCoordinator(string identifier)
    {
        _mutexName = $"Global\\{identifier}_Mutex";
        _pipeName = $"{identifier}_Pipe";
    }

    public async Task<bool> TryStartAsync(string[] argsToForward, Func<string[], Task> onArgumentsReceived)
    {
        var mutex = new Mutex(true, _mutexName, out var isPrimaryInstance);
        if (!isPrimaryInstance)
        {
            mutex.Dispose();
            await NotifyPrimaryAsync(argsToForward ?? Array.Empty<string>());
            return false;
        }

        _mutex = mutex;
        _cts = new CancellationTokenSource();
        _listenerTask = ListenAsync(onArgumentsReceived, _cts.Token);
        return true;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            _listenerTask?.Wait(1000);
        }
        catch (Exception)
        {
            // 無視して確実に解放
        }

        _cts?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }

    private async Task ListenAsync(Func<string[], Task> handler, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                var args = await ReadArgumentsAsync(server, token).ConfigureAwait(false);
                await handler(args).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 待受を継続
            }
        }
    }

    private static async Task<string[]> ReadArgumentsAsync(NamedPipeServerStream server, CancellationToken token)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            using var ms = new MemoryStream();
            do
            {
                var bytesRead = await server.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                ms.Write(buffer, 0, bytesRead);
            }
            while (!server.IsMessageComplete);

            if (ms.Length == 0)
            {
                return Array.Empty<string>();
            }

            var json = Encoding.UTF8.GetString(ms.ToArray());
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task NotifyPrimaryAsync(string[] args)
    {
        var payload = JsonSerializer.Serialize(args ?? Array.Empty<string>());
        var bytes = Encoding.UTF8.GetBytes(payload);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
                client.Connect(1000);
                await client.WriteAsync(bytes.AsMemory(0, bytes.Length)).ConfigureAwait(false);
                await client.FlushAsync().ConfigureAwait(false);
                return;
            }
            catch
            {
                await Task.Delay(150).ConfigureAwait(false);
            }
        }
    }
}
