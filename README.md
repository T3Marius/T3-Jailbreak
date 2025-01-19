# T3-Jailbreak

# Features:
- Simon Sistem:
   - Simon Menu: (!smenu).
   - Simon Model: Simon can have a specail model which you can set it from config. 
   - Open: Simon can open cells using !o command and closing them using !c command.
   - Laser: Simon can use laser holding E and can change the laser color from !smenu (it remains saved foreach player).
   - Marker: Simon can create a marker(circle) using ping and can change the marker color from !smenu(it remains saved foreach player).
   - Freeday: Simon can give freeday using !freeday <name> or using the !smenu.
   - Forgive: Simon can forgive a rebel using !pardon <name> command or the rebel can use !p and a menu will pop up on simon screen asking him if he wants to forgive him.
   - Cuffs: Simon can put someone on cuffs using his special taser. While the prisoner is in cuffs Simon can move him wherever he wants holding right click at him.
   - Prisoner Color: Simon can color prisoners using !color <playername> <color> or using the !smenu (which is more easier.) NOTE: You can use all the colors.
   - Box: Simon can use the !box command allowing prisoners to hurt eachother. The box needs to be stopped using !box again.
   - Extend: Simon can extend round by X minutes using !extend command. This will open a menu with minutes to be extended.

- Deputy Sistem:
   - Simon can have a deputy when one of the guards uses !d or !deputy. 
   - Deputy Model: Deputy can have a special model too if you add it from config.
   - Deputy can use !box , !open and !color commands so he can be helpful to Simon.
   - A Deputy can give up on his role using !ud.
   - If the Simon dies and there is a Deputy, the deputy immediatly becomes the new Simon.
   
- Special Days:
   - Current Special Days:
   ```md
   1. OneInTheChamber
   2. Teleport
   3. WarDay
   4. DrunkDay
   5. HideNSeek
   6. NoScope
   7. ArmsRace
   8. FreeForAll (all players have acces to !guns command).
   9. Zombie (not finished yet.)
   ```
  - Special Days can be enabled/disabled from config.
  - The only persons who can set a special day are Admins and Simon.
  - Each special day has a countdown before it starts so players can run.
  - Special Day can be setted each 3 rounds. It has a 3 rounds cooldown by default but you can set it from config.

- Last Request:
  - Current Last Requests:
  ```md
   1. KnifeFight (5 types)
   2. ShotForShot
   3. MagForMag
   4. Rebel (can be used if there are more than 2 ct.)
   5. OnlyHeadshot
   6. NoScope
   7. Dodgeball
   ```
  - The last request wins are saved to the database and player can see the tops using !lrtop or !toplr.
  - On the last request the last prisoner can speak.
 
- Prisoner:
   - Sadly, you can't put a prisoner model from config. You need to install PlayerModelChanger and set it as default.
   - Prisoner can use !heal command twice a round and if simon accepts he will get an healthshot.
   - Rebel: When a prisoner shoots or hurt a ct he becomes rebel and turns red announcing everyone in chat.
   - If the rebel gives up and Simon forgives him , his weapons will be stripped.

- Queue Sistem:
   - Prisoners can join the queue using !queue command if the ct team is full>
   - You can set a special permission for queue system in config and that one will always be in fron of the queue.
   - When a ct place is free, the first prisoner in queue will be moved to CT on round end.

- Hud Text:
   - A hud static text to show the Current Simon. (Soon to show the current guardians and prisoners).

Guardians:
   - All guardians can use !guns command and select their guns. They can also save them by checking the Save Weapons option.
