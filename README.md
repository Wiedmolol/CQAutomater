# CQAutomater

CQAutomater is a tool that runs in the background and automatically claims your miracles as soon as they are ready. It can also open the daily free chest, start battles with random person when your hourly battle is ready, automatically send a predefined lineup to beat DQ or run the calc to solve it and finally fight World Bosses.

# v 0.9
#### General:
Implemented Task Queue - now game requests will be added to queue to prevent request spam;
Added bugs so I can fix them later. :D

#### Auto-AH:
Fully implemented.

#### Auto-Level
Every 4h, after DQ and after WB dies an autoleveler will run if you enable it. It will automatically level chosen heroes with prana/CC/AS. You can also convert AS to prana. 

# v 0.7
#### General:
Fixed DQSoundEnabled setting, added setting for chest opener chest amount. 

#### Auto-WB:
Added "safe mode". When safe mode is enabled the app will ask you for confirmation when it wants to attack the boss. It will tell you how many times it wants to attack it and with what lineup. It won't ask for the same boss more than once.

#### Auto-AH:
For now it's only party implemented. It can refresh current auction data but it won't make any bids. 

# v 0.6
#### General: 
Changed settings file from simple .txt to .json; New settings file will be automatically created from old MacroSettings.txt file after you run new version for the first time or you can create one from scratch using the "Settings Creator Helper". Creator Helper can be run with a button in "Other" tab. Program will also ask you if you want to run it, if you don't have any settings file present in your folder. Old "MacroSettings.txt" file can be removed after Settings.json file is created.

#### Auto-DQ: 
There is now a possibility to do DQs with preset lineup. It will use that lineup until it can no longer beat current DQ. If you don't check that option but have "Run the calc" option enabled it will still use the best lineup first, before attempting to solve DQs with calc. If you don't provide your best lineup(meaning all slots are empty) the app will automatically set best lineup to the last lineup used after calc is finished. 

#### Auto-Chest:
Small bug fix concerning listed rewards.

#### Auto-WB: 
Fully implemented. App will check the current WB every 5 minutes. If your attacks available are equal or greater than amount listed under "Attack amount requirement" the app will attack the boss X times where X is the value listed under "Attack target". "Attack target" can be greater than "attack requirement" - in that case if requirement is met app will try to reach the target regardless if target can actually be achieved. E.g if your requirement is 4 and target is 7 and you have 5 attacks available it will attack the boss 5 times. If in the meantime you will get another attack ready it will attack the WB again for 6 attacks total. If in the same scenario you have 3 attacks available boss won't be attacked but as soon as you get that 4th attack ready it will attack the boss 4 times.
Heroes Allowed bosses will be attacked with preset lineups. No Heroes bosses will be attacked by optimal lineups calculated by pebl and listed here: https://pastebin.com/u/pebl

### WARNING: 
Auto-WB will work correctly only if you've enabled your username on website: https://cosmosquest.net/enable.php
I really don't recommend using auto-WB feature without username enabled as this will probably cause you attack way too many times. 

# How to get Authentication Ticket and Kong ID:

You can follow instructions from that thread: https://www.kongregate.com/forums/910715-cosmos-quest/topics/965457-cq-macro-creator-for-diceycles-calc
or
you can use the "new" method. 
Add a new bookmark in your browser. In most browser you do that by right clicking on your bookmarks bar(CTRL+SHIFT+B if you can't see it) and choosing "Add Page". You can write anything in the "Name" field. In the "URL" field paste this:
>javascript:prompt('UserID:\n'+active_user.id()+'\n\nGameAuthToken:\n'+"Copy to clipboard: Ctrl+C, Enter", active_user.gameAuthToken());

Now make sure that currently selected tab in your browser is Kongregate with Cosmos Quest game and you are logged in. Click on the created bookmark and the windows should open with your KongID and Auth Ticket.

#### Don't share Authentication Ticket with anyone!

Now when you start a program for the first time and you don't have valid MacroSettings file, the program will ask if you need help with creating one. Just provide it with necessary info(for CQAutomater you only need KongID and AuthTicket, other settings are used only in CQMacroCreator), save them to file and restart the program.
