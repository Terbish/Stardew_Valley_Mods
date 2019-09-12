using System.Collections.Generic;
using System.Linq;
using Revitalize.Framework.Utilities;
using StardewValley;

namespace Revitalize.Framework.Crafting
{
    /// <summary>
    /// A crafting recipe.
    /// </summary>
    public class Recipe
    {
        /// <summary>
        /// The ingredients necessary to craft this recipe.
        /// </summary>
        public List<CraftingRecipeComponent> ingredients;
        /// <summary>
        /// The items produced by this recipe.
        /// </summary>
        public List<CraftingRecipeComponent> outputs;

        /// <summary>
        /// The item that is displayed for the crafting recipe.
        /// </summary>
        private Item displayItem;

        public Item DisplayItem
        {
            get => this.displayItem ?? this.outputs.ElementAt(0).item;
            set => this.displayItem = value;
        }

        /// <summary>
        /// The description for the crafting recipe.
        /// </summary>
        public string outputDescription;
        /// <summary>
        /// The name for the crafting recipe.
        /// </summary>
        public string outputName;

        /// <summary>
        /// The stats that this recipe costs. Magic, HP, stamina, gold, etc.
        /// </summary>
        public StatCost statCost;

        /// <summary>
        /// The number of in-game minutes it takes to craft this item.
        /// </summary>
        public int timeToCraft;

        public Recipe() { }

        /// <summary>Constructor for single item output.</summary>
        /// <param name="inputs">All the ingredients required to make the output.</param>
        /// <param name="output">The item given as output with how many</param>
        public Recipe(List<CraftingRecipeComponent> inputs, CraftingRecipeComponent output, StatCost StatCost = null,int TimeToCraft=0)
        {
            this.ingredients = inputs;
            this.DisplayItem = output.item;
            this.outputDescription = output.item.getDescription();
            this.outputName = output.item.DisplayName;
            this.outputs = new List<CraftingRecipeComponent>()
            {
                output
            };
            this.statCost = StatCost ?? new StatCost();
            this.timeToCraft = TimeToCraft;
        }

        public Recipe(List<CraftingRecipeComponent> inputs, List<CraftingRecipeComponent> outputs, string OutputName, string OutputDescription, Item DisplayItem = null, StatCost StatCost = null,int TimeToCraft=0)
        {
            this.ingredients = inputs;
            this.outputs = outputs;
            this.outputName = OutputName;
            this.outputDescription = OutputDescription;
            this.DisplayItem = DisplayItem;
            this.statCost = StatCost ?? new StatCost();
            this.timeToCraft = TimeToCraft;
        }

        /// <summary>Checks if a player contains all recipe ingredients.</summary>
        public bool PlayerContainsAllIngredients()
        {
            return this.InventoryContainsAllIngredient(Game1.player.Items.ToList());
        }

        /// <summary>Checks if a player contains a recipe ingredient.</summary>
        public bool PlayerContainsIngredient(CraftingRecipeComponent pair)
        {
            return this.InventoryContainsIngredient(Game1.player.Items.ToList(), pair);
        }

        /// <summary>Checks if an inventory contains all items.</summary>
        public bool InventoryContainsAllIngredient(IList<Item> items)
        {
            foreach (CraftingRecipeComponent pair in this.ingredients)
                if (!this.InventoryContainsIngredient(items, pair)) return false;
            return true;
        }

        /// <summary>Checks if an inventory contains an ingredient.</summary>
        public bool InventoryContainsIngredient(IList<Item> items, CraftingRecipeComponent pair)
        {
            foreach (Item i in items)
            {
                if (i != null && this.ItemEqualsOther(i, pair.item) && pair.requiredAmount <= i.Stack)
                    return true;
            }
            return false;
        }

        /// <summary>Checks roughly if two items equal each other.</summary>
        public bool ItemEqualsOther(Item self, Item other)
        {
            return
                self.Name == other.Name
                && self.getCategoryName() == other.getCategoryName()
                && self.GetType() == other.GetType();
        }

        /// <summary>
        /// Consumes all of the ingredients for the recipe.
        /// </summary>
        /// <param name="from"></param>
        public void consume(ref IList<Item> from)
        {
            if (this.InventoryContainsAllIngredient(from) == false)
                return;

            InventoryManager manager = new InventoryManager(from);
            List<Item> removalList = new List<Item>();
            foreach (CraftingRecipeComponent pair in this.ingredients)
            {
                foreach (Item item in manager.items)
                {
                    if (item == null) continue;
                    if (this.ItemEqualsOther(item, pair.item))
                    {
                        if (item.Stack == pair.requiredAmount)
                            removalList.Add(item); //remove the item
                        else
                            item.Stack -= pair.requiredAmount; //or reduce the stack size.
                    }
                }
            }

            foreach (var v in removalList)
                manager.items.Remove(v);
            removalList.Clear();
            from = manager.items;
        }

        /// <summary>
        /// Produces outputs for the crafting recipe.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="dropToGround"></param>
        /// <param name="isPlayerInventory"></param>
        public void produce(ref IList<Item> to, bool dropToGround = false, bool isPlayerInventory = false)
        {
            var manager = isPlayerInventory
                ? new InventoryManager(new List<Item>())
                : new InventoryManager(to);
            foreach (CraftingRecipeComponent pair in this.outputs)
            {
                Item item = pair.item.getOne();
                item.addToStack(pair.requiredAmount - 1);
                bool added = manager.addItem(item);
                if (!added && dropToGround)
                    Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.getDirection());
            }
            to = manager.items;
        }


        /// <summary>
        /// Consumes all ingredients in given inventory and adds in outputs to the other given inventory.
        /// </summary>
        /// <param name="from">The inventory to take ingredients from.</param>
        /// <param name="to">The inventory to put outputs into.</param>
        /// <param name="dropToGround">Should this item be dropped to the ground when crafted?</param>
        /// <param name="isPlayerInventory">Checks to see if the invventory is the player's</param>
        public void craft(ref IList<Item> from, ref IList<Item> to, bool dropToGround = false, bool isPlayerInventory = false)
        {
            InventoryManager manager = new InventoryManager(to);
            if (manager.ItemCount + this.outputs.Count >= manager.capacity)
            {
                if (isPlayerInventory)
                    Game1.showRedMessage("Inventory Full");
                else return;
            }
            this.consume(ref from);
            this.produce(ref to, dropToGround, isPlayerInventory);
        }

        /// <summary>
        /// Actually crafts the recipe.
        /// </summary>
        public void craft()
        {
            IList<Item> playerItems = Game1.player.Items;
            IList<Item> outPutItems = new List<Item>();
            this.craft(ref playerItems, ref outPutItems, true, true);

            Game1.player.Items = playerItems; //Set the items to be post consumption.
            foreach (Item I in outPutItems)
                Game1.player.addItemToInventory(I); //Add all items produced.
            this.statCost.payCost();
        }

        public bool PlayerCanCraft()
        {
            return this.PlayerContainsAllIngredients() && this.statCost.canSafelyAffordCost();
        }

        public bool CanCraft(IList<Item> items)
        {
            return this.InventoryContainsAllIngredient(items);
        }
    }
}
