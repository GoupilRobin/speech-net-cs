# speech-net-cs
A .NET server that start a speech recognition using grammar provided by the client.

It was created for a Unity project that required offline speech recognition.
The .NET System.Speech assembly start from .NET 3.0, whereas Unity only use 2.0 .NET assemblies (maybe 2.5 but that is still not good enough for me).
I created a separate binary, spawned by the Unity project, that communicate with a MonoBehaviour running in the project using TCP.

By design the Server will wait for 10s after accepting a Client for the handshake. After that time, the Server exit.
The Server will also exit if the connection with the Client is lost.
As the Server is spawned by the Unity project, I wanted to make sure that the Server process won't become a zombie in case the game crash.
If the Server crash, the client continuously try to spawn it again.

TODO:
- Unity Server process spawner to observe a cooldown or a max retry count, followed by an event to inform external components (e.g. show error message to user)
- Better connection integrity detection. A simple ping should already help a lot

# How to setup

## Unity
Copy/paste the files MarvinConsumer.cs and MarvinStarter.cs from Unity/Marvin into your Unity project's assets.
MarvinStarter is a component that spawn the Server process. You must specify the path relative tot he project asset data folder to the binary in the component.
MarvinConsumer is a component to connect to the Server, send the grammar and receive the semantic when speech is recognized.

Both components need to be present on an entity in the scene - putting more than one of each will result in the random destruction of the components until there is no more than one of each.

## Interface
I strongly suggest adding the MarvinInterface/MarvinInterface project to the Unity project in Visual studio.
If needed, replace the path in thepost-build event of the MarvinInterface project to copy the binary to the correct place.

## Server
I strongly suggest adding the Marvin/MarvinServer project to the Unity project in Visual studio.
If needed, replace the path in thepost-build event of the MarvinInterface project to copy the binary to the correct place.
Add a reference to the MarvinInterface project.

If you followed these step correctly, you should now be able tohop in Unity, start the scene with the two components MarvinStarter and MarvinConsumer and test the voice recognition.
The initial project being a tactical shooter, the default grammar is loaded with a set of rules of subject and orders.
To make sure everything is setup correctly, you should now be able to say:
- Red on me
- Everyone follow me
- Blue breach and clear
- Red open bang and wait

# Grammar framework

## Phrases

Phrases are a set of words too small or missing parts to make a valid sentence.
The class __Phrase__ allow to declare a phrase with optional words, wildcards and assign it a semantic value.
The __Phrase__ constructor take the semantic value, followed by the phrase:
```C#
Phrase phrase = new Phrase("cmd_open", "open");
Phrase phrase = new Phrase("cmd_open", "start");
Phrase phrase = new Phrase("cmd_open", "execute");
```

The syntax for the phrase is:
- __word__: written with alphanumerical characters or underscore and can be separated by spaces, required by the speech => jump there
- __optional word__: a __word__ preceded by an interogation mark and not separated by space, not required in the speech => ?jump
- __anything__: written as an ellipsis, can take the value of any speech but can lead to ambiguity if not careful => ...

A more complex phrase could be:
```C#
Phrase phrase = new Phrase("cmd_lightstick", "deploy ... ?light stick ... ?and");
```
Resulting in the following permutations:
- deploy ... light stick ... and
- deploy ... light stick ...
- deploy ... stick ... and
- deploy ... stick ...

## Sentences

While sentences are normally a well defined arrangement of words, the definition for this project is a bit loose.
Here, the sentence is defined as an arrangement of semantic values (from already defined phrases) but also allowing to use value(s), optional value(s) and wildcard.
The __Sentence__ constructor take a sentence, later used in conjuction with the __Phrase__(s) declared to generate a full permutation set of possible recognizable sentences.
```C#
Sentence sentence = new Sentence("... cmd_open");
```

The syntax for the phrase is:
- __value__: written with alphanumerical characters or underscore, required by the speech => jump
- __values__: multiple __value__ surrounded by parenthesis and separated by a pipe => (jump|run|cmd_open)
- __optional value__: a __value__ preceded by an interogation mark and not separated by space, not required in the speech => ?jump
- __optional values__: __values__ preceded by anow interogation mark, not required in the speech => ?(jump|run|cmd_open)
- __anything__: written as an ellipsis, can take the value of any speech but can lead to ambiguity if not careful => ...

A more complex sentence could be:
```C#
Sentence sentence = new Sentence("... (cmd_open|jump|run) ?and ?(cmd_open|jump|run)");
```
Resulting in the following permutations: TODO
- deploy ... light stick ... and
- deploy ... light stick ...
- deploy ... stick ... and
- deploy ... stick ...
