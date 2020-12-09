using ClickerClass.Dusts;
using ClickerClass.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ClickerClass.Items.Weapons.Clickers
{
	public class MiceClicker : ClickerWeapon
	{
		public override void SetStaticDefaults()
		{
			base.SetStaticDefaults();

			ClickEffect.Collision = ClickerSystem.RegisterClickEffect(mod, "Collision", null, null, 10, new Color(150, 150, 225), delegate (Player player, Vector2 position, int type, int damage, float knockBack)
			{
				Main.PlaySound(SoundID.Item, (int)Main.MouseWorld.X, (int)Main.MouseWorld.Y, 105);
				for (int k = 0; k < 8; k++)
				{
					Projectile.NewProjectile(Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), ModContent.ProjectileType<MiceClickerPro>(), (int)(damage * 0.5f), knockBack, player.whoAmI);
				}
				for (int k = 0; k < 10; k++)
				{
					Dust dust = Dust.NewDustDirect(Main.MouseWorld, 8, 8, ModContent.DustType<MiceDust>(), Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 0, default, 1.25f);
					dust.noGravity = true;
				}
			});
		}

		public override void SetDefaults()
		{
			base.SetDefaults();
			SetRadius(item, 6f);
			SetColor(item, new Color(150, 150, 225));
			SetDust(item, ModContent.DustType<MiceDust>());
			AddEffect(item, ClickEffect.Collision);

			item.damage = 94;
			item.width = 30;
			item.height = 30;
			item.knockBack = 1f;
			item.value = Item.sellPrice(0, 5, 0, 0);
			item.rare = 10;
		}

		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ModContent.ItemType<MiceFragment>(), 18);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
