using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Diagnostics;


public class proxanne
{
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

                _ = handleClientAsync(clientSocket);
            }
        }
        public async Task handleClientAsync(TcpClient clientSocket)
        {
            TcpClient remoteServer = new TcpClient();
            IPEndPoint remoteEndPoint;
            IPAddress[] addresses;
            StreamWriter writer;
            NetworkStream clientStream, remoteStream;
            StreamReader reader;
            StreamWriter remoteWriter;
            string requestLine = string.Empty, remainingRequest = string.Empty, line = string.Empty, remoteDomainName = string.Empty;
            Ping pingToServer;
            PingReply replyFromServer;
            byte[] requestBytes;
            Task clientToServer, serverToClient;

            using (clientSocket)
            {
                using (remoteServer)
                {
                    using (writer = new StreamWriter("TCPProxy.log", append: true))
                    {
                        try
                        {
                            clientStream = clientSocket.GetStream();
                            reader = new StreamReader(clientStream);

                            requestLine = await reader.ReadLineAsync();
                            remainingRequest = requestLine + "\r\n";

                            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                            {
                                remainingRequest += line + "\r\n";
                            }

                            remainingRequest += "\r\n";

                            remoteDomainName = remainingRequest.Split("Host: ")[1];

                            if (remoteDomainName.Contains(":"))// Sometimes comes with port
                            {
                                remoteDomainName = remoteDomainName.Split(":")[0];
                            }

                            writer.WriteLine("\n\nHost: " + remoteDomainName);

                            if (remoteDomainName.Contains("\r") || remoteDomainName.Contains("\n"))
                            {
                                remoteDomainName = remoteDomainName.Replace("\r", "").Replace("\n", "");
                            }

                            addresses = Dns.GetHostAddresses(remoteDomainName);

                            if (remainingRequest.Contains("www.google.com"))
                            {
                                remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8000);
                                writer.WriteLine("Google catched : forward to localhost:8000");
                            }
                            else
                            {
                                remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7000);
                            }

                            if(remoteDomainName != "localhost")
                            {
                                pingToServer = new Ping();
                                writer.WriteLine("Ping to: " + addresses[0]);
                                replyFromServer = pingToServer.Send(addresses[0]);

                                if (replyFromServer.Status == IPStatus.Success)
                                {
                                    if (!(replyFromServer.Options.Ttl > 128 || replyFromServer.Options.Ttl < 64))
                                    {
                                        writer.WriteLine(remoteDomainName + " is a windows server");
                                        writer.WriteLine("Proxy close the session");
                                        return;
                                    }
                                    else
                                    {
                                        writer.WriteLine(remoteDomainName + " is a Non-windows server");
                                    }
                                }
                            }
                             
                            await remoteServer.ConnectAsync(remoteEndPoint);
                            remoteStream = remoteServer.GetStream();
                            remoteWriter = new StreamWriter(remoteStream);
                            requestBytes = System.Text.Encoding.ASCII.GetBytes(remainingRequest);
                            await remoteStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                            clientToServer = clientStream.CopyToAsync(remoteStream);
                            serverToClient = remoteStream.CopyToAsync(clientStream);
                            await Task.WhenAny(clientToServer, serverToClient);
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine("Error handling client" + ex.ToString());
                        }
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