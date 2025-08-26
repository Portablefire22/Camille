using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Camille;

public class XmppServer
{
    private readonly TcpListener _listener;
    private bool _isSsl;
    private readonly X509Certificate2? _certificate;
    private readonly List<XmppClient> _clients;
    private bool _isRunning;

    private readonly ILogger _logger;

    public static IConfigurationRoot ConfigurationRoot;
    
    public XmppServer(IPEndPoint serverEndPoint)
    {
        _listener = new TcpListener(serverEndPoint);

        var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", false, true);
        ConfigurationRoot = builder.Build();
        
        using ILoggerFactory factory = LoggerFactory.Create(build => build.AddConsole());
        _logger = factory.CreateLogger("Camille");
        
        _clients = new List<XmppClient>();
    }

    public XmppServer(IPEndPoint serverEndPoint, X509Certificate2 certificate)
    {
        _listener = new TcpListener(serverEndPoint);
        _certificate = certificate;
        _isSsl = true;
        
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
        ConfigurationRoot = builder.Build();
        
        using ILoggerFactory factory = LoggerFactory.Create(build => build.AddConsole());
        _logger = factory.CreateLogger("Camille");
        
        _clients = new List<XmppClient>();
    }
    
    public void Listen()
    {
        _listener.Start();
        _isRunning = true;
        _listener.BeginAcceptTcpClient(OnClientAccepted, _listener);
    }

    async void OnClientAccepted(IAsyncResult asyncResult)
    {
        try
        {
            var listener = asyncResult.AsyncState as TcpListener;
            if (listener == null)
            {
                return;
            }

            var client = listener.EndAcceptTcpClient(asyncResult);

            var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            _logger.LogInformation("Accepting Client {addr}", remoteEndPoint.Address);
            
            Stream stream = client.GetStream();
            if (_isSsl && _certificate != null)
            {
                stream = new SslStream(stream);
                await ((SslStream)stream).AuthenticateAsServerAsync(_certificate);
            }

            var xmpclient = new XmppClient(client, stream);
            xmpclient.SetDisconnectCallback(RemoveClient);
            _clients.Add(xmpclient);
            _logger.LogInformation("Added Client");
        }
        finally
        {
            // This needs to be done in a finally block to prevent
            // a memory leak from continuously creating async objects
            if (_isRunning)
            {
                _listener.BeginAcceptTcpClient(OnClientAccepted, _listener);
            }
            else
            {
                _listener.Stop();
            }
        }
    }
    private bool RemoveClient(XmppClient client)
    {
        _logger.LogInformation("Removing client {clientId}", client.GetClientId());
        client.Close(true);
        return _clients.Remove(client);
    }
}