# FantasyFootballAnalyzer
Code for collecting Yahoo fantasy football information and simulating seasons to predict each team's playoff odds.

In order to run this code you need to set get a Yahoo Network Developer login, which will grant you access to the tokens needed to make API requests.

Basic workflow:

1. Run the PageScraper project to collect projected points data for each player, in each week of the season.
2. Run the HTMLParser to process the saved projected poitns pages, and output each player's projections.
3. Run the FantasyFootballAnalyzer to generate various stats for each team, included projected wins, projected points scored, schedule difficulty information, and most important odds of reaching the playoffs.

Note that many of the settings in this code are specific to my own league (for example, assuming 13 weeks in the regular season, 8 teams make the playoffs, etc). This could will likely not work out of the box for any other league setup, but could be adapated to do so.
