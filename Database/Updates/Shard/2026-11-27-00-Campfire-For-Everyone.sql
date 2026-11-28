/*
 * Campfires for everyone: Build Campfire (1053953) no longer needs the Cooking skill to place (world-db dropped its
 * UseRequiresSkill). Every saved item keeps its own copy of its properties, so the Build Campfires players already
 * own still ask for Cooking. This clears UseRequiresSkill (PropertyInt 366) from them.
 *
 * Cooking food at a campfire still needs Cooking: the campfire's own emotes check for it, not this property.
 *
 * Safe to run more than once.
 */

-- Preview: how many Build Campfires still need Cooking
-- SELECT COUNT(*) FROM biota_properties_int bpi JOIN biota b ON b.id = bpi.object_Id
--  WHERE b.weenie_Class_Id = 1053953 AND bpi.`type` = 366;

DELETE bpi
  FROM biota_properties_int bpi
  JOIN biota b ON b.id = bpi.object_Id
 WHERE b.weenie_Class_Id = 1053953
   AND bpi.`type` = 366;
