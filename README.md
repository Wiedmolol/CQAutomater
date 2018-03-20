# CQAutomater

CQAutomater is a tool that runs in the background and automatically claims your miracles as soon as they are ready. It can also open the daily free chest and start battles with random person when your hourly battle is ready.

I also plan to add simple system that automatically attacks World Bosses and automatic-DQ(solves DQ if you have my CQMacroCreator and a calc from Diceycle or one of its forks).

# How to get Authentication Ticket and Kong ID:

You can follow instructions from that thread: https://www.kongregate.com/forums/910715-cosmos-quest/topics/965457-cq-macro-creator-for-diceycles-calc
or
you can use the "new" method. 
Add a new bookmark in your browser. In most browser you do that by right clicking on your bookmarks bar(CTRL+SHIFT+B if you can't see it) and choosing "Add Page". You can write anything in the "Name" field. In the "URL" field paste this:
>javascript:prompt('UserID:\n'+active_user.id()+'\n\nGameAuthToken:\n'+"Copy to clipboard: Ctrl+C, Enter", active_user.gameAuthToken());

Now make sure that currently selected tab in your browser is Kongregate with Cosmos Quest game and you are logged in. Click on the created bookmark and the windows should open with your KongID and Auth Ticket.

#### Don't share Authentication Ticket with anyone!

Now when you start a program for the first time and you don't have valid MacroSettings file, the program will ask if you need help with creating one. Just provide it with necessary info(for CQAutomater you only need KongID and AuthTicket, other settings are used only in CQMacroCreator), save them to file and restart the program.
