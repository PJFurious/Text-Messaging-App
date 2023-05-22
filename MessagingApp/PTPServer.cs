using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PTPServer
{
    public class PeerToPeerServer
    {
        private TcpListener listener;
        private TcpClient[] clients;
        private int clientCount;
        private bool isRunning;

        public PeerToPeerServer()
        {
            clients = new TcpClient[100];
            clientCount = 0;
            isRunning = true;
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
                Console.WriteLine("Server started on port {0}, {1}", ipAddress, port);

                while (isRunning)
                {
                    // Accept a new client connection
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Client connected to the PTP server: {0}", client.Client.RemoteEndPoint);

                    // Add the client to the array
                    lock (clients)
                    {
                        clients[clientCount] = client;
                        clientCount++;
                    }

                    // Create a new thread to handle communication with the client
                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                    clientThread.Start(client);
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
            string clientId = "";
            byte[] data = Encoding.ASCII.GetBytes($"Your IP#{client.Client.RemoteEndPoint}");
            stream.Write(data, 0, data.Length);

            try
            {

                while (isRunning)
                {
                    // Receive the client's identifier
                    clientId = client.Client.RemoteEndPoint.ToString();


                    // Receive a message from the client
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    //Console.WriteLine("Message received from client {0}: {1}", clientId, me>

                    if (bytesRead == 0)
                        break;

                    // Extract the recipient and message content from the received message
                    string[] parts = message.Split(new[] { ':' }, 3);
                    string recipientId = parts[0] +":" + parts[1];
                    string content = parts[2];

                    TcpClient recipient = Array.Find(clients, c => GetClientId(c) == recipientId);

                    if (recipient != null && recipient.Connected)
                    {

                        bool messageSent = SendMessage(recipientId, content, clientId);

                        if (!messageSent)
                        {
                            // Send a response back to the sender indicating failure
                            byte[] response = Encoding.ASCII.GetBytes("Failed to deliver the message.");
                            stream.Write(response, 0, response.Length);
                            Console.WriteLine("Response sent to client {0}: Failed to deliver the message", clientId);
                        }
                    }
                    else
                    {
                        byte[] response = Encoding.ASCII.GetBytes($"Recipient is currently disconnected.");
                        stream.Write(response, 0, response.Length);
                        Console.WriteLine("Recipient client {0} is disconnected. Message not delivered {1}", recipientId, content);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client {0}: {1}", client.Client.RemoteEndPoint.ToString(), ex);
            }
            finally
            {
                // Cleanup when the client disconnects
                lock (clients)
                {
                    // Remove the client from the array
                    for (int i = 0; i < clientCount; i++)
                    {
                        if (clients[i] == client)
                        {
                            for (int j = i; j < clientCount - 1; j++)
                            {
                                clients[j] = clients[j + 1];
                            }
                            clientCount--;
                            break;
                        }
                    }
                }

                try
                {
                    Console.WriteLine("Client disconnected: {0}", client.Client.RemoteEndPoint);
                    client.Close();
                }
                catch (Exception ex) { Console.WriteLine($"Error sending disconnect message: {ex.Message}"); }
            }
        }

        private bool SendMessage(string recipientId, string message, string clientId)
        {
            lock (clients)
            {
                for (int i = 0; i < clientCount; i++)
                {
                    TcpClient recipient = clients[i];
                    string recipientClientId = GetClientId(recipient);

                    if (recipientClientId == recipientId)
                    {
                        if (recipient.Connected) // Check if the recipient client is still connected
                        {
                            NetworkStream stream = recipient.GetStream();
                            byte[] data = Encoding.ASCII.GetBytes($"{clientId}#{message}");
                            stream.Write(data, 0, data.Length);
                            Console.WriteLine("Message sent to client {0}: {1}", recipientId, message);
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("Recipient client {0} is disconnected. Skipping message deliviry.", recipientId);
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        private string GetClientId(TcpClient client)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    string remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                    return remoteEndPoint;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting client ID: " + ex.Message);
            }

            return null;
        }

        /*static void Main()
        {
            int port = 32332; // Replace with the desired port number
            PeerToPeerServer server = new PeerToPeerServer();
            server.Start(port);

            // Keep the server running until a key is pressed
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.Stop();
        }*/

    }
}