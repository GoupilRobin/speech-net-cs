# speech-net-cs
A .NET server that start a speech recognition using grammar provided by the client.

It was created for a Unity project that required offline speech recognition and the Speech-to-Text UnityLabs solution was a real pain to setup. I also needed to generate the grammar from alternatives and the .NET Choiches and GrammarBuilder looked like a good fit.
The .NET System.Speech assembly start from .NET 3.0, whereas Unity only use 2.0 .NET assemblies (maybe 2.5 but that is still not good enough for me).
I created a separate binary, spawned by the Unity project, that communicate with a MonoBehaviour running in the project using TCP.

By design the Server will wait for 10s after accepting a Client for the handshake. After that time, the Server exit.
The Server will also exit if the connection with the Client is lost.
As the Server is spawned by the Unity project, I wanted to make sure that the Server process won't become a zombie in case the game crash.
If the Server crash, the client continuously try to spawn it again.

The server only send the semantic sentences, which allow the receiver to easily understand the result of the speech when multiple phrases mean the same thing.
For instance: "I am good", "I am fine", "I am OK" and simply "good" could all be understood as "user_ok", making easier to use the final result.

Please feel free to send PR if you happen to improve this.

# How to setup

## Interfaces

### Unity

Copy/paste the files __MarvinConsumer.cs__ and __MarvinStarter.cs__ from __Interfaces/Unity/__ into your Unity project's assets.
__MarvinStarter__ is a component that spawn the __Server__ process. You must specify the path relative to the project asset data folder to the binary in the component.
__MarvinConsumer__ is a component to connect to the __Server__, send the grammar and receive the semantic when speech is recognized.

Both components need to be present on an entity in the scene - putting more than one of each will result in the random destruction of the components until there is no more than one of each.

### Other

If you happen to create an interface that we are missing consider sending a PR so that it can be merged here.

## Marvin Interface
I strongly suggest adding the __MarvinInterface__ project to your Visual Studio Unity project.  
If needed, replace the path in the post-build event of the __MarvinInterface__ project to copy the binary to the correct place.

It contains the classes __Configuration__ (explicit enough), __Phrase__ (described later),  __Utils__ (only used for serialization/deserialization helpers so far) and __HandshakeRequest__ (sent by the Client to the Server, contains the grammar).

## Marvin Server
I strongly suggest adding the __MarvinServer__ project to your Visual Studio Unity project.  
If needed, replace the path in the post-build event of the __MarvinServer__ project to copy the binary to the correct place.
Add a reference to the __MarvinInterface__ project.

If you followed these step correctly, you should now be able to hop in Unity, start the scene with the two components __MarvinStarter__ and __MarvinConsumer__ and test the voice recognition.
The initial project being a tactical shooter, the default grammar is loaded with a set of rules of subject and orders.
To make sure everything is setup correctly, you should now be able to say various orders such as:
- Red drop a lightstick
- Everyone follow me
- Gold toss a bang
- Blue breach and clear
- open bang and wait

# Grammar syntax

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

The syntax for the sentence is:
- __value__: written with alphanumerical characters or underscore, required by the speech => jump
- __values__: multiple __value__ surrounded by parenthesis and separated by a pipe => (jump|run|cmd_open)
- __optional value__: a __value__ preceded by an interogation mark and not separated by space, not required in the speech => ?jump
- __optional values__: __values__ preceded by anow interogation mark, not required in the speech => ?(jump|run|cmd_open)
- __anything__: written as an ellipsis, can take the value of any speech but can lead to ambiguity if not careful => ...

A more complex sentence could be:
```C#
Sentence sentence = new Sentence("... ?(subject_all|subject_this) (cmd_open|cmd_lightstick) cmd_follow");
```
Resulting in the following permutations, where each word could be pointing to one or multiple phrases (resulting in even more sentences handed to the Speech Engine):
- ... subject_all cmd_open cmd_follow
- ... subject_all cmd_lightstick cmd_follow
- ... subject_this cmd_open cmd_follow
- ... subject_this cmd_lightstick cmd_follow
- ... cmd_open cmd_follow
- ... cmd_lightstick cmd_follow

# Calculating permutations number

While the server keep track of the total for you, it is good to have an idea how the impact each elements can have on the final number of sentences generated.

## Total number of phrases created by one Phrase:

Given a Phrase _p_ made of _n_ words.
Where the function _W(i)_ return the following values based on the type of _i_:
- Word: 1
- Optional: 2
- Anything: 1

The total number of phrases ![alt text](https://latex.codecogs.com/gif.latex?\inline&space;&T(p)) created by one Phrase is:

![alt text](https://latex.codecogs.com/gif.latex?T(p)&space;=\prod_{i=0}^{n}iW(i))

## Total number of sentences created by one Sentence:

Given a Sentence made of _m_ elements.
Where the function _E(i)_ return the following values based on the type of _i_:
- Value - not Phrase: 1
- Value - Phrase: ![alt text](https://latex.codecogs.com/gif.latex?\inline&space;&T(p))
- Values: with e elements => ![alt text](https://latex.codecogs.com/gif.latex?\inline&space;\sum_{i=0}^{e}value(i))
- Optional: ![alt text](https://latex.codecogs.com/gif.latex?\inline&space;value(i)&plus;1)
- Optionals: ![alt text](https://latex.codecogs.com/gif.latex?\inline&space;values(i)&plus;1)
- Anything: 1

The total number of sentences ![alt text](https://latex.codecogs.com/gif.latex?\inline&space;&T(s)) created by one Sentence is:

![alt text](https://latex.codecogs.com/gif.latex?T(p)&space;=&space;\prod_{i=0}^{m}iE(i))

To sumarize, be careful when using __Optionals__ made of __Phrases__ with a large number of permutations as these are in the end generating the most sentences.

To see the number of semantic sentences and actual number of sentences sent to the Speech Engine, you can examinate the value of `splittedSentences.Count` and `choichesCount` in the file __MarvinServer:SpeechToText.cs__ at the end of the function `private void UpdateGrammar()`.

### TODO:
- MarvinConsumer to use external grammar in XML or JSON for faster iterations and the possibility for the final user to modify it (modding <3)
- Adding "Group" to the Sentence syntax or as a Class to allow grouping several Phrases into one to make the grammar sentence declaration shorter and easier to read
- Send both the understood text and the semantic text on speech recognized
- Allow to read the speech catched by the __Anything__ in the Phrase and Sentence syntax - may be useful in case the user has a free choice such as number or color.  
Maybe with seperate syntax such as `new Phrase("prompt_number", "...{number}")` used like this `new Sentence("set width ?to prompt_number measure_unit")`.
- Add repeater to the Sentence and Phrase syntax such as the Regex one `new Sentence("(cmd_open|cmd_follow){2}")` to repeat the group 2 times and `new Sentence("(cmd_open|cmd_follow){1,3}")` to repeat between 1 to 3 times for instance (for 0 times as minimum, just make it optional)
- Unity Server process spawner to observe a cooldown or a max retry count, followed by an event to inform external components (e.g. show error message to user that something is wrong with the Marvin Server)
- Better connection integrity detection. A simple ping should already help a lot
- Add an abstract layer between the Server and the speech recognition to allow adding other speech libraries
- Evaluate alternate offline speech engine such as CMUSphinx
