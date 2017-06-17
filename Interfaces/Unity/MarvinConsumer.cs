using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Marvin;

public class MarvinConsumer : MonoBehaviour
{
    public delegate void SpeechRecognizedEvent(string result);
    public event SpeechRecognizedEvent SpeechRecognized;

    private TcpClient m_TcpClient;
    private bool m_Connecting;
    private float m_ConnectStartedTime;
    private static readonly float ConnectTimeout = 3; // seconds
    private bool m_ConnectionEstablished;

    void Awake()
    {
        if (FindObjectsOfType<MarvinConsumer>().Length > 1)
        {
            UnityEngine.Debug.LogError("More than one MarvinConsumer found in the scene, destroying randomly");
            DestroyImmediate(this);
            return;
        }

        ConnectToServer();
    }

    void Update()
    {
        // Always try to connect if not connected already
        if (!m_Connecting && (m_TcpClient == null || !m_TcpClient.Connected))
        {
            Debug.Log("Connection to Marvin server was lost");
            ConnectToServer();
        }
        
        if (m_ConnectionEstablished)
        {
            // read and convert the data available
            if (m_TcpClient.Available > 0)
            {
                byte[] buffer = new byte[4096];
                int len = -1;

                Debug.Log("Data available");
                len = m_TcpClient.Client.Receive(buffer);
                if (len > 0)
                {
                    string speech = Encoding.ASCII.GetString(buffer);
                    Debug.Log("Read data: '" + speech + "'");

                    if (SpeechRecognized != null) SpeechRecognized(speech);
                }
            }
        }
        else if(m_Connecting)
        {
            if (m_TcpClient.Connected)
            {
                m_Connecting = false;
                Debug.Log("Connected to Marvin server");

                ProceedHandshake();
            }
            else if (Time.time > m_ConnectStartedTime + ConnectTimeout)
            {
                m_Connecting = false;
                m_TcpClient.Close();
                Debug.Log("Connection to Marvin server timed out");
                
                ConnectToServer();
            }
        }
    }

    private void ConnectToServer()
    {
        Debug.Log("Trying to connect to Marvin server");
        m_ConnectionEstablished = false;
        m_Connecting = true;
        m_ConnectStartedTime = Time.time;
        IPAddress[] IPs = new IPAddress[] {
            IPAddress.Loopback,
            IPAddress.IPv6Loopback
        };
        m_TcpClient = new TcpClient();
        m_TcpClient.BeginConnect(IPs, Configuration.Port, (IAsyncResult ar) =>
        {
            m_TcpClient.EndConnect(ar);
        }, null);
    }

    private void ProceedHandshake()
    {
        List<Phrase> phrases = new List<Phrase>()
        {
            /* Subjects */
            new Phrase("subject_all", "all"),
            new Phrase("subject_all", "everyone"),
            new Phrase("subject_all", "everybody"),
            new Phrase("subject_all", "guys"),
            new Phrase("subject_all", "lads"),
            new Phrase("subject_all", "squad"),
            new Phrase("subject_all", "as an element"),
            new Phrase("subject_all", "element"),
            new Phrase("subject_all", "gold ?team"),

            new Phrase("subject_fireteam1", "red ?team"),

            new Phrase("subject_fireteam2", "blue ?team"),

            /* Commands */
            new Phrase("cmd_follow", "follow ?me"),
            new Phrase("cmd_follow", "fall in"),
            new Phrase("cmd_follow", "fall in on me"),
            new Phrase("cmd_follow", "come"),
            new Phrase("cmd_follow", "come to me"),
            new Phrase("cmd_follow", "return ..."),
            new Phrase("cmd_follow", "fall back"),
            new Phrase("cmd_follow", "on me"),
            new Phrase("cmd_follow", "come here"),
            new Phrase("cmd_follow", "regroup"),
            new Phrase("cmd_follow", "regroup on me"),
            new Phrase("cmd_follow", "behind me"),

            new Phrase("cmd_status", "what ?is your status"), // "what's" can be tricky to understand, skipping it may help
            new Phrase("cmd_status", "give ?me ?your status"),
            new Phrase("cmd_status", "your status"),
            new Phrase("cmd_status", "status"),
            new Phrase("cmd_status", "report"),

            new Phrase("cmd_freeze", "freeze ..."),
            new Phrase("cmd_freeze", "stop ..."),
            new Phrase("cmd_freeze", "halt ..."),
            new Phrase("cmd_freeze", "hang on ..."),

            new Phrase("cmd_restrain", "restrain ... ?and"),
            new Phrase("cmd_restrain", "cuff ... ?and"),

            new Phrase("cmd_restrain_all", "restrain all ... ?and"),
            new Phrase("cmd_restrain_all", "restrain them ... ?and"),
            new Phrase("cmd_restrain_all", "restrain these ... ?and"),
            new Phrase("cmd_restrain_all", "restrain that bunch ... ?and"),
            new Phrase("cmd_restrain_all", "cuff all ... ?and"),
            new Phrase("cmd_restrain_all", "cuff them ... ?and"),
            new Phrase("cmd_restrain_all", "cuff these ... ?and"),
            new Phrase("cmd_restrain_all", "cuff that bunch ... ?and"),

            new Phrase("cmd_breach", "breach ?and"),
            new Phrase("cmd_breach", "breach ... with shotgun ?and"),
            new Phrase("cmd_breach", "open ... with shotgun ?and"),

            new Phrase("cmd_c2", "c2 ?and"),
            new Phrase("cmd_c2", "breach ... with c2 ?and"),
            new Phrase("cmd_c2", "open ... with c2 ?and"),

            new Phrase("cmd_ram", "ram ?and"),
            new Phrase("cmd_ram", "breach ... with ram ?and"),
            new Phrase("cmd_ram", "open ... with ram ?and"),

            new Phrase("cmd_open", "open ?and"),

            new Phrase("cmd_bang", "bang ?and"),
            new Phrase("cmd_bang", "flash ?bang ?and"),
            new Phrase("cmd_bang", "throw ?some ?flash bang ?and"),
            new Phrase("cmd_bang", "throw ?some flash ?and"),
            new Phrase("cmd_bang", "toss ?some ?flash bang ?and"),
            new Phrase("cmd_bang", "toss ?some flash ?and"),

            new Phrase("cmd_gas", "gas ?and"),
            new Phrase("cmd_gas", "cs ?and"),
            new Phrase("cmd_gas", "throw ?some gas ?and"),
            new Phrase("cmd_gas", "toss ?some gas ?and"),

            new Phrase("cmd_stinger", "stinger ?and"),
            new Phrase("cmd_stinger", "throw ?some stinger ?and"),
            new Phrase("cmd_stinger", "toss ?some stinger ?and"),

            new Phrase("cmd_lead_throw", "lead throw ... ?and"),
            new Phrase("cmd_lead_throw", "leader throw ... ?and"),
            new Phrase("cmd_lead_throw", "wait for my ... throw ?and"),
            new Phrase("cmd_lead_throw", "I throw ... ?and"),
            new Phrase("cmd_lead_throw", "I'll throw ... ?and"),

            new Phrase("cmd_clear", "clear ?out ?and"),
            new Phrase("cmd_clear", "?make entry ?and"),
            new Phrase("cmd_clear", "?go ?go ?go go ?and"),

            new Phrase("cmd_wait_mark", "wait ?and"),
            new Phrase("cmd_wait_mark", "wait for me ?and"),
            new Phrase("cmd_wait_mark", "wait for ?my top ?and"),
            new Phrase("cmd_wait_mark", "wait for ?my mark ?and"),
            new Phrase("cmd_wait_mark", "wait for ?my go ?and"),

            new Phrase("cmd_lightstick", "deploy ... ?light stick ... ?and"),
            new Phrase("cmd_lightstick", "drop ... ?light stick ?and"),
            new Phrase("cmd_lightstick", "mark ... with ?a ?light stick ?and"),
            new Phrase("cmd_lightstick", "light stick ?and"),

            /* Infos */
            new Phrase("info_safe", "all clear"),
            new Phrase("info_safe", "no contact"),
            new Phrase("info_safe", "no visual"),
        };

        /**
         * To calculate sentences permutations number.
         * 
         * Each element in the sentence has a multiplication value
         * - any "...":
         *      1
         *
         * - value "cmd_test":
         *      Phrase permutations number or 1 if just a word - simply 1 if only interested by semantic
         *
         * - values "(cmd_test,word)":
         *      Sum of each Phrase permutations number in the group + words count in the group - simply group count if only interested by semantic
         *
         * - optional "?cmd_test":
         *      value + 1
         *
         * - optionals "?(cmd_test,word)":
         *      values + 1
         * 
         * The number of permutations is equal to the product of each multiplication values in the sentence.
         * Example:
         *   "test (a|b|c) ... ?optional"
         * would give 1 x 3 x 1 x 2
         * 
         * Same example but 'test' is a Phrase wiht 4 permutations and 'a' is a Phrase with 3 permutations:
         *   "test (a|b|c) ... ?optional"
         * would give (4) x (3+2) x 1 x 2
        **/

        // Already 1200+ permutation - 50 000+ choices
        List<string> sentences = new List<string>()
        {
            // select a unit
            @"... (subject_all|subject_fireteam1|subject_fireteam2)",
            
            // simple order with no chaining
            @"... ?(subject_all|subject_fireteam1|subject_fireteam2) (cmd_follow|cmd_restrain|cmd_restrain_all|cmd_lightstick|cmd_status)",

            // simple command followed by move orders
            @"... ?(subject_all|subject_fireteam1|subject_fireteam2) (cmd_restrain|cmd_lightstick) cmd_follow",
            
            // entry point already opened
            @"... ?(subject_all|subject_fireteam1|subject_fireteam2) ?cmd_wait_mark (cmd_breach|cmd_c2|cmd_open|cmd_ram|cmd_bang|cmd_gas|cmd_stinger|cmd_lead_throw) ?(cmd_clear|cmd_wait_mark)",
            // special case for previous sentence
            @"... ?(subject_all|subject_fireteam1|subject_fireteam2) ?cmd_wait_mark cmd_clear",
            // entry point closed, need to open it before doing anything else
            @"... ?(subject_all|subject_fireteam1|subject_fireteam2) ?cmd_wait_mark (cmd_breach|cmd_c2|cmd_open|cmd_ram) (cmd_bang|cmd_gas|cmd_stinger|cmd_lead_throw) ?(cmd_clear|cmd_wait_mark)",
            // TODO: groups would help to make sentences great again - example for previous sentence:
            //@"... (grp_team) ?cmd_wait_mark (grp_open) (grp_throw) ?(grp_throw) ?(cmd_clear|cmd_wait_mark)",

            // orders to civilian/enemies & info
            @"... (cmd_freeze|info_safe)",
        };

        SocketAsyncEventArgs handshakeSendEvent = new SocketAsyncEventArgs();
        HandshakeRequest handshakeRequest = new HandshakeRequest(phrases, sentences);

        byte[] data = Utils.Serialize(handshakeRequest);

        handshakeSendEvent.SetBuffer(data, 0, data.Length);
        handshakeSendEvent.Completed += OnHandshakeDone;

        m_TcpClient.Client.SendAsync(handshakeSendEvent);
    }

    private void OnHandshakeDone(object sender, SocketAsyncEventArgs e)
    {
        // make sure we are still connected to the server - might've dropped because the API version is too old
        if (m_TcpClient.Connected)
        {
            m_ConnectionEstablished = true;
        }
    }
}
