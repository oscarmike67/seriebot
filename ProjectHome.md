Updates users with information about there favorite series in a direct connect hub.

[Click here if you want to know HOW to run it](HowToRun.md)

# Available commands are: #

## +next <serie name> ##
Example:
```
[14:20:12] <User> +next smallville
[14:20:13] <serieBot> Smallville - [S09E15] Escape - 2010-04-02
```

## +last <serie name> ##
Example:
```
[14:22:23] <User> +last smallville
[14:22:23] <serieBot> Smallville - [S09E14] Conspiracy - 2010-02-26
```

## +new ##
Example:
```
[09:24:33] <User> +new
[09:24:35] <serieBot> Please note that this command may take several minutes to complete. (Writing the command more then once will reset your position in queue and place you last)
[09:26:30] <serieBot> Your current serie information:
I have found 48 different series in your share.
You want me to ignore 20 of them.
	Heroes: You are behind 2 episodes.		(Your last episode is: S04E17)
	Legend of the Seeker: You are behind 1 episode.		(Your last episode is: S02E12)
	Smallville: You are behind 1 episode.		(Your last episode is: S09E13)
	The Simpsons: You are behind 1 episode.		(Your last episode is: S21E12)


This result was given to you by: http://code.google.com/p/seriebot/ with the help by: www.TvRage.com
```

Gives user information on new episodes for series he/she is sharing.
(Will find all files in user:s share that match this regexp:
```
 _.*\\([a-zA-Z\.]+)\.[S|s]([0-9]{1,2})[E|e]([0-9]{0,2}).*\\.*_).
```

## +countdown / +cd ##
Example:
```
[11:11:29] <User> +cd
[21:31:04] <SerieBot> Please note that this command may take several minutes to complete. (Writing the command more then once will reset your position in queue and place you last)
[21:34:20] <SerieBot> Countdown of your Series:
	Today (2010-09-27):
		Chuck

	Tomorrow (2010-09-28):
		NCIS
		NCIS: Los Angeles
		Stargate Universe
		Top Gear Australia

	Thursday (2010-09-30):
		Bones
		Community
		CSI: Crime Scene Investigation
		Fringe
		The Big Bang Theory
		The Mentalist

	Friday (2010-10-01):
		CSI: NY
		Smallville

	Saturday (2010-10-02):
		Merlin (2008)

	Sunday (2010-10-03):
		CSI: Miami
		Dexter
		Mad Men
		The Simpsons

	Next week (2010-10-04 -> 2010-10-10):
		MythBusters (2010-10-06)

	More than 2 weeks:
		V (2009) (2010-11-01)
		Burn Notice (2010-11-11)
		Warehouse 13 (2010-12-01)
		Caprica (2011-01-14)


This result was given to you by: http://code.google.com/p/seriebot/ with the help by: www.TvRage.com, www.TheTvDb.com
```

## +ignore ##
Makes it possible to ignore Series in the +new command.