# Udemy - Mehmet Ozkaya - React - .NET Microservices: DDD, CQRS, Vertical/Clean Architecture

## Section 04 - ASP.NET for Microservice Development: Minimal APIs, DI, Routing, and Docker

### Preamble

I'm putting my code for the very simple Todo API on GitHub in the hope that someone else trying to figure out how to do this course in macOS using Visual Studio Code (VS Code) will have an easier time. Or potentially someone else troubleshooting the interaction between VS Code and Docker.

In the Udemy course Mehmet uses Visual Studio, which integrates differently (arguably better) with Docker. VS Code can do all the things Mehmet demonstrates, but requires some tweaking.

All the below is done on macOS 26.4.1 using VS Code 1.119.0. Also I'm using .NET Core 10, not 8.

### Project Creation and Running Without Docker

Let's start with the basic, pre-Docker, project, created thusly (screenshots show using the Command Palette, Cmd-Shift-P, but you can also do this through the terminal):

![Creating new .NET Project in VS Code Command Palette](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_NETCreateProject_Step01.png)

![Selecting "ASP.NET Core Empty" as the Project Template in VS Code Command Palette](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_NETCreateProject_Step02.png)

(Not shown: between selecting the Project Template and naming the project, we must select the folder we are putting the project in.)

![Naming the new project "TodoApi" in VS Code Command Palette](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_NETCreateProject_Step03.png)

![Creating the project in VS Code Command Palette](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_NETCreateProject_Step04.png)

So that once we add the couple of files from the instruction, and modify Program.cs, we have the following file structure:

	v TodoApi
		- v TodoApi
			- appsettings.Development.json
			- appsettings.json
			- > bin
			- > obj
			- Program.cs
			- > Properties
			- TodoApi.csproj
			- TodoDb.cs
			- TodoItem.cs

This builds and runs as expected in VS Code.

In HTTP (which it runs as by default as the http profile is first in launchSettings.json):

![Project running in HTTP only in VS Code](/Section04MinimalAPIsDIRoutingDocker/assets/RunFromVSCode01_HTTP_RunningInVSCode.png)

![HTTP access to the /todoitems endpoint, empty as no Todo Items have been posted to it](/Section04MinimalAPIsDIRoutingDocker/assets/RunFromVSCode02_HTTP_TodoItemsEndpoint.png)

And in HTTPS (we have to specify which profile to use in the `dotnet run` command to `dotnet run --launch-profile https`):

![Project running in HTTPS and HTTP in VS Code](/Section04MinimalAPIsDIRoutingDocker/assets/RunFromVSCode03_HTTPS_RunningInVSCode.png)

![HTTPS access to the /todoitems endpoint, empty as no Todo Items have been posted to it](/Section04MinimalAPIsDIRoutingDocker/assets/RunFromVSCode03_HTTPS_TodoItemsEndpoint.png)

The issues begin when we try run the project in Docker.

### Docker - Dockerfile and Initial Attempt

With the "Container Tools" VS Code extension (I am using 2.4.4) we can add Docker files to the project using the `Containers: Add Docker Files to Workspace...` in the Command Palette, but if we follow Mehmet's lead in Visual Studio and only add the Docker and not the Docker Compose (compose.yaml) file, it doesn't entirely work.

The following Command Palette sequence (note that we are not adding a Docker Compose file)

![Containers: Add Docker Files 01](/Section04MinimalAPIsDIRoutingDocker/assets/DockerSetupOriginal01.png)

![Containers: Add Docker Files 02](/Section04MinimalAPIsDIRoutingDocker/assets/DockerSetupOriginal02.png)

![Containers: Add Docker Files 03](/Section04MinimalAPIsDIRoutingDocker/assets/DockerSetupOriginal03.png)

![Containers: Add Docker Files 04](/Section04MinimalAPIsDIRoutingDocker/assets/DockerSetupOriginal04.png)

![Containers: Add Docker Files 05](/Section04MinimalAPIsDIRoutingDocker/assets/DockerSetupOriginal05.png)

produces the following Dockerfile:

```
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5050

ENV ASPNETCORE_URLS=http://+:5000

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["TodoApi/TodoApi.csproj", "TodoApi/"]
RUN dotnet restore "TodoApi/TodoApi.csproj"
COPY . .
WORKDIR "/src/TodoApi"
RUN dotnet build "TodoApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TodoApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoApi.dll"]

```
After creating the Dockerfile, our top-level directory now looks like this:

	v TodoApi
		- .dockerignore
		- v .vscode
			- launch.json
			- tasks.json
		- > TodoApi

The closest parallel to setting the running profile to "Docker" in Visual Studio when using VS Code (as far I can tell as of yet) is the "Containers: .NET Launch" option in the Run and Debug sidebar (this option comes from the launch.json and is configured there):

!["Containers: .NET Launch" option in Run and Debug sidebar](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_RunAndDebug_ContainersNETLaunch.png)

But that errors out (MSB1009: Project file does not exist):

![](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_RunAndDebug_ContainersNETLaunch_Error.png)

### Docker - Fixing Deployment

First we're going to fix our project's file structure.

Close your TodoApi VS Code widow. Move the .vscode directory and .dockerignore file into the inner TodoApi directory so it looks like this:

	v TodoApi
		- v TodoApi
		 	- .dockerignore
			- v .vscode
				- launch.json
				- tasks.json
			- appsettings.Development.json
			- appsettings.json
			- > bin
			- Dockerfile
			- > obj
			- Program.cs
			- > Properties
			- TodoApi.csproj
			- TodoDb.cs
			- TodoItem.cs

(In a real project, the upper TodoApi folder would contain a README, a LICENSE, and depending on how your repo is structured, the .git directory and .gitignore file.)

In VS Code, reopen TodoApi from the inner TodoApi directory. To check that you're in the right folder, your terminal prompt should look something like:

![VS Code terminal prompt after reopening VS Code](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_TerminalCheckAfterFileChange.png)

Trying to run and debug the project again we still get an error:

```
Unable to determine project information for project 'REDACTED_ROOT_FOLDER_PATH/TodoApi/TodoApi/TodoApi/TodoApi.csproj': Process exited with code 1
```
![](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_UnableToDetermineProject.png)

We need to make some tweaks to the Dockerfile, launch.json, and tasks.json files for the project to deploy to Docker correctly:

Dockerfile:
```diff
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5050

ENV ASPNETCORE_URLS=http://+:5000

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
-COPY ["TodoApi/TodoApi.csproj", "TodoApi/"]
+COPY ["TodoApi.csproj", "TodoApi/"]
RUN dotnet restore "TodoApi/TodoApi.csproj"
COPY . .
WORKDIR "/src/TodoApi"
RUN dotnet build "TodoApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TodoApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoApi.dll"]

```

launch.json:
```diff
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/TodoApi/bin/Debug/net10.0/TodoApi.dll",
      "args": [],
      "cwd": "${workspaceFolder}/TodoApi",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    },
    {
      "name": ".NET Core Attach",
      "type": "coreclr",
      "request": "attach"
    },
    {
      "name": "Containers: .NET Launch",
      "type": "docker",
      "request": "launch",
      "preLaunchTask": "docker-run: debug",
      "netCore": {
-        "appProject": "${workspaceFolder}/TodoApi/TodoApi.csproj"
+        "appProject": "${workspaceFolder}/TodoApi.csproj"
      }
    }
  ]
}
```

tasks.json:
```diff
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
-        "../../../../Library/Application Support/Code/User/workspaceStorage/02db2017399d0479911fd0d9b7d4abba/ms-dotnettools.csdevkit/TodoApi.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary;ForceNoAlign"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "publish",
      "command": "dotnet",
      "type": "process",
      "args": [
        "publish",
        "../../../../Library/Application Support/Code/User/workspaceStorage/02db2017399d0479911fd0d9b7d4abba/ms-dotnettools.csdevkit/TodoApi.sln",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary;ForceNoAlign"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "watch",
      "command": "dotnet",
      "type": "process",
      "args": [
        "watch",
        "run",
        "--project",
        "../../../../Library/Application Support/Code/User/workspaceStorage/02db2017399d0479911fd0d9b7d4abba/ms-dotnettools.csdevkit/TodoApi.sln"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "type": "docker-build",
      "label": "docker-build: debug",
      "dependsOn": [
        "build"
      ],
      "dockerBuild": {
        "tag": "todoapi:dev",
        "target": "base",
-       "dockerfile": "${workspaceFolder}/TodoApi/Dockerfile",
+       "dockerfile": "${workspaceFolder}/Dockerfile",
        "context": "${workspaceFolder}",
        "pull": true
      },
      "netCore": {
-       "appProject": "${workspaceFolder}/TodoApi/TodoApi.csproj"
+       "appProject": "${workspaceFolder}/TodoApi.csproj"
      }
    },
    {
      "type": "docker-build",
      "label": "docker-build: release",
      "dependsOn": [
        "build"
      ],
      "dockerBuild": {
        "tag": "todoapi:latest",
        "dockerfile": "${workspaceFolder}/TodoApi/Dockerfile",
        "context": "${workspaceFolder}",
        "platform": {
          "os": "linux",
          "architecture": "amd64"
        },
        "pull": true
      },
      "netCore": {
        "appProject": "${workspaceFolder}/TodoApi/TodoApi.csproj"
      }
    },
    {
      "type": "docker-run",
      "label": "docker-run: debug",
      "dependsOn": [
        "docker-build: debug"
      ],
      "dockerRun": {
+		"ASPNETCORE_URLS": "https://+:5050;http://+5000"
       },
      "netCore": {
-       "appProject": "${workspaceFolder}/TodoApi/TodoApi.csproj",
+       "appProject": "${workspaceFolder}/TodoApi.csproj",
        "enableDebugging": true
+		"configureSsl": true
      }
    },
    {
      "type": "docker-run",
      "label": "docker-run: release",
      "dependsOn": [
        "docker-build: release"
      ],
      "dockerRun": {},
      "netCore": {
        "appProject": "${workspaceFolder}/TodoApi/TodoApi.csproj"
      }
    }
  ]
}
```
And with those changes the project deploys to Docker:

![](/Section04MinimalAPIsDIRoutingDocker/assets/DockerDeploy_HTTPOnly.png)

However, while HTTP works correctly, HTTPS does not.

### Docker - Fixing HTTPS

As far as I can tell, there is no way for Docker on macOS to work over HTTPS without a Docker Compose file, so we need to create one. We use the `Containers: Add Compose Files to Workspace...` command:

![](/Section04MinimalAPIsDIRoutingDocker/assets/VSCode_ContainersAddDockerCompose.png)

Which gives us the following default Docker Compose file:

```
# Please refer https://aka.ms/HTTPSinContainer on how to setup an https developer certificate for your ASP.NET Core service.

services:
  todoapi:
    image: todoapi
    build:
      context: .
      dockerfile: ./Dockerfile
    ports:
      - 5000:5000
      - 5050:5050

```
Which we then modify to:

```
# Please refer https://aka.ms/HTTPSinContainer on how to setup an https developer certificate for your ASP.NET Core service.

services:
  todoapi:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=5000
      - ASPNETCORE_HTTPS_PORTS=5050
      - ASPNETCORE_Kestrel__Certificates__Default__Password=pass
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    image: todoapi
    build:
      context: .
      dockerfile: Dockerfile
    volumes:
      - ~/.aspnet/https:/https:ro
```

The above assumes you have a self-signed certificate that dotnet trusts. For more information on this, see: [Microsoft Learn - Hosting ASP.NET Core images with Docker over HTTPS](https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-10.0)

We then modify the Dockerfile:
```diff
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5050

-ENV ASPNETCORE_URLS=http://+:5000
+ENV ASPNETCORE_URLS=http://+:5000;https://+:5050

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src

COPY ["TodoApi.csproj", "TodoApi/"]
RUN dotnet restore "TodoApi/TodoApi.csproj"
COPY . .
WORKDIR "/src/TodoApi"
RUN dotnet build "TodoApi.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "TodoApi.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoApi.dll"]

```
And with that, running and debugging to the "Containers: .NET Launch" configuration should successfully deploy to Docker, your api should be accessible through both HTTP and HTTPS:

![Docker Desktop Containers view, showing HTTP port](/Section04MinimalAPIsDIRoutingDocker/assets/Docker_SuccessfullyDeployed_HTTP_Port.png)

![Docker Desktop Containers view, showing HTTPS port](/Section04MinimalAPIsDIRoutingDocker/assets/Docker_SuccessfullyDeployed_HTTPS_Port.png)

![HTTP /todoitems api endpoint](/Section04MinimalAPIsDIRoutingDocker/assets/Docker_SuccessfullyDeployed_HTTP_Port_Endpoint.png)

![HTTPS /todoitems api endpoint](/Section04MinimalAPIsDIRoutingDocker/assets/Docker_SuccessfullyDeployed_HTTPS_Port_Endpoint.png)

Finally, debugging in VS Code works as well:

![Breakpoint being hit in the /todoitems api endpoint in VS Code](/Section04MinimalAPIsDIRoutingDocker/assets/Docker_SuccessfullyDeployed_DebuggingBreakpoint.png)
