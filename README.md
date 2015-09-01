# Pro Item Sets
A League of Legends item set generator that generates item sets from pro player purchase data.

View the current item sets at
http://www.kyleschouviller.com/lolprobuilds/setviewer.html

# Instructions for Running

1. Open the solution in Visual Studio.
2. Right-click on the ProBuilds project and select Properties.
3. Select the Debug tab on the left of the properties page.
4. In command line argument, enter your api key, followed by your per 10 seconds limit, then your per 10 minutes limit. e.g. `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa 3000 180000`
5. Build the solution.
6. Run the solution.

Optionally, add `-nodownload` to the end to run from cached data (if you have run the app before).

# Methodology

To create item sets, we pull pro player match data, process the match timelines to determine purchase times for a champion, then use the purchase data to calculate average build orders. We then process these build orders into item sets, performing some pre- and post-processing to clean up the item set and make it as usable in-game as possible.

Note that this is a standalone process, and produces static website data. This was intentional to minimize costs. However, it is easily ported to a persistently-running cloud solution.

## Version Identification

The first step is to identify the current item and champion versions. Because items and champions change between patches, we only want to use the most recent patch data. We only consider the first two numbers as version identifying, so `16.1.0.0` and `16.1.2.5` would both be considered `16.1`.

## Data Collection

Our data collection uses a [TPL DataFlow](https://msdn.microsoft.com/en-us/library/hh228603%28v=vs.110%29.aspx) pipeline to process player and match data. The pipeline has several stages, each of which runs in parallel.

### Load Cached Matches

We start by loading cached matches. This allows us to iterate on later stages of the pipeline faster, as we can limit match counts and load cached matches from disk to test any changes. Matches are serialized to JSON and stored gzipped to save space. In an automated solution, these would be stored in the cloud, using a blob-based storage solution, HDFS storage, or even a JSON data store like DocumentDB or MongoDB, depending on budget and accompanying technology choices.

### Get Pro Player Lists

We pull pro player ladders from all regions, which consist of challenger ladders followed by master ladders. Filtering to a particular region (or even generating item sets per region) would be interesting, but it would take much longer after a patch release to gather a significant quantity of match data per region.

### Get New Matches

We process players, pulling match listings per player (currently using the old API, due to RiotSharp's lack of support for the new API). For every player, we store a file identifying the latest-seen match from that player, and we stop pulling matches for a player once we've either (a) reached the latest seen match, or (b) reached a match with a version older than the current version.

### Get Match Timelines

We use the match ids generated in the previous stage to pull full match timeline data, skipping any matches we've already seen (either from another player, or from the cache). We then pass this data on to a pluggable processor.

### Process Matches Into Purchase History

We process each match timeline into 10 item transaction logs, one per champion in a match. These are also keyed by champion lane, as well as whether or not the champion has the summoner spell Smite.

The log entries track purchases, sales, destroys, and undos of items, as well as relevant game state at the time of the event. This state currently consists of per-team towers, per-champion kills, per-champion items, and game time. The log is then post-processed to eliminate undo events and their correlated purchase/sale/destroy event, correlate destroy events with consumable usage and item combination, and correlate sale events with the original purchase event.

We finally process this log into an aggregated tracker, which maintains averages for all relevant game state, as well as average upgrade paths, per item purchase. Note that in this case, "item purchase" is tracked as of the number of that item bought, so e.g. a second Doran's Ring bought during a match would have a separate tracker from the first Doran's Ring bought during a match.

In a continuously or frequently running solution, the system would periodically run the previous stages of the pipeline to gather new matches, and would maintain trackers in persistent storage. New items sets could be generated as new data comes in, without recalculating all match data. For now, we have not done this, as iteration on the algorithm has required constant recalculation of aggregate data.

## Item Set Generation

Once we have collected data and aggregated it per champion-lane-smite, we then use the data to generate item sets. We took several approaches to this, including [purchase graph analysis](http://kyleschouviller.com/lolprobuilds/purchasegraph.html) and a naive percentage cutoff method which utilized [purchase stats per game stage](http://kyleschouviller.com/lolprobuilds/purchasestats.html). Eventually, we settled upon a slightly less naive approach, which still produced nice results.

### Purchase Cutoff

We average purchase time of an item (item and the number of that item that have been purchased), and then implement a scaling cutoff that is more forgiving at later stages of a game. We found that a flat cutoff didn't work out well, because the volume of short games meant that some games ended before later stages were reached (due to surrender) or games ended early enough that players hadn't reached a full build. This meant that later-built items had a much lower percentage of purchase due to their depth in a build path.

We also found that item recipes weren't often completed in later stages of the game, so we include the most popular final purchase path given a base item purchase, as well as including any popular final purchase paths (with a high enough purchase percentage) past that minimum of one.

All of these percentages are easily tunable in `SetBuilderSettings`.

### Stage Separation

Near the end of set generation, we separate items based on stage of game. We defined game stage as:

Start: before 90 seconds, less than one kill, less than one tower destroyed
Early: less than one tower destroyed
Mid: less than 3 inner towers destroyed, and less than one base tower destroyed
Late: at least 3 inner towers destroyed or at least one base tower destroyed

Note that because game stages are computed based on averages, and purchases at different times of the game would weight averages up or down, we use "less than one" instead of "zero". We also use `2.5` instead of `3.0` for number of inner towers destroyed, as it distributes items between mid and late game more evenly.

These are all also tunable in `SetBuilderSettings`.

### Clean Up

We finally perform some post-processing, where we shuffle consumables to the end of blocks, combine adjacent items with the same id (because they are keyed by id and number bought), and try to ensure the starting block has enough items to purchase given starting gold. We also eliminate base items from mid and late game, as players are expected to choose among different items at that stage of the game, and showing only final items reduces clutter.

After the sets have been generated, we name them based on champion, lane, and whether or not Smite was taken. We then look for any champion sets that have only two lane-Smite combinations, where one set is "Jungle with Smite" and the other is any non-jungle lane without Smite, and we combine the sets, switching blocks on or off based on whether or not the Smite summoner spell was taken.

### Output

We output all item sets to their relative paths to the base League of Legends directory. Then we zip them all to create an "all item sets" downloadable package. We then output a manifest of all item sets to a directory that includes a static web page which reads that manifest to build an online item set browser. This browser displays a list of item sets, can display the full item set within the web page, and provides both a download link and a textbox displaying the JSON for the selected item set.

We also include additional data in our item set serialization which allows us to display item purchase percentages within the web page, as well as (in the future) the popularity of a particular build.

## Results

The item sets are fairly good, and provide insight into pro player build strategies. They also highlight upgrading and switching of trinkets, popular boot upgrade paths, and even popular elixir choices.

Further, because percentages are available, the item sets display some interesting data, including the popularity of the new Juggernaut items. For example, Dead Man's Plate is bought almost 50% of the time on Garen, though it is typically bought later into the game.

---

Pro Item Sets isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing League of Legends. League of Legends and Riot Games are trademarks or registered trademarks of Riot Games, Inc. League of Legends © Riot Games, Inc.
