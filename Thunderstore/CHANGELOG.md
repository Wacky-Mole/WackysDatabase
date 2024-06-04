| Version | Changes                                                                                                                                                                                                                                                                                                                                |
|----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 2.0.0 | 2.0.0 - Lots of betas <br/>
| 2.0.1 | First Release of 2.0 <br/>
| 2.0.2 | Bug fix for cloned pieces being deleted at logout and login -sorry  <br/>
| 2.0.3 | Bug fix for cloned items being deleted for some people.  Fix for piece disabling, disabling already placed pieces - whoops  <br/>
| 2.0.4 | Added ConsumableStatusEffect to items.  </br>Hovernames for cloned doors. </br> Added Sap and Fermentor Section to pieces. </br>  BIG - Moved main loading to a later point for more pieces to be found. </br> Reduced bug counts with disabling pieces. </br> Known bug: moving from one hammer to another hammer, might require disabling orginal and cloning. </br>  Fixed Mock items for the adventurous few, added example for mock bike.
| 2.0.5 | Bug fix for a cache item error on updateitemhash. 
| 2.0.6 | Big bug fix for servers. Moved main loading to an even later point. Added SizeMultiplier to cache, for extra sized cached weapons. 
| 2.0.7 | Fixed effects not following you. </br> Add beehive data to pieces. </br> Fix for dedicated servers not loading data. Moved up reload for dedicated servers. </br> Changed log messages, added more warnings.</br> Added more checks for cloned cache. </br> Fix for mock items.
| 2.0.8 | Updated ServerSync, Piecemanger, Patch update for 217.24 </br> Fix bug for recipes consuming resources twice. 
| 2.0.9 | Bug Fix
| 2.1.0 | Bug fix, changed color on messages from lime to red
| 2.1.1 | Change a priorty. </br> Minimized the chance of a recipe consuming double resources with cfc. It will still happen with cfc, if a recipe has quality of > 1
| 2.1.2 | Updated recipeGet, Removed recipe quality (it's a good idea, but I didn't like how it was implemented**).** </br> Took out my custom ServerSync temporarily to test a bug, it won't display message 0.0.1
| 2.1.3 | Added custom SS messsage back </br> added a 4th layer to piece search </br> Separated out sizeMultiplier for x,y,z or just one value </br> Added a check for transplier at closeout for a few people that hang.</br> Updated Refs for new Bepinex </br> Changed loading order again
| 2.1.4 | Happy Halloween, this update is for the spooky people that use "," as decimal delimiters, resulting in crazy big sizes of items/pieces. </br> SizeMultiplier is now seperated with \| </br> Updated a Priority for loading
| 2.1.5 | Added API for Clone mapping to orginal prefab. </br> Adjustment for Epicloot+wackydb on quitting </br> 
| 2.1.6 | Bug fix for cloned pieces on relog. </br> Thx to OrianaVenture for updated icon
| 2.1.7 | Update Readme a bit. </br> Made it so some pieces didn't reload twice. 
| 2.2.0 | Decent sized Update: Fix for cloned creatures replacing main creatures name </br> Enabled piece snapshot again, hopefully it works well this time. Added a command wackydb_snapshot for pieces </br> Vastly expanded effect capabilities. Old Effects will work, but generate new yamls for more features. </br> Added Remove to piece conversion list allows you to disable an input and not forcing me to clear the whole list. Now the list shouldn't conflict with additional mods.
| 2.2.1 | Added BaseItemStamina for statmods </br> Add StaffSkelton attack  </br> Fix Bug on PLUS effects
| 2.2.2 | Bug Fix for SE_Effects with generated PLUS effects 
| 2.2.5 | Added snapshotRotation and snapshotOnMaterialChange for items. <br> Fix for some cloned pieces </br> Fix for reloading RecipeMaxStationLvl </br> Added incineratorData conversion for obliterator, you can now make obliterator into a recycler if you want.
| 2.2.6 | Removed Reloadtime(broken) and replaced it with ReloadTimeMultiplier for crossbow. 
| 2.3.0 | Update for Ashlands. </br>Removed m_baseItemsStaminaModifier, added a lot of other StaminaModfiers. </br> Updated PieceManager
| 2.3.1 | Adjust font sizeMin for Categories. </br> Added AllowAllItems for portal Pieces </br> Added Fireplace for pieces - Infinite Fuel now </br> Fixed Mock System again.
| 2.3.3 | Added values for Staff of protection SE or SE Staff_Sheild </br> Updated PieceManager </br> Officially added upgrade_reqs and disabledUpgrade for Recipe goodness. Say thank you to Blaxx and Item Manager </br> Added warning if Recipes don't find req item </br> Fixed using actual recipes like Recipe_Bronze5 </br> Added notOnWood for pieces
| 2.3.4 | Bug Fix for recipe. A bug still remains with items that have upgrade_reqs recipe showing up at start.

