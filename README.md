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
