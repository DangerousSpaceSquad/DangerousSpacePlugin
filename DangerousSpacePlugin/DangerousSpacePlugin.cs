using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DangerousSpacePlugin
{
    // Add BepInEx Dependencies:
    // API for adding items
    [BepInDependency(ItemAPI.PluginGUID)]
    // API for localizing to multiple languages
    [BepInDependency(LanguageAPI.PluginGUID)]
    // Mandatory for plugins. Registers with the modloader, I think.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // BepIn will load all classes inheriting from BaseUnityPlugin as mods
    // For reference, BaseUnityPlugin inherits from Unity's MonoBehavior, so you can write plugins like MonoBehaviors.
    public class DangerousSpacePlugin : BaseUnityPlugin
    {
        // Metadata for the plugin.
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "DangerousSpaceSquad";
        public const string PluginName = "DangerousSpacePlugin";
        public const string PluginVersion = "0.0.1";

        // Declare the item as a field of the class.
        private static ItemDef myItemDef;

        public void Awake()
        {
            // Initialize the custom logger
            Log.Init(Logger);

            // Define the item
            myItemDef = ScriptableObject.CreateInstance<ItemDef>();

            // Set the metadata of the item. Here, we're loading it from the localization file depending on user language settings.
            myItemDef.name = "EXAMPLE_CLOAKONKILL_NAME";
            myItemDef.nameToken = "EXAMPLE_CLOAKONKILL_NAME";
            myItemDef.pickupToken = "EXAMPLE_CLOAKONKILL_PICKUP";
            myItemDef.descriptionToken = "EXAMPLE_CLOAKONKILL_DESC";
            myItemDef.loreToken = "EXAMPLE_CLOAKONKILL_LORE";

            // Set the tier of the item.
            // The warning disabling is necessary because this is a private member. There's probably a better way around this problem.
#pragma warning disable Publicizer001
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            // Set the assets for the item. These specific ones are question mark icons.
            myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();

            // Can the item be removed from the inventory (e.g. scrapper)
            myItemDef.canRemove = true;
            // Is the item hidden from the player (e.g. hidden player buffs on Drizzle difficulty)
            myItemDef.hidden = false;

            // How the item displays on the character.
            // If you choose to set this up, it's recommended that you use a helper mod to do it, as it's tricky to do manually.
            var displayRules = new ItemDisplayRuleDict(null);

            // Register the item to R2API.
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));

            // After this point, you'll want to register some code to run on some event, most likely.
            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManager_onCharacterDeathGlobal;
        }
        private void GlobalEventManager_onCharacterDeathGlobal(DamageReport report)
        {
            // If a character was killed by the world, we shouldn't do anything.
            if (!report.attacker || !report.attackerBody)
            {
                return;
            }

            var attackerCharacterBody = report.attackerBody;

            // We need an inventory to do check for our item
            if (attackerCharacterBody.inventory)
            {
                // Store the amount of our item we have
                var garbCount = attackerCharacterBody.inventory.GetItemCount(myItemDef.itemIndex);
                if (garbCount > 0 &&
                    // Roll for our 50% chance.
                    Util.CheckRoll(50, attackerCharacterBody.master))
                {
                    // Since we passed all checks, we now give our attacker the cloaked buff.
                    // Note how we are scaling the buff duration depending on the number of the custom item in our inventory.
                    attackerCharacterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, 3 + garbCount);
                }
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop our defined item in front of the player.
                Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
