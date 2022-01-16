# Badimebot

Badimebot is a general irc bot for movie or anime/tv episode watchalongs.   

### Supported commands for general public
* Channel message ```!badime``` :  Returns the elapsed time for the current item being watched.  If it has not started yet, this returns a negative number.
* Private message ```!badime``` :  Returns the elapsed time for the current item being watched.  If it has not started yet, this returns a negative number.

Note:  The ```!``` can be replaced by ```@``` and will also work.

### Supported commands for admin
* Private message ```add <Title of episode> for <Length Timespan> in <Countdown Timespan>``` : Enqueues an episode and immediately begins counting down to it.  
Example:  ```add Evangelion episode 01 for 25:00 in 05:00``` would immediately begin a timer for 5 minutes,  print periodic announcements to the current channel so others can be notified of when it will start, and then keep track of duration elapsed once it begins.
* General Timespan format:   <2 digit number represents minutes>:<2 digit number represents seconds>.
