using GServer;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using PTPServer;
using System.Drawing;

namespace MessagingApp
{
    public partial class MainFrm : Form
    {
        public MainFrm()
        {
            InitializeComponent();
        }

        private bool connected = false;
        private bool gConnected = false;
        private bool ptpConnected = false;
        private TcpClient clientSocket;
        private Thread receiveThread;
        string ptpIP = string.Empty;
        string username = string.Empty;
        string userIP = "";

        public bool Connect(string serverIP, int serverPort)
        {
            try
            {
                if (!connected)
                {
                    // Connect to server
                    clientSocket = new TcpClient();
                    clientSocket.Connect(serverIP, serverPort);
                    connected = true;

                    Console.WriteLine("Connected to server {0}:{1}", serverIP, serverPort);

                    // Run listening/ReceiveMessages in a different thread to prevent GUI freeze
                    receiveThread = new Thread(new ParameterizedThreadStart(ReceiveMessages));
                    receiveThread.Start(this);

                    return true; // Connection successful
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }

            return false;
        }


        public void SendGroupMessage(string message)
        {
            try
            {
                // connect to socket
                NetworkStream networkStream = clientSocket.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(message);

                // send message over socket/networkstream
                networkStream.Write(data, 0, data.Length);
                Console.WriteLine("Message sent: {0}", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public void SendPTPMessage(string recipientId, string message)
        {
            try
            {
                // connect to socket/networkstream
                NetworkStream networkStream = clientSocket.GetStream();
                string fullMessage = recipientId + ": " + message;  // add target ip and message into one string
                byte[] data = Encoding.ASCII.GetBytes(fullMessage);

                // send new string(target ip + message) over socket/network stream
                networkStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void ReceiveMessages(object obj)
        {
            try
            {
                // connect to socket/network stream
                NetworkStream networkStream = clientSocket.GetStream();
                MainFrm p = obj as MainFrm;

                while (connected)
                {
                    // read incoming messages over the networkstream/socket
                    byte[] buffer = new byte[1024];
                    int bytesRead = networkStream.Read(buffer, 0, buffer.Length);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // check to see if it is a group message or a ptp message and format the incomming string accordingly
                    if (ptpConnected)
                    {
                        try
                        {
                            // split sender ip and message to display correctly
                            string[] items = message.Split(new[] { '#' }, 2);
                            string sender = items[0];
                            message = items[1];

                            if (sender != ptpIP && ptpIP != string.Empty)
                            {
                                SendPTPMessage(sender, "Recipient in another chat.");
                            }
                            else if (sender == "Your IP")
                            {
                                string[] ipItems = message.Split(':');
                                string ip = ipItems[0];
                                string port = ipItems[1];
                                userIP = $"{ip}:{port}";
                                p.lblMyIP.BeginInvoke((MethodInvoker)(() => p.lblMyIP.Text = $"Your IP: {ip}"));
                                p.lblMyPort.BeginInvoke((MethodInvoker)(() => p.lblMyPort.Text = $"Your Port: {port}"));
                            }
                            else
                            {
                                string[] itemsA = message.Split(new[] { '#' }, 2);
                                string senderUName = itemsA[0];
                                message = itemsA[1];
                                p.rtbOutput.BeginInvoke((MethodInvoker)(() => p.rtbOutput.AppendText($"{senderUName.Trim()}: {message.Trim()}\n")));
                            }
                        }
                        catch 
                        { 
                            p.rtbOutput.BeginInvoke((MethodInvoker)(() =>
                            {
                                p.rtbOutput.SelectionStart = p.rtbOutput.TextLength;
                                p.rtbOutput.SelectionAlignment = HorizontalAlignment.Center;
                                p.rtbOutput.AppendText($"{message}\n");
                                p.rtbOutput.SelectionAlignment = HorizontalAlignment.Left; // Reset to default alignment
                            }));

                        }
                        rtbOutput.ScrollToCaret(); ;
                    }
                    else if (gConnected)
                    {
                        // split sender ip and message to display correctly
                        string[] items = message.Split(new[] { '#' }, 2);
                        string sender = items[0];
                        message = items[1];
                        p.rtbOutput.BeginInvoke((MethodInvoker)(() => 
                        {
                            p.rtbOutput.SelectionStart = p.rtbOutput.TextLength;
                            if (sender[0] == '(') p.rtbOutput.SelectionAlignment = HorizontalAlignment.Center;
                            p.rtbOutput.AppendText($"{sender.Trim()}: {message.Trim()}\n");
                            p.rtbOutput.SelectionAlignment = HorizontalAlignment.Left;

                        }));
                        rtbOutput.ScrollToCaret();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }


        public void Disconnect()
        {
            if (connected)
            {
                try
                {
                    // disconnect from sockets, join running thread and change needed boolean values
                    clientSocket.GetStream().Close();
                    clientSocket.Close();
                    clientSocket = null;
                    receiveThread.Join();
                    connected = false;
                    ptpConnected = false;
                    gConnected = false;

                    Console.WriteLine("Disconnected from the server");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            // check to see if client is connected to a server before trying to send a message.
            string message = textBox1.Text.Trim();
            if (message != string.Empty)
            {
                if (ptpConnected)
                {
                    if (userIP != ptpIP)
                    {
                        SendPTPMessage(ptpIP, $"{username}#{message}");

                        rtbOutput.SelectionStart = rtbOutput.TextLength;
                        rtbOutput.SelectionLength = 0;

                        rtbOutput.SelectionColor = Color.LightSkyBlue;
                        rtbOutput.SelectedText = $"You: {message}\n";
                        rtbOutput.SelectionColor = rtbOutput.ForeColor;
                    }
                    else MessageBox.Show("Don't do that. Why do you want to message yourself. I'm sorry you dont have friends.");
                    
                }
                else
                {
                    SendGroupMessage($"{username}#{message}");

                    rtbOutput.SelectionStart = rtbOutput.TextLength;
                    rtbOutput.SelectionLength = 0;

                    rtbOutput.SelectionColor = Color.LightSkyBlue;
                    rtbOutput.SelectedText = $"You: {message}\n";
                    rtbOutput.SelectionColor = rtbOutput.ForeColor;
                }
                textBox1.Clear();
                textBox1.Focus();
                rtbOutput.ScrollToCaret();

            }
        }

        private void GetUsername(string filename)
        {
            while (username.Trim() == string.Empty || username.Length > 15)
            {
                if (username.Length > 15) MessageBox.Show("Max username length is 15 characters");
                GetUserName frm = new GetUserName();
                frm.ShowDialog();

                username = frm.username;

                using (StreamWriter writer = new StreamWriter(filename, false))
                {
                    writer.WriteLine(username);
                }
            }
        }

        private void MainFrm_Load(object sender, EventArgs e)
        {
            string fileName = "Username.txt";

            if (File.Exists(fileName))
            {
                using (StreamReader reader = new StreamReader(fileName))
                    username = reader.ReadLine();
                GetUsername(fileName);
            }
            else
            {
                GetUsername(fileName);
            }

            lblName.Text = username;
            lblMyIP.Text = string.Empty;
            lblMyPort.Text = string.Empty;

            // just to test servers locally
            GroupServer server = new GroupServer();
            Thread thread = new Thread(() => server.Start(23223));
            //thread.Start();

            PeerToPeerServer ptpserver = new PeerToPeerServer();
            Thread ptpthread = new Thread(() => ptpserver.Start(32332));
            //ptpthread.Start();
        }

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
        }

        private void btnConnectGroup_Click(object sender, EventArgs e)
        {
            if (!gConnected)
            {
                if (ptpConnected)
                {
                    Disconnect();
                    ptpConnected = false;
                    ptpIP = string.Empty;
                    lblMyIP.Visible = false;
                    lblMyPort.Visible = false;
                }

                // connect to global/group chat
                //34.145.222.43
                rtbOutput.Clear();
                if (Connect("34.145.222.43", 23223))
                {
                    gConnected = true;
                    btnSend.Enabled = true;
                }
                else MessageBox.Show("Global chat server offline");
            }            
        }

        private void btnPTPConnect_Click(object sender, EventArgs e)
        {
            if (!ptpConnected)
            {
                if (gConnected)
                {
                    Disconnect();
                    gConnected = false;
                }

                rtbOutput.Clear();
                // connect to ptp server/chat

                if (Connect("35.194.95.129", 32332))
                {
                    ptpConnected = true;
                    lblMyIP.Visible = true;
                    lblMyPort.Visible = true;
                    btnSend.Enabled = false;
                    txtIP.Focus();
                }
                else MessageBox.Show("Peer to peer server offline");
            }
        }

        private void btnStartChat_Click(object sender, EventArgs e)
        {
            if (ptpConnected)
                if (txtIP.Text.Trim() != string.Empty)
                    if (txtPort.Text.Trim() != string.Empty)
                    {
                        if (ptpIP != txtIP.Text + ":" + txtPort.Text)
                        {
                            rtbOutput.Clear();
                            ptpIP = txtIP.Text + ":" + txtPort.Text;
                            btnSend.Enabled = true;

                            SendPTPMessage(ptpIP, $"{username} Connected");

                            rtbOutput.SelectionStart = rtbOutput.TextLength;
                            rtbOutput.SelectionAlignment = HorizontalAlignment.Center;
                            rtbOutput.AppendText($"Connected to { ptpIP}\n");
                            rtbOutput.SelectionAlignment = HorizontalAlignment.Left;

                            rtbOutput.ScrollToCaret();
                        }
                    }
                    else MessageBox.Show("Enter a Port.");
                else MessageBox.Show("Enter an IP.");
            else MessageBox.Show("Please connect to PTP server first.");
        }
    }
}
