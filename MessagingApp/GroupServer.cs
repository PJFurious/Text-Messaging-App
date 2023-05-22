using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GServer
{
    public class GroupServer
    {
        private TcpListener listener;
        private TcpClient[] clients;
        private Thread[] clientThreads;
        private bool isRunning;
        private int clientCount;

        public GroupServer()
        {
            clients = new TcpClient[100]; // Maximum 100 clients
            clientThreads = new Thread[100]; // Maximum 100 threads
            isRunning = true;
            clientCount = 0;
        }

        public void Start(int port)
        {
            try
            {
                // Set up the server socket
                IPAddress ipAddress = IPAddress.Any;
                listener = new TcpListener(ipAddress, port);

                // Start listening for incoming connections
                listener.Start();
                Console.WriteLine("Server started on port {0}", port);

                while (isRunning)
                {
                    // Accept a new client connection
                    TcpClient client = listener.AcceptTcpClient();
                    clients[clientCount] = client;
                    Console.WriteLine("Client connected: {0}", client.Client.RemoteEndPoint);

                    // Create a new thread to handle communication with the client
                    clientThreads[clientCount] = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThreads[clientCount].Start(client);
                    clientCount++;

                    // Send the message to all other clients that x joined
                    for (int i = 0; i < clientCount; i++)
                    {
                        NetworkStream joinedStream = clients[i].GetStream();
                        byte[] data = Encoding.ASCII.GetBytes($"({client.Client.RemoteEndPoint.ToString()})#Joined the chat!");
                        joinedStream.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                isRunning = false;

                // Close all client connections and threads
                for (int i = 0; i < clientCount; i++)
                {
                    clients[i].Close();
                    clientThreads[i].Join();
                }

                // Stop the server socket
                listener.Stop();
                Console.WriteLine("Server stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void HandleClient(object obj)
        {
            TcpClient client = obj as TcpClient;
            NetworkStream stream = client.GetStream();

            try
            {
                while (isRunning)
                {
                    // Receive a message from the client
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    string[] items = message.Split(new[] { '#' }, 2);
                    string username = items[0];
                    message = items[1];
                    // Console.WriteLine("Message received from client {0}: {1}", client.Client.RemoteEndPoint, message);

                    if (bytesRead == 0)
                        break;

                    // Send the message to all other clients
                    for (int i = 0; i < clientCount; i++)
                    {
                        if (clients[i] != client)
                        {
                            NetworkStream otherStream = clients[i].GetStream();
                            byte[] data = Encoding.ASCII.GetBytes($"{username}#{message}");
                            otherStream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client {0}: {1}", client.Client.RemoteEndPoint, ex.Message);
            }
            finally
            {
                // Cleanup when the client disconnects
                for (int i = 0; i < clientCount; i++)
                {
                    if (clients[i] == client)
                    {
                        for (int j = i; j < clientCount - 1; j++)
                        {
                            clients[j] = clients[j + 1];
                            clientThreads[j] = clientThreads[j + 1];
                        }
                        clientCount--;
                        break;
                    }
                }

                try
                {
                    for (int i = 0; i < clientCount; i++)
                    {
                        NetworkStream joinedStream = clients[i].GetStream();
                        byte[] data = Encoding.ASCII.GetBytes($"({client.Client.RemoteEndPoint})#Left the chat!");
                        joinedStream.Write(data, 0, data.Length);
                    }
                    Console.WriteLine("Client disconnected: {0}", client.Client.RemoteEndPoint);
                }
                catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}");  }
            }
        }

        /*static void Main()
        {
            int port = 23223; // Replace with the desired port number
            GroupServer server = new GroupServer();
            server.Start(port);

            // Keep the server running until a key is pressed
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.Stop();
        }*/
    }
}
