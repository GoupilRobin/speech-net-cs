# speech-net-cs
A .NET server that start a speech recognition using grammar provided by the client.

It was created for a Unity project that required offline speech recognition.
The .NET System.Speech assembly start from .NET 3.0, whereas Unity only use 2.0 .NET assemblies (maybe 2.5 but that is still not good enough for me).
I created a separate binary, spawned by the Unity project, that communicate with a MonoBehaviour running in the project using TCP.

The communication between the client and the server is simplistic at best:
- Accept, send and receive are synchronous
- Server block until reading handshake from the Client
- Server send packet to Client everytime speech is recognized
- Since there is no ping, the Client and the Server rarely detect the other part has disconnected

By design the Server will wait for 10s after accepting a Client for the handshake. After that time, the Server exit.
The Server will also exit if the connection with the Client is lost.
As the Server is spawned by the Unity project, I wanted to make sure that the Server process won't become a zombie in case the game crash.
If the Server crash, the client continuously try to spawn it again.

TODO:
- Better, asynchronous network communication handle - no more blocking accept, send and receive
- Unity Server process spawner to observe a cooldown or a max retry count, followed by an event to inform external components (e.g. show error message to user)
- Better connection integrity detection. A simple ping should already help a lot
