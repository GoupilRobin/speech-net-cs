using System;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Text;
using System.Collections.Generic;

namespace Marvin
{
    class Server
    {
        private TcpListener m_Listener;
        private TcpClient m_CurrentClient;
        private SpeechToText m_CurrentSpeechToText;
        private Timer m_IdleTimer;
        private bool m_ConnectionEstablished;

        public Server()
        {
            // Timer to close the process after waiting for a client for too long
            // to avoid zombie process
            m_IdleTimer = new Timer();
            m_IdleTimer.Interval = 10 * 1000; // 10s
            m_IdleTimer.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                Console.WriteLine("Waited for a connection for too long, exiting");
                Environment.Exit(0);
            };

            m_CurrentClient = null;
        }

        private void SpeechRecognized(string speech)
        {
            if (m_CurrentClient == null) return;

            if (!m_CurrentClient.Connected)
            {
                ClientLost();
            }
            else if (m_ConnectionEstablished)
            {
                byte[] payload = Encoding.ASCII.GetBytes(speech);

                if (payload.Length < Configuration.MaxPayloadLength)
                {
                    SocketAsyncEventArgs sendAsyncArg = new SocketAsyncEventArgs();
                    sendAsyncArg.SetBuffer(payload, 0, payload.Length);

                    m_CurrentClient.Client.SendAsync(sendAsyncArg);
                }
                else
                {
                    Console.WriteLine("Payload was too big to send");
                }
            }
        }

        public void Start()
        {
            m_Listener = new TcpListener(IPAddress.Loopback, Configuration.Port);

            try
            {
                m_Listener.Start();
            }
            catch (SocketException)
            {
                Console.WriteLine("Port already used, exiting");
                Environment.Exit(1);
                return;
            }

            OnListenerStarted();
        }

        private void OnListenerStarted()
        {
            m_IdleTimer.Start();

            // Accept loop
            while (true)
            {
                HandleNewClient();
            }
        }

        private void ClientLost()
        {
            m_CurrentClient.Close();
            m_CurrentClient = null;
            m_IdleTimer.Start();
            m_CurrentSpeechToText.Stop();

            Console.WriteLine("Lost connection, exiting");
            Environment.Exit(0);
        }

        private void HandleNewClient()
        {
            TcpClient newClient = m_Listener.AcceptTcpClient();
            if (m_CurrentClient == null && newClient != null)
            {
                m_ConnectionEstablished = false;

                Console.WriteLine("New client connected");
                m_CurrentClient = newClient;
                m_IdleTimer.Stop();

                List<byte> fullPacket = new List<byte>();
                int len = -1;
                do
                {
                    byte[] buffer = new byte[4096];
                    len = m_CurrentClient.Client.Receive(buffer);
                    for (int i = 0; i < len; ++i)
                    {
                        fullPacket.Add(buffer[i]);
                    }
                } while (m_CurrentClient.Client.Available > 0 && len > 0);
                HandshakeReceived(fullPacket.ToArray());
            }
        }

        private void HandshakeReceived(byte[] buffer)
        {
            m_CurrentSpeechToText = new SpeechToText();
            m_CurrentSpeechToText.SpeechRecognized += SpeechRecognized;
            
            HandshakeRequest handshakeRequest = Utils.Deserialize<HandshakeRequest>(buffer);

            List<Phrase> phrases = handshakeRequest.Phrases;
            foreach (Phrase phrase in phrases)
            {
                m_CurrentSpeechToText.AddVocabulary(phrase);
            }

            List<string> sentences = handshakeRequest.Sentences;
            foreach (string sentence in sentences)
            {
                m_CurrentSpeechToText.AddSentence(sentence);
            }

            m_CurrentSpeechToText.Start();
            m_ConnectionEstablished = true;
        }
    }
}
