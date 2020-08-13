# pixel-bot

This is a pixel bot for playing World of Warcraft in two components:

* `wow-addon/` - Lua addon for exposing in-game state via magic pixels.
* `bot/` - C# application for reading magic pixels and performing in-game actions.

The bot requires a user to pilot and target enemies, but will manage combat against any targets provided (e.g. cast buffs, ensure adequate health/mana, cast spells).

I only implemented mages, then got bored.
