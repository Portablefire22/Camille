// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;


class Camille
{
    static void Main(string[] args)
    {
        X509Certificate2 certificate = new X509Certificate2(File.ReadAllBytes("camille.pfx"), "wasd");
        var ipEndPoint = new IPEndPoint(IPAddress.Any, 5223);
        TcpListener listener = new(ipEndPoint);
        try
        {
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();

            SslStream ns = new SslStream(client.GetStream());
            ns.AuthenticateAsServer(certificate, false, SslProtocols.Tls, false);

            while (client.Connected)
            {
                byte[] msg = new byte[1024];
                ns.Read(msg, 0, msg.Length);
                Console.WriteLine(Encoding.Default.GetString(msg).Trim(' '));
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}