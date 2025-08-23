// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Camille
{
    class Program
    {
        static void Main(string[] args)
        {
            X509Certificate2 certificate = new X509Certificate2(File.ReadAllBytes("camille.pfx"), "wasd");
            var ipEndPoint = new IPEndPoint(IPAddress.Any, 5223);
            
            XmppServer server = new XmppServer(ipEndPoint);
            server.Listen();

            while (true)
            {
                Thread.Sleep(10000);
            }
        }
    }
}
