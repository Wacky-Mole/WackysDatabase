| Version | Changes                                                                                                                                                                                                                                                                                                                                |
|----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 2.4.56  | Added SeFrost and SePoison. Also added - `m_attackStaminaUseModifier, m_blockStaminaUseModifier, m_dodgeStaminaUseModifier, m_swimStaminaUseModifier, m_homeItemStaminaUseModifier, m_sneakStaminaUseModifier, m_runStaminaUseModifier for SEs
| 2.4.55  | Added EndingStatusEffect to SE's, you can now chain SEs together. Don't forget to use Category. </br> Updated ServerSync.
| 2.4.54  | Added burnable to WearnTear. 
| 2.4.53  | Added Display Normal Logs config. If you want to turn off most of wackydb logs. You shouldn't do this unless you have a good reason. ie 1000+ yamls </br> Removed comfortObject and replaced with comfortObjectName. </br> Lowered some warnings to just info logs. 
| 2.4.52  | Fixed Tooltip for Water
| 2.4.51  | Update 220.3 - ServerSync update. Fixed Startup Error on GamePass Crossplay |
| 2.4.50  | Added ShieldGenData </br>Added BatteringRamData </br>Okay, I’ve had some questions about icons being in folders over the past few weeks. You can specify the relative file path of the icon. For example, if your icon is in the folder `Icons/Items/wacky.png`, just use `"Items/wacky.png"`. Will revealing the "secret" forward slash come back to haunt me? Absolutely. |
| 2.4.48  | ConsumableStatusEffect can be deleted now ("delete"). </br> Added materialType to WearNTear on pieces. You can now change the material the piece is made out of and it's support strength as a result. 
| 2.4.47  | Added Feast editing feastStacks to FoodStats. Feasts have a Item and Piece component and then an another _Material item. Most people should just edit the normal Item. Can't clone because they are too complicated. </br> Added ashlandProof to ships(pieces) (Majestic request) </br> Added SE cache(ing) for mods like MonsterDB. </br> Added AppendToolTip to items. It's not used too much, but essential when needed (mostly foods)(ignore this for feasts) </br>  Cleaned up some code.
| 2.4.46  | Minor bug fix for extra resource consumption with recipes.
| 2.4.45  | Expanded saving Attack classes for non attack items that still have important fields. Like Hoe and Shields. 
| 2.4.44  | Bug fix for cSExtensionDataList
| 2.4.43  | Update for PieceManager
| 2.4.42  | Bug fix. 
| 2.4.41  | Bog The Witch Update </br> Add support for multiple Piece CSExtensionData. You can now also make (almost) any piece into an extension for a craftingstation. </br> Added support for Pieces to become craftingstations. </br> Added Pickable m_extraDrops stuff </br> Removed unnecessary warning for disabled pieces. </br> Added Hit_Friendly to attack
| 2.4.31  | Expanded upgrade_reqs for recipes levels!  You can now specify completely different recipes for all upgrades. Rejoice, no more hacks. 
| 2.4.28  | Renamed a field to Damage_Multiplier_By_Health_Deficit_Percent </br> Updated Readme with some (light) video tutorial links.
| 2.4.27  | Fix for Primary/Secondary Status Effect. </br> Added `Attack_Start_Noise`(float),       `Attack_Hit_Noise`(float),        `Dmg_Multiplier_Per_Missing_Health`(float),        `Dmg_Multiplier_Per_Total_Health`(float), `Stamina_Return_Per_Missing_HP`(float),     `SelfDamage`(int), `Attack_Kills_Self`(bool)
| 2.4.26  | Fix for rare unarmed attack causing error and no dmg.
| 2.4.25  | Coop sync fix</br> Rewrote how 'ExtraSecurity on Servers' works. Should sync from Servers to clients better now.  </br> Send logs for any issues
| 2.4.24  | PerBurst_Resource_usage bug fix.
| 2.4.23  | Updated PerBurst_Resource_usage and Destroy_Previous_Projectile for misspellings. If you use these, please regenerate your yaml files or rename fields, otherwise can be ignored. </br> Bug fixes for pickables with other plant mods.
| 2.4.22  | Bug fixes for materials. </br> AlanDrake added filterMode to textures for advanced texturing. 
| 2.4.21  | Incoming Big Update sponsored by Lughbaloo: Pickables and Plants! </br> PlantData is saved in pieces, which point to either Pickable or Treebase when matured. When you pick a pickable it becomes an item. You can of course clone and change size/material. Enjoy this wacky expansion.|
| 2.4.13  | Bug fix for attack_status_effect|
| 2.4.12  | Rexabyte fix for darker textures than intended. Also you can now save materials with spaces in the name, using underscores '_' in place of a space. </br> Bug fix for unarmed hits|
| 2.4.11  | Added 'delete' to spawn_on_hit and spawn_on_terrain_hit for items. </br> Added command to save all mobs wackydb_all_creatures in Bulk Folder </br> Separated out attack_status_effect into primary and secondary, custom wackydb patch for it. The normal attack_status_effect will be overwritten if a primary or secondary is set. </br> Added m_jumpModifier as a Vector3. Unnecessary lines can be deleted. </br> Added some more logs for debugmode </br> IG forgot to add a check for zero stamina for staminaReload, added|
| 2.3.9   | Added Attack_status_effect_chance </br> Fixed COOP servers again. I even verfied with Gamepass PC.|
| 2.3.8   | Added SE/se to wackydb_clone, rejoice, you can now change which SE you clone. Allows you to clone SEShield now.</br> Added the ability to delete Attack_status_effect with "delete"|
| 2.3.7   | Changed disabled recipes code again</br> Added back ReloadTime, might work for some things. </br> Added Reload_Eitr_Drain </br> Fix for DedServer load Memory = false.</br> Added Reset_Chain_If_hit, SpawnOnHit, SpawnOnHit_Chance, Raise_Skill_Amount, Skill_Hit_Type, Special_Hit_Skill, Special_Hit_Type to Attack. </br> Added Attach_Override for items|
| 2.3.6   | Possible bug fix for disabled recipes.|
| 2.3.5   | Bug fixes for recipes, disabled and disabledUpgrade </br> Bug fix for double serversync issue, was probably causing server admins a lot of problems on pushing updates without restarts. </br> Changed singleplayer loading yml files to be more consistent with how multiplayer client receives them. Required fields are required. </br>Removed unnecessarily log on respawn|
| 2.3.4   | Bug Fix for recipe.|
| 2.3.3   | Added values for Staff of protection SE or SE Staff_</br> Updated PieceManager </br> Officially added upgrade_reqs and disabledUpgrade for Recipe goodness. Say thank you to Blaxx and Item Manager </br> Added warning if Recipes don't find req item </br> Fixed using actual recipes like Recipe_Bronze5 </br> Added notOnWood for pieces|
| 2.3.1   | Adjust font sizeMin for Categories. </br> Added AllowAllItems for portal Pieces </br> Added Fireplace for pieces - Infinite Fuel now </br> Fixed Mock System again.|
| 2.3.0   | Update for Ashlands. </br>Removed m_baseItemsStaminaModifier, added a lot of other StaminaModfiers. </br> Updated PieceManager|
| 2.2.6   | Removed Reloadtime(broken) and replaced it with ReloadTimeMultiplier for crossbow.|
| 2.2.5   | Added snapshotRotation and snapshotOnMaterialChange for items. <br> Fix for some cloned pieces </br> Fix for reloading RecipeMaxStationLvl </br> Added incineratorData conversion for obliterator, you can now make obliterator into a recycler if you want.|
| 2.2.2   | Bug Fix for SE_Effects with generated PLUS effects|
| 2.2.1   | Added BaseItemStamina for statmods </br> Add StaffSkelton attack  </br> Fix Bug on PLUS effects|
| 2.2.0   | Decent sized Update: Fix for cloned creatures replacing main creatures name </br> Enabled piece snapshot again, hopefully it works well this time. Added a command wackydb_snapshot for pieces </br> Vastly expanded effect capabilities. Old Effects will work, but generate new yamls for more features. </br> Added Remove to piece conversion list allows you to disable an input and not forcing me to clear the whole list. Now the list shouldn't conflict with additional mods.|
| 2.1.7   | Update Readme a bit. </br> Made it so some pieces didn't reload twice.|
| 2.1.6   | Bug fix for cloned pieces on relog. </br> Thx to OrianaVenture for updated icon|
| 2.1.5   | Added API for Clone mapping to orginal prefab. </br> Adjustment for Epicloot+wackydb on quitting|
| 2.1.4   | Happy Halloween, this update is for the spooky people that use "," as decimal delimiters, resulting in crazy big sizes of items/pieces. </br> SizeMultiplier is now seperated with \| </br> Updated a Priority for loading|
| 2.1.3   | Added custom SS messsage back </br> added a 4th layer to piece search </br> Separated out sizeMultiplier for x,y,z or just one value </br> Added a check for transplier at closeout for a few people that hang.</br> Updated Refs for new Bepinex </br> Changed loading order again|
| 2.1.2   | Updated recipeGet, Removed recipe quality (it's a good idea, but I didn't like how it was implemented**).** </br> Took out my custom ServerSync temporarily to test a bug, it won't display message 0.0.1|
| 2.1.1   | Change a priorty. </br> Minimized the chance of a recipe consuming double resources with cfc. It will still happen with cfc, if a recipe has quality of > 1|
| 2.1.0   | Bug fix, changed color on messages from lime to red|
| 2.0.9   | Bug Fix|
| 2.0.8   | Updated ServerSync, Piecemanger, Patch update for 217.24 </br> Fix bug for recipes consuming resources twice.|
| 2.0.7   | Fixed effects not following you. </br> Add beehive data to pieces. </br> Fix for dedicated servers not loading data. Moved up reload for dedicated servers. </br> Changed log messages, added more warnings.</br> Added more checks for cloned cache. </br> Fix for mock items.|
| 2.0.6   | Big bug fix for servers. Moved main loading to an even later point. Added SizeMultiplier to cache, for extra sized cached weapons.|
| 2.0.5   | Bug fix for a cache item error on updateitemhash.|
| 2.0.4   | Added ConsumableStatusEffect to items.  </br>Hovernames for cloned doors. </br> Added Sap and Fermentor Section to pieces. </br>  BIG - Moved main loading to a later point for more pieces to be found. </br> Reduced bug counts with disabling pieces. </br> Known bug: moving from one hammer to another hammer, might require disabling orginal and cloning. </br>  Fixed Mock items for the adventurous few, added example for mock bike.|
| 2.0.3   | Bug fix for cloned items being deleted for some people.  Fix for piece disabling, disabling already placed pieces - whoops|
| 2.0.2   | Bug fix for cloned pieces being deleted at logout and login -sorry|
| 2.0.1   | First Release of 2.0 <br/>|
| 2.0.0   | 2.0.0 - Lots of betas <br/>|