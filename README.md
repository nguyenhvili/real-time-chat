# Real time chat application
This repository is a simple real-time chat that uses either WebSockets or REST API for communication.

Repository is divided into multiple projects
- RealTimeChat - chat server
- RealTimeChat.Client - clients that are connecting to server
- RealTimeChat.Database - database project, containing database entities
- RealTimeChat.Models - models that are shared between Client and Server
- RealTimeChat.Tests - integration tests for verifying server functionality

## Setup
Requirements
- .NET 10 SDK (https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

Server is required for chat clients to work, which can be simply build and run through `Visual Studio` or by navigating into `RealTimeChat` and running `dotnet build` and `dotnet run`.

Clients can be run by running both `dotnet build` and `dotnet run` in `RealTimeChat.Client` folder.

After running client, a dialog prompting user to enter their username is displayed and it is required for chatting.

Client uses WebSockets by default, since it is the most convenient way to chat. User can switch communication to REST API in `File > Settings` and select `REST API (polling)`. Users can also change their username similar way.
