using System.Net;
using System.Security.Cryptography.X509Certificates;

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
