
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
