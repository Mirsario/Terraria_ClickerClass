using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ClickerClass.Items
{
	[AutoloadEquip(EquipType.Body)]
	public class MotherboardSuit : ClickerItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Motherboard Suit");
			Tooltip.SetDefault("Increases click damage by 6%");
		}

		public override void SetDefaults()
		{
			isClicker = true;
			item.width = 18;
			item.height = 18;
			item.value = 30000;
			item.rare = 3;
			item.defense = 8;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetModPlayer<ClickerPlayer>().clickerDamage += 0.06f;
		}
		
		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddRecipeGroup("ClickerClass:SilverBar", 25);
			recipe.AddIngredient(ItemID.Wire, 75);
			recipe.AddTile(TileID.Anvils);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}