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
    private static readonly string LogFilePath = Path.Combine(Path.GetTempPath(), "ImgViewer_single_instance.log");
    private Mutex? _mutex;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private readonly ManualResetEventSlim _listenerReady = new(false);

    public SingleInstanceCoordinator(string identifier)
    {
        _mutexName = $"Local\\{identifier}_Mutex";
        _pipeName = $"{identifier}_Pipe_{Environment.UserName}";
        ClearLog();
        Log($"Coordinator initialized. Mutex={_mutexName}, Pipe={_pipeName}");
    }

    public async Task<bool> TryStartAsync(string[] argsToForward, Func<string[], Task> onArgumentsReceived)
    {
        bool createdNew;
        Mutex mutex;
        try
        {
            mutex = new Mutex(true, _mutexName, out createdNew);
        }
        catch (Exception ex)
        {
            Log($"Mutex creation failed: {ex.Message}");
            return true;
        }

        if (createdNew)
        {
            Log("Primary instance: Mutex acquired.");
            _mutex = mutex;
            _cts = new CancellationTokenSource();
            _listenerTask = ListenAsync(onArgumentsReceived, _cts.Token);
            
            // パイプリスナーが準備完了するまで待機（最大2秒）
            _listenerReady.Wait(2000);
            Log("Primary instance: Listener ready.");
            return true;
        }

        // セカンダリインスタンス
        mutex.Dispose();
        Log("Secondary instance detected.");
        
        if (argsToForward is { Length: > 0 })
        {
            await NotifyPrimaryAsync(argsToForward);
        }
        
        return false;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listenerReady.Set();
        
        try
        {
            _listenerTask?.Wait(1000);
        }
        catch
        {
        }

        _listenerReady.Dispose();
        _cts?.Dispose();
        
        if (_mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
            }
            _mutex.Dispose();
        }
    }

    private async Task ListenAsync(Func<string[], Task> handler, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                // パイプ作成後、準備完了をシグナル
                _listenerReady.Set();

                await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                Log("Primary: Client connected.");
                
                var args = await ReadArgumentsAsync(server, token).ConfigureAwait(false);
                Log($"Primary: Received {args.Length} argument(s).");
                
                if (args.Length > 0)
                {
                    await handler(args).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Log("Listener cancelled.");
                break;
            }
            catch (Exception ex)
            {
                Log($"Listener error: {ex.Message}");
                await Task.Delay(100, token).ConfigureAwait(false);
            }
            finally
            {
                server?.Dispose();
            }
        }
    }

    private static async Task<string[]> ReadArgumentsAsync(NamedPipeServerStream server, CancellationToken token)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            using var ms = new MemoryStream();
            do
            {
                var bytesRead = await server.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
                if (bytesRead == 0) break;
                ms.Write(buffer, 0, bytesRead);
            }
            while (!server.IsMessageComplete);

            if (ms.Length == 0) return Array.Empty<string>();

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
        var payload = JsonSerializer.SerializeToUtf8Bytes(args);

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
                await client.ConnectAsync(1000).ConfigureAwait(false);
                await client.WriteAsync(payload.AsMemory()).ConfigureAwait(false);
                await client.FlushAsync().ConfigureAwait(false);
                Log($"Arguments forwarded on attempt {attempt}.");
                return;
            }
            catch (TimeoutException)
            {
                Log($"Attempt {attempt}: Connection timeout.");
            }
            catch (Exception ex)
            {
                Log($"Attempt {attempt} failed: {ex.Message}");
            }

            await Task.Delay(100 * attempt).ConfigureAwait(false);
        }

        Log("Failed to forward arguments after all attempts.");
    }

    private static void ClearLog()
    {
        try { File.WriteAllText(LogFilePath, string.Empty); } catch { }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                LogFilePath,
                $"{DateTime.Now:HH:mm:ss.fff} [{Environment.ProcessId}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
