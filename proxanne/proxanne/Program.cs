using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


public class proxanne
{
    class TCPProxy
    {

        public async Task start(IPEndPoint proxyEndPoint, IPEndPoint remoteEndPoint)
        {
            TcpListener proxyListener = new TcpListener(IPAddress.Any, proxyEndPoint.Port);
            proxyListener.Start();
            Console.WriteLine($"Proxy listening on port {proxyEndPoint.Port}.");

            while (true)
            {
                TcpClient clientSocket = await proxyListener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected...");

                _ = handleClientAsync(clientSocket, remoteEndPoint);
                
               
            }
        }
        public async Task handleClientAsync(TcpClient clientSocket, IPEndPoint remoteEndPoint)
        {
            TcpClient remoteServer = new TcpClient();
            using (clientSocket)
            {
                using (remoteServer)
                {
                    try
                    {
                        remoteServer.ConnectAsync(remoteEndPoint);
                        Console.WriteLine("Proxy connected to" + remoteEndPoint);

                        NetworkStream clientStream = clientSocket.GetStream();
                        NetworkStream remoteStream = remoteServer.GetStream();
                        
                        Task clientToServer = clientStream.CopyToAsync(remoteStream);
                        Task serverToClient = remoteStream.CopyToAsync(clientStream);

                        await Task.WhenAny(clientToServer, serverToClient); 
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error handling client" + ex.ToString());
                    }
                }
            }
        }
    }
    
    public static async Task Main(String[] args)
    {
        TCPProxy Proxy = new TCPProxy();
        await Proxy.start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7000));

    }
}