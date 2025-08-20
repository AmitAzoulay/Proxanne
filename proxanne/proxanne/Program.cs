using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;


public class proxanne
{

    /*
     * using var reader = new StreamReader(clientStream, Encoding.ASCII, leaveOpen: true);
                            string? requestLine = await reader.ReadLineAsync();
                            Console.WriteLine($"Request: {requestLine}");
    */
    class TCPProxy
    {

        public async Task start(IPEndPoint proxyEndPoint)
        {
            TcpListener proxyListener = new TcpListener(IPAddress.Any, proxyEndPoint.Port);
            proxyListener.Start();
            Console.WriteLine($"Proxy listening on port {proxyEndPoint.Port}.");

            while (true)
            {
                TcpClient clientSocket = await proxyListener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected...");

                _ = handleClientAsync(clientSocket);

            }
        }
        public async Task handleClientAsync(TcpClient clientSocket )
        {
            TcpClient remoteServer = new TcpClient();
            byte[] requestBuffer = new byte[4096];
            int totalRequestBuffered = 0, bytesRead = 0;
            string remotePort = "", remoteDomainName = "", requestText = "";
            IPAddress[] addresses;
            IPEndPoint remoteEndPoint;

            using (clientSocket)
            {
                var destination = clientSocket.Client.RemoteEndPoint?.ToString();

                using (remoteServer)
                {
                    try
                    {
                        NetworkStream clientStream = clientSocket.GetStream();
                        MemoryStream copyClientStream = new MemoryStream();

                       
                        while ((bytesRead = await clientStream.ReadAsync(requestBuffer, 0, requestBuffer.Length)) > 0)
                        {
                            copyClientStream.Write(requestBuffer, 0, bytesRead);
                            totalRequestBuffered += bytesRead;

                            string requestBufferChunk = Encoding.ASCII.GetString(copyClientStream.GetBuffer(), 0, totalRequestBuffered);

                            if(requestBufferChunk.Split("HTTP/")[0].Split("T ")[0] == "GE")
                            {
                                remoteDomainName = requestBufferChunk.Split("Host: ")[1].Split("\n")[0];
                                
                                if (remoteDomainName.Contains(":"))
                                {
                                    remotePort = remoteDomainName.Split(":")[1];
                                }
                                else
                                {
                                    remotePort = "80";
                                }
                                requestText = requestBufferChunk;
                            }
                            else if(requestBufferChunk.Split("HTTP/")[0].Split("T ")[0] == "CONNEC")
                            {
                                remoteDomainName = requestBufferChunk.Split("HTTP/1.1")[0].Split("T ")[1].Split(":")[0];
                                remotePort = requestBufferChunk.Split("HTTP/1.1")[0].Split("T ")[1].Split(":")[1];
                                requestText = requestBufferChunk;
                            }
                            break;
                        }
                        copyClientStream.Position = 0;
               
                        addresses = Dns.GetHostAddresses(remoteDomainName);
                        
                        byte[] responseBytes = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection Established\r\n\r\n");
                        await clientStream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        await clientStream.FlushAsync();
                        remoteEndPoint = new IPEndPoint(addresses[0], Int32.Parse(remotePort));

                        await remoteServer.ConnectAsync(remoteEndPoint);
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
        await Proxy.start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000));

    }
}
     


  


        

     
 

