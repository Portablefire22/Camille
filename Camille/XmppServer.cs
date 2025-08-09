using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace Camille;

public class XmppServer
{
    private TcpListener _listener;
    private bool _isSSL = false;
    private X509Certificate2 _certificate;
    private List<XmppClient> _clients;
    private bool _isRunning;

    public XmppServer(IPEndPoint serverEndPoint)
    {
        _listener = new TcpListener(serverEndPoint);

        _clients = new List<XmppClient>();
    }

    public XmppServer(IPEndPoint serverEndPoint, X509Certificate2 certificate)
    {
        _listener = new TcpListener(serverEndPoint);
        _certificate = certificate;
        _isSSL = true;
        
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
            Console.WriteLine("Accepting Client");
            TcpListener listener = asyncResult.AsyncState as TcpListener;
            if (listener == null)
            {
                return;
            }

            TcpClient client = listener.EndAcceptTcpClient(asyncResult);
            Stream stream = client.GetStream();
            if (_isSSL && _certificate != null)
            {
                stream = new SslStream(stream);
                await ((SslStream)stream).AuthenticateAsServerAsync(_certificate);
            }

            var xmpclient = new XmppClient(client, stream);
            xmpclient.SetDisconnectCallback(RemoveClient);
            _clients.Add(xmpclient);
            Console.WriteLine("Added Client");
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
   bool Handshake(Stream stream, string clientId)
    {
        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment
        };
        using (var reader = XmlReader.Create(stream, settings))
        {
            int i = 0;
            while (reader.Read())
            {
                if (i == 1)
                {
                    if (reader.Name != "stream:stream" || 
                        reader.GetAttribute("xmlns") != "jabber:client" || 
                        reader.GetAttribute("to") != "pvp.net"
                        )
                    {
                        Console.WriteLine("Client did not provide a valid handshake"); 
                        return false;
                    }
                    break;
                }
                i++;
            }
        }

        XmlWriterSettings writerSettings = new XmlWriterSettings();

        using (XmlWriter writer = XmlWriter.Create(stream, writerSettings))
        {
            writer.WriteStartElement("stream:stream");
            writer.WriteAttributeString("from", "pvp.net");
            writer.WriteAttributeString("xmlns", "jabber:client");
            writer.WriteAttributeString("xmlns:stream", "http://etherx.jabber.org/streams");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("id", clientId);
            writer.Close();
        }
        
        Console.WriteLine("Valid handshake"); 
        return true;
    }
    bool RemoveClient(XmppClient client)
    {
        Console.WriteLine("Removing client {0}", client.GetClientId());
        client.Close();
        return _clients.Remove(client);
    }
    
 
}