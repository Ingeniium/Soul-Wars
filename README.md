
# Soul-Wars

9/15/2017
---------
There will be significant code changes aimed at improving code legibility, accomodating changes that came from upgrading from 5.2 to 5.7(due to me buying a new laptop), and documentation in addition to improvements to AI,equipment, and (eventually) level building

9/29/2017
---------
I have uploaded code that properly syncs objects that would traditional be children on the network (Guns and Shields). The problem was that the NetworkTransfrom object didn't sync child objects very well, and the NetworkTransfromChild had to be set at editor time rather than runtime. It works,but only once the objects are utilized once. The flurry object's 'Debris' skill has also been modified.

10/11/2017
---------
I have started working on each gun's 3rd column of gun abilities. Strike is the only one with full three columns of 4 abilities each, with
Blaster coming in second with only half of that.I have also figured out how I want my first 10 levels to be structured,and I will be 
displaying tutorial tips in each of those levels.

11/7/2017
---------
I have finished Blaster's third column of gun abilities. I have also started working on Haze's third column,and reworked its 'Infect' gun ability so it works as intended. I have implemented 11 levels for the game. My sophmore friend, Ian Mcdonald,also worked on a scene(called test) which has a particle effect he made himself and imported a font.Essentialy, he made a title screen(with the whole folder TitleScreenAssets attributed to him) in which I further developed to include a level select.I have fixed aggro so it works fully as intended.I also reconfigured the game to allow multiple teams of CPUs that can fight amongst eachother, and can even be allied with the player to fight other CPUs.For this reason, the different types of CPUs now have colored shells to show their team alignment.
