using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class MarvinStarter : MonoBehaviour
{
    private Process m_MarvinProcess;
    private static readonly string ProcessPath = Path.Combine(Path.Combine("TactShooter", "Plugins"), "MarvinServer.exe");

    void Awake()
    {
        StartMarvinProcess();
    }

    void OnDestroy()
    {
        if (m_MarvinProcess != null && !m_MarvinProcess.HasExited)
        {
            m_MarvinProcess.Kill();
        }
    }

    void Update()
    {
        if (m_MarvinProcess == null || m_MarvinProcess.HasExited)
        {
            //UnityEngine.Debug.Log("Marvin process has stopped");
            //StartMarvinProcess();
        }
    }

    private void StartMarvinProcess()
    {
        //m_MarvinProcess = Process.Start(Path.Combine(Application.dataPath, ProcessPath));
        //UnityEngine.Debug.Log("Marvin process started");
    }
}
