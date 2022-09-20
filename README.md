![Space](https://user-images.githubusercontent.com/1116555/189133769-0a2272df-0f94-4279-bef5-ab7e654ca54e.png)


# SpaceHoliday
(Experimental) 🇸🇬 Holiday calendar extension for JetBrains Space. 

Save your team a trip to Ministry of Manpower's holidays directory -- yell `next` to the chatbot, and get a list of upcoming public holidays.

- Pulls official SG holiday data directly from the `data.gov.sg` endpoint. Configurable resources, see `dgs.json`.
- Counts the days till each upcoming holiday. Favourable days-of-week get a (Y).
- Multi-org application. Most of the code in here consists of plumbing for getting multi-org to work.
- Extensions have to be hosted by yourself, Space won't do it for you (yet?).
- This project has been tested on fly.io, and starts at about 120MB, which is about 50% of the free tier capacity.
- Made through a Rider 30-day trial. May not be able to update this after mid-October.

---

Extra tips

- Getting started with developing on Space and deciding between their Kotlin/.NET SDK? Kotlin seems to be their preferred SDK, with [slightly](https://github.com/JetBrains/space-app-tutorials) [more examples](https://www.jetbrains.com/help/space/tutorials.html) in their docs.
- There isn't a multi-org tutorial for .NET yet, so it'll be a lot quicker to get started from [this sample project](https://github.com/JetBrains/space-app-tutorials/tree/main/dotnet/space-translate/SpaceTranslate).


### Deploying Space applications on fly.io

Space applications have to live _somewhere_. Conveniently, fly.io offers a generous free tier that is just enough for basic Space applications. The application must first be packaged in a docker image before it can be deployed on fly.io.

#### Build

First, the application has to be built. This configuration worked for me:

![rider64_UrZDaAsNxk](https://user-images.githubusercontent.com/1116555/191182958-1ffd66c2-d187-4df6-98ef-c232531c62a8.png)

#### Package for fly.io

This is an example `Dockerfile` to create a deployment image:

```
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64

EXPOSE 8080

RUN mkdir /app
WORKDIR /app
COPY ./publish/. ./
RUN chmod +x ./SpaceHoliday
CMD ["./SpaceHoliday"]
```

`FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64`
- Use a known-good base alpine linux image from Microsoft that is compatible with the application.
- Also matches the architecture of fly.io (amd64)

`EXPOSE 8080`
- The application should be [serving http (non-secure!) on port 8080](builder.WebHost.UseUrls("http://0.0.0.0:8080");). 
- This instruction opens port 8080 from the docker VM to fly.io
- Fly.io will automatically apply a http reverse proxy for us, including automatic certs for https. 
- Port 8080 will *not* be exposed to the public internet.

`RUN mkdir /app`,
`WORKDIR /app`
- Create a directory to store the application files, and set the current working directory to `/app`

`COPY ./publish/. ./`, 
`RUN chmod +x ./SpaceHoliday`, 
`CMD ["./SpaceHoliday"]`
- Copy the `publish` folder containing the built application into the current working directory in the virtual machine.
- Set the execute bit on the application binary to allow it to be run.
- Run the application, in this case, `SpaceHoliday`

#### Handy commands

- Build an image: `docker build --progress=plain --no-cache -t spaceapp .`
- Run an image locally: `docker run -dp 8080:8080 spaceapp`
- Create a new fly.io instance: `flyctl launch`
- Update an existing fly.io instance: `flyctl deploy` 

#### Persistence

In its default configuration, the application stores data through sqlite. **The database will be destroyed on each deployment** as the container is rebuilt. For storing data across deployments, fly.io offers persistent volumes.

This command creates a 1GB persistent volume named `data` in the Singapore region:

`flyctl volumes create data --region sin --size 1`

To mount this new volume, modify `fly.toml` and add these lines to mount the new volume as `/data` in the virtual machine:

```
  [[mounts]]
    source = "data"
    destination = "/data"
```

Finally, adjust the `Data Source` [sqlite path](https://github.com/jglim/SpaceHoliday/blob/003b7113c116c59b3ad798216069952d526d3397/SpaceHoliday/Program.cs#L10) to use the new volume: 

`builder.Services.AddSqlite<SpaceDb>("Data Source=/data/application.db;Cache=Shared");`

#### Publish


When the application is successfully published on fly.io, it should be accessible on yourappname.fly.dev (where `yourappname` is your fly.io instance name).

In this project, the API endpoint is defined here: https://github.com/jglim/SpaceHoliday/blob/003b7113c116c59b3ad798216069952d526d3397/SpaceHoliday/Startup/SpaceStartupExtensions.cs#L17

Therefore, the full API endpoint to be submitted to JetBrains Marketplace should be `yourappname.fly.dev/api/space`.

---

At this time, the submission for this project in JetBrains Marketplace is stuck at "Check Succeeded" after selecting "Send for Verification". It is not publicly visible in the marketplace at this time. 
