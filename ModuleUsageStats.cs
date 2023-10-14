using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BBRAPIModules;

namespace DevMinersBBModules;

/// Uploads the currently loaded module list to a <see cref="https://github.com/TheDevMinerTV/bb-telemetry-api/pkgs/container/bb-telemetry-api">telemetry server</see>.
/// 
/// Developer contact:
///   Email: devminer@devminer.xyz
///   Discord: @anna_devminer
[Module("Uploads the currently loaded module list to a telemetry server.", "2.0.0")]
public class ModuleUsageStats : BattleBitModule
{
    private static Client? _client;

    // "Official" server, operated by @anna_devminer
    private const string Endpoint = "raw.devminer.xyz:65502";

    public override void OnModuleUnloading()
    {
        if (_client is null) return;

        _client.Stop();
        _client = null;
    }

    private class AppSettings
    {
        public string? ModulesPath { get; set; }
        public List<string>? Modules { get; set; }
    }

    private static IEnumerable<FileInfo> GetModuleFilesFromFolder(DirectoryInfo directory) =>
        directory.GetFiles("*.cs", SearchOption.TopDirectoryOnly).ToList();

    private static IEnumerable<FileInfo> GetModuleFiles()
    {
        var moduleFiles = new List<FileInfo>();
        var appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText("appsettings.json"));

        if (appSettings?.ModulesPath != null)
            moduleFiles.AddRange(GetModuleFilesFromFolder(new DirectoryInfo(appSettings.ModulesPath)));

        if (appSettings?.Modules == null) return moduleFiles;

        moduleFiles.AddRange(appSettings.Modules.Select(module => new FileInfo(module)).Where(file => file.Exists));

        return moduleFiles;
    }

    private static string? GetVersionFromFile(FileSystemInfo file)
    {
        var text = File.ReadAllText(file.FullName);
        var regex = new Regex(@"\[Module\("".*"", ""(.*)""\)\]");
        var matches = regex.Matches(text);

        foreach (Match match in matches) return match.Groups[1].Value;

        return null;
    }

    private static string GetHashFromFile(FileInfo file)
    {
        using var md5 = MD5.Create();
        using var stream = file.OpenRead();

        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static List<ModuleInfo> GetModuleInfoFromFiles(IEnumerable<FileInfo> files) => (from file in files
        where file.Extension.ToLowerInvariant() == ".cs"
        select new ModuleInfo(name: Path.GetFileNameWithoutExtension(file.Name),
            version: GetVersionFromFile(file) ?? "Unknown", hash: GetHashFromFile(file))).ToList();

    private static void Initialize() {
        if (_client is not null) return;

        var uri = new Uri("tcp://" + Endpoint);
        Utils.Log("Getting list of installed modules");
        var modules = GetModuleInfoFromFiles(GetModuleFiles());

        Utils.Log($"Got list of {modules.Count} installed modules");
        _client = new Client(uri, modules);
        _client.Start();
    }

    public ModuleUsageStats() => Initialize();
    public override void OnModulesLoaded() => Initialize();
    public override Task OnConnected() {
        Initialize();
        return Task.CompletedTask;
    }
}

#region networking

internal class Client
{
    private TcpClient? _socket;
    private readonly Uri _uri;
    private readonly List<ModuleInfo> _modules;
    private CancellationTokenSource? _connectionCancellation;

    private delegate Task DisconnectHandler();

    private event DisconnectHandler? Disconnected;

    public Client(Uri uri, List<ModuleInfo> modules)
    {
        _uri = uri;
        _modules = modules;
    }

    public Task Start()
    {
        Disconnected += () =>
        {
            var rand = new Random();
            var delay = rand.Next(5000, 10000);

            Task.Delay(delay).Wait();

            return _init();
        };

        return _init();
    }

    private async Task _init()
    {
        _connectionCancellation = new CancellationTokenSource();
        _connectionCancellation.Token.Register(OnSocketDisconnectedCleanup);

        _socket = new TcpClient();

        try
        {
            await _socket.ConnectAsync(_uri.Host, _uri.Port);
        }
        catch (SocketException e)
        {
            _connectionCancellation.Cancel();
            return;
        }

        await SendPacket(new HandshakeRequestPacket(_modules));

        Task.Run(ReadLoop, _connectionCancellation.Token);
    }

    private void OnSocketDisconnectedCleanup()
    {
        if (_socket is null) return;

        _socket?.Dispose();
        _socket = null;

        Disconnected?.Invoke().Wait();
    }

    public void Stop() => _connectionCancellation?.Cancel();

    private async Task SendPacket(IPacket packet)
    {
        if (_connectionCancellation is null || _connectionCancellation.IsCancellationRequested || _socket is null)
            return;

        var s = _socket.GetStream();
        var p = new WrappedPacket(packet);
        await s.WriteAsync(p.Encode(), _connectionCancellation.Token);
    }

    private async Task ReadLoop()
    {
        var buffer = new byte[4096];

        while (true)
        {
            if (_connectionCancellation is null || _connectionCancellation.IsCancellationRequested || _socket is null)
                return;

            var s = _socket.GetStream();
            var n = await s.ReadAsync(buffer, 0, buffer.Length, _connectionCancellation.Token);
            if (n <= 0)
            {
                _connectionCancellation.Cancel();
                return;
            }

            if (n < WrappedPacket.DataLengthSize) continue;

            var rawPacket = new byte[n];
            Array.Copy(buffer, rawPacket, n);

            var dataLength = BinaryPrimitives.ReadUInt16BigEndian(rawPacket);
            if (dataLength > WrappedPacket.DataLengthSize + WrappedPacket.PacketTypeLength + n) continue;

            var packetType = (PacketType)buffer[WrappedPacket.DataLengthSize];
            var data = new byte[n - WrappedPacket.DataLengthSize];
            Array.Copy(buffer, WrappedPacket.DataLengthSize, data, 0, data.Length);

            switch (packetType)
            {
                case PacketType.HandshakeResponsePacket:
                {
                    var response = HandshakeResponsePacket.Decode(data);

                    using var h = new HMACSHA256(response.Key);
                    var hash = h.ComputeHash(Encoding.UTF8.GetBytes(string.Join("", _modules)));

                    await SendPacket(new StartRequestPacket(hash));

                    break;
                }

                case PacketType.StartResponsePacket:
                {
                    Task.Run(PingLoop, _connectionCancellation.Token);

                    break;
                }

                case PacketType.HandshakeRequestPacket:
                case PacketType.StartRequestPacket:
                case PacketType.HeartbeatRequestPacket:
                default:
                    // if this happens, then the server fucked up LOL
                    break;
            }
        }
    }

    private async Task PingLoop()
    {
        while (true)
        {
            if (_connectionCancellation is { IsCancellationRequested: true }) return;

            await SendPacket(new HeartbeatRequestPacket());

            await Task.Delay(30 * 1000);
        }
    }
}

internal readonly struct ModuleInfo
{
    private readonly string _name;
    private readonly string _version;
    private readonly string _hash;

    public ModuleInfo(string name, string version, string hash)
    {
        _name = name;
        _version = version;
        _hash = hash;
    }

    public override string ToString() => $"{_name} {_version} {_hash}";

    public int GetEncodedLength() =>
        Utils.EncodedStringLength(_name) +
        Utils.EncodedStringLength(_version) +
        Utils.EncodedStringLength(_hash);

    public byte[] Encode()
    {
        var buf = new byte[GetEncodedLength()];

        var buf2 = Utils.EncodeString(_name);
        buf2.CopyTo(buf, 0);
        var currentPosition = buf2.Length;

        var buf3 = Utils.EncodeString(_version);
        buf3.CopyTo(buf, currentPosition);
        currentPosition += buf3.Length;

        var buf4 = Utils.EncodeString(_hash);
        buf4.CopyTo(buf, currentPosition);

        return buf;
    }
}

internal static class Utils
{
    internal static void Log(object msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]  ModuleUsageStats > {msg}");


    internal static int EncodedStringLength(string s) => 2 + Encoding.UTF8.GetByteCount(s);

    internal static byte[] EncodeString(string s)
    {
        var len = Encoding.UTF8.GetByteCount(s);
        var buf = new byte[2 + len];

        BinaryPrimitives.WriteUInt16BigEndian(buf, (ushort)len);
        Encoding.UTF8.GetBytes(s).CopyTo(buf, 2);

        return buf;
    }
}

#region packets

internal enum PacketType : byte
{
    HandshakeRequestPacket = 1,
    HandshakeResponsePacket = 2,
    StartRequestPacket = 3,
    StartResponsePacket = 4,
    HeartbeatRequestPacket = 5
}

internal interface IPacket
{
    public PacketType Type();
    public byte[] Encode();
}

internal class WrappedPacket
{
    public const int DataLengthSize = 2;
    public const int PacketTypeLength = 1;

    private IPacket Inner { get; }

    public WrappedPacket(IPacket inner) => Inner = inner;

    public byte[] Encode()
    {
        var inner = Inner.Encode();

        var dataLength = inner.Length;
        var length = DataLengthSize + PacketTypeLength + dataLength;

        var buf = new byte[length];

        BinaryPrimitives.WriteUInt16BigEndian(buf, (ushort)dataLength);
        buf[DataLengthSize] = (byte)Inner.Type();
        inner.CopyTo(buf, DataLengthSize + PacketTypeLength);

        return buf;
    }
}

internal class HandshakeRequestPacket : IPacket
{
    private List<ModuleInfo> Modules { get; }

    public HandshakeRequestPacket(List<ModuleInfo> modules) => Modules = modules;
    public PacketType Type() => PacketType.HandshakeRequestPacket;

    public byte[] Encode()
    {
        var moduleCount = Modules.Count;
        var length = 2 + Modules.Sum(module => module.GetEncodedLength());

        var offset = 0;

        var buf = new byte[length];
        BinaryPrimitives.WriteUInt16BigEndian(buf, (ushort)moduleCount);
        offset += 2;

        foreach (var module in Modules)
        {
            var encoded = module.Encode();
            encoded.CopyTo(buf, offset);
            offset += encoded.Length;
        }

        return buf;
    }
}

internal class HandshakeResponsePacket
{
    public byte[] Key { get; }

    private HandshakeResponsePacket(byte[] key) => Key = key;

    public static HandshakeResponsePacket Decode(byte[] buf)
    {
        var key = new byte[32];
        Array.Copy(buf, 1, key, 0, buf.Length - 1);

        return new HandshakeResponsePacket(key);
    }
}

internal class StartRequestPacket : IPacket
{
    private readonly byte[] _hmac;
    public StartRequestPacket(byte[] hmac) => _hmac = hmac;
    public PacketType Type() => PacketType.StartRequestPacket;
    public byte[] Encode() => _hmac;
}

internal class HeartbeatRequestPacket : IPacket
{
    public PacketType Type() => PacketType.HeartbeatRequestPacket;
    public byte[] Encode() => Array.Empty<byte>();
}

#endregion

#endregion
