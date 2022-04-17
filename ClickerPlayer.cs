using ClickerClass.Buffs;
using ClickerClass.Items;
using ClickerClass.NPCs;
using ClickerClass.Projectiles;
using ClickerClass.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using ClickerClass.Items.Accessories;

namespace ClickerClass
{
	public partial class ClickerPlayer : ModPlayer
	{
		/// <summary>
		/// Static bool, not unloaded, carry over into other worlds if needed (no message if you enter world a second time in a game session)
		/// </summary>
		public static bool enteredWorldOnceThisSession = false;

		//Key presses
		public double pressedAutoClick;
		public int clickerClassTime = 0;

		//-Clicker-
		//Misc
		public Color clickerRadiusColor = Color.White;
		/// <summary>
		/// Cached clickerRadiusColor for draw
		/// </summary>
		public Color clickerRadiusColorDraw = Color.Transparent;
		public float ClickerRadiusColorMultiplier => clickerRadiusRangeAlpha * clickerRadiusSwitchAlpha;
		/// <summary>
		/// Visual indicator that the cursor is inside clicker radius
		/// </summary>
		public bool clickerInRange = false;
		/// <summary>
		/// Visual indicator that the cursor is inside Motherboard radius
		/// </summary>
		public bool clickerInRangeMotherboard = false;
		public bool GlowVisual => clickerInRange || clickerInRangeMotherboard;
		public bool clickerSelected = false;
		/// <summary>
		/// False if phase reach
		/// </summary>
		public bool clickerDrawRadius = false;
		public const float clickerRadiusSwitchAlphaMin = 0f;
		public const float clickerRadiusSwitchAlphaMax = 1f;
		public const float clickerRadiusSwitchAlphaStep = clickerRadiusSwitchAlphaMax / 40f;
		public float clickerRadiusSwitchAlpha = clickerRadiusSwitchAlphaMin;

		//Gameplay only: not related to player select screen
		public bool CanDrawRadius => !Main.gameMenu && !Player.dead && clickerRadiusSwitchAlpha > clickerRadiusSwitchAlphaMin;

		public const float clickerRadiusRangeAlphaMin = 0.2f;
		public const float clickerRadiusRangeAlphaMax = 0.8f;
		public const float clickerRadiusRangeAlphaStep = clickerRadiusRangeAlphaMax / 20f;
		public float clickerRadiusRangeAlpha = clickerRadiusRangeAlphaMin;

		/// <summary>
		/// Set via hotkey, reset if no autoclick-giving effects are applied (i.e. Hand Cream)
		/// </summary>
		public bool clickerAutoClick = false;
		/// <summary>
		/// Saved amount of clicks done with any clicker, accumulated, fluff
		/// </summary>
		public int clickerTotal = 0;
		/// <summary>
		/// Amount of clicks done, constantly incremented. Used for click effect proccing
		/// </summary>
		public int clickAmount = 0;
		/// <summary>
		/// cps, use Math.Floor if you need as integer
		/// </summary>
		public float clickerPerSecond = 0;
		private const int ClickTimerCount = 60;
		/// <summary>
		/// Keeps track of clicks done in the last <see cref="ClickTimerCount"/> ticks, tracked as separate timers
		/// </summary>
		private List<Ref<float>> clickTimers = new List<Ref<float>>();
		/// <summary>
		/// Amount of money generated by clicker items
		/// </summary>
		public int clickerMoneyGenerated = 0;
		/// <summary>
		/// Used for double tap dash effects
		/// </summary>
		public int clickerDoubleTap = 0;

		//Click effects
		/// <summary>
		/// Used to track effect names that are currently active. Resets every tick
		/// </summary>
		private Dictionary<string, bool> ClickEffectActive = new Dictionary<string, bool>();

		public bool effectHotWings = false;
		public const int EffectHotWingsTimerMax = 70; //Full duration. Damage part excludes the fade time
		public const int EffectHotWingsTimerFadeStart = 30;
		public int effectHotWingsTimer = 0; //Gets set to max, counts down

		public const int EffectHotWingsFrameMax = 4;
		public int effectHotWingsFrame = 0;

		public bool DrawHotWings => effectHotWingsTimer > 0;

		//Out of combat
		public const int OutOfCombatTimeMax = 300;
		public bool OutOfCombat => outOfCombatTimer <= 0;
		public int outOfCombatTimer = 0;

		//Armor
		public int setAbilityDelayTimer = 0;
		public float setMotherboardRatio = 0f;
		public float setMotherboardAngle = 0f;
		/// <summary>
		/// Calculated after clickerRadius is calculated, and if the Motherboard set is worn
		/// </summary>
		public Vector2 setMotherboardPosition = Vector2.Zero;
		public float setMotherboardAlpha = 0f;
		public int setMotherboardFrame = 0;
		public bool setMotherboardFrameShift = false;
		public bool setMotherboard = false;
		public bool SetMotherboardDraw => setMotherboard && setMotherboardRatio > 0;

		public bool setMice = false;
		public bool setPrecursor = false;
		public bool setOverclock = false;
		public bool setRGB = false;
		
		public int setPrecursorTimer = 0;

		//Acc
		[Obsolete("Use HasClickEffect(\"ClickerClass:ChocolateChip\") and EnableClickEffect(\"ClickerClass:ChocolateChip\") instead", false)]
		public bool accChocolateChip = false;
		public bool accEnchantedLED = false;
		public bool accEnchantedLED2 = false; //different visuals
		public bool accHandCream = false;
		[Obsolete("Use HasClickEffect(\"ClickerClass:StickyKeychain\") and EnableClickEffect(\"ClickerClass:StickyKeychain\") instead", false)]
		public bool accStickyKeychain = false;
		public Item accSMedalItem = null;
		public bool AccSMedal => accSMedalItem != null && !accSMedalItem.IsAir;
		public Item accFMedalItem = null;
		public bool AccFMedal => accFMedalItem != null && !accFMedalItem.IsAir;
		public bool accGlassOfMilk = false;
		public Item accCookieItem = null;
		public bool accCookie = false;
		public bool accCookie2 = false; //different visuals
		public bool accClickingGlove = false;
		public bool accAncientClickingGlove = false;
		public bool accRegalClickingGlove = false;
		public bool accPortableParticleAccelerator = false; //"is wearing"
		public bool accPortableParticleAccelerator2 = false; //"is active", client only
		public bool IsPortableParticleAcceleratorActive => accPortableParticleAccelerator && accPortableParticleAccelerator2;
		public bool accGoldenTicket = false;
		public bool accTriggerFinger = false;
		public bool accIcePack = false;
		public bool accMouseTrap = false;
		public Item accPaperclipsItem = null;
		public bool AccPaperclips => accPaperclipsItem != null && !accPaperclipsItem.IsAir;
		public bool accHotKeychain = false;
		public bool accHotKeychain2 = false;
		public bool accButtonMasher = false;

		public int accClickingGloveTimer = 0;
		public int accCookieTimer = 0;
		public int accSMedalAmount = 0; //Only updated clientside
		public int accFMedalAmount = 0; //Only updated clientside
		public float accMedalRot = 0f; //Unified rotation for all medals
		public int accPaperclipsAmount = 0;
		public int accHotKeychainTimer = 0;
		public int accHotKeychainAmount = 0;

		//Stats
		/// <summary>
		/// How many less clicks are required to trigger an effect
		/// </summary>
		public int clickerBonus = 0;

		/// <summary>
		/// Multiplier to clicks required to trigger an effect
		/// </summary>
		public float clickerBonusPercent = 1f;

		/// <summary>
		/// Effective clicker radius in pixels when multiplied by 100
		/// </summary>
		public float clickerRadius = 1f;

		/// <summary>
		/// Cached clickerRadius for draw
		/// </summary>
		public float clickerRadiusDraw = 1f;

		/// <summary>
		/// Clicker radius in pixels
		/// </summary>
		public float ClickerRadiusReal => clickerRadius * 100;

		/// <summary>
		/// Clicker draw radius in pixels
		/// </summary>
		public float ClickerRadiusRealDraw => clickerRadiusDraw * 100;

		/// <summary>
		/// Motherboard radius in pixels
		/// </summary>
		public float ClickerRadiusMotherboard => ClickerRadiusReal * 0.5f;

		/// <summary>
		/// Motherboard draw radius in pixels
		/// </summary>
		public float ClickerRadiusMotherboardDraw => ClickerRadiusRealDraw * 0.5f;

		//Helper methods
		/// <summary>
		/// Enables the use of a click effect for this player
		/// </summary>
		/// <param name="name">The unique effect name</param>
		public void EnableClickEffect(string name)
		{
			if (ClickEffectActive.TryGetValue(name, out _))
			{
				ClickEffectActive[name] = true;
			}
		}

		/// <summary>
		/// Enables the use of click effects for this player
		/// </summary>
		/// <param name="names">The unique effect names</param>
		public void EnableClickEffect(IEnumerable<string> names)
		{
			foreach (var name in names)
			{
				EnableClickEffect(name);
			}
		}

		/// <summary>
		/// Checks if the player has a click effect enabled
		/// </summary>
		/// <param name="name">The unique effect name</param>
		/// <returns><see langword="true"/> if enabled</returns>
		public bool HasClickEffect(string name)
		{
			if (ClickEffectActive.TryGetValue(name, out _))
			{
				return ClickEffectActive[name];
			}
			return false;
		}

		/// <summary>
		/// Checks if the player has a click effect enabled
		/// </summary>
		/// <param name="name">The unique effect name</param>
		/// <param name="effect">The effect associated with the name</param>
		/// <returns><see langword="true"/> if enabled</returns>
		public bool HasClickEffect(string name, out ClickEffect effect)
		{
			effect = null;
			if (HasClickEffect(name))
			{
				return ClickerSystem.IsClickEffect(name, out effect);
			}
			return false;
		}

		//Unused yet
		public bool HasAnyClickEffect()
		{
			foreach (var value in ClickEffectActive.Values)
			{
				if (value) return true;
			}
			return false;
		}

		internal void ResetAllClickEffects()
		{
			//Stupid trick to be able to write to a value in a dictionary
			foreach (var key in ClickEffectActive.Keys.ToList())
			{
				ClickEffectActive[key] = false;
			}
		}

		/// <summary>
		/// Call to register a click towards the "clicks per second" and total calculations
		/// </summary>
		internal void AddClick()
		{
			clickTimers.Add(new Ref<float>(1f));
			clickerTotal++;
		}

		/// <summary>
		/// Call to increment the click amount counter used for proccing click effects
		/// </summary>
		internal void AddClickAmount()
		{
			clickAmount++;
		}

		/// <summary>
		/// Manages the click queue and calculates <see cref="clickerPerSecond"/>
		/// </summary>
		private void HandleCPS()
		{
			clickerPerSecond = 0f; //Recalculates every tick

			//Loop from back to front, removing ran out timers and adding existing timers
			for (int i = clickTimers.Count - 1; i > 0; i--)
			{
				clickTimers[i].Value -= 1f / ClickTimerCount; //Decrement timer
				if (clickTimers[i].Value <= 0f)
				{
					clickTimers.RemoveAt(i);
				}
				else
				{
					float value = clickTimers[i].Value;
					value = Math.Clamp(value, 0.3f, 0.75f); //Smooths out the values, with slight bias towards > 0.5f, so that the cps stays on a given value longer if clicks are regular (i.e. autoswing)
					clickerPerSecond += value * 2; //* 2 is because the average click timer "state" is 0.5f
				}
			}
		}

		private void HandleRadiusAlphas()
		{
			if (clickerDrawRadius)
			{
				if (clickerRadiusSwitchAlpha < clickerRadiusSwitchAlphaMax)
				{
					clickerRadiusSwitchAlpha += clickerRadiusSwitchAlphaStep;
				}
				else
				{
					clickerRadiusSwitchAlpha = clickerRadiusSwitchAlphaMax;
				}
			}
			else
			{
				if (clickerRadiusSwitchAlpha > clickerRadiusSwitchAlphaMin)
				{
					clickerRadiusSwitchAlpha -= clickerRadiusSwitchAlphaStep;
				}
				else
				{
					clickerRadiusSwitchAlpha = clickerRadiusSwitchAlphaMin;
				}
			}

			if (GlowVisual)
			{
				if (clickerRadiusRangeAlpha < clickerRadiusRangeAlphaMax)
				{
					clickerRadiusRangeAlpha += clickerRadiusRangeAlphaStep;
				}
				else
				{
					clickerRadiusRangeAlpha = clickerRadiusRangeAlphaMax;
				}
			}
			else
			{
				if (clickerRadiusRangeAlpha > clickerRadiusRangeAlphaMin)
				{
					clickerRadiusRangeAlpha -= clickerRadiusRangeAlphaStep;
				}
				else
				{
					clickerRadiusRangeAlpha = clickerRadiusRangeAlphaMin;
				}
			}
		}

		/// <summary>
		/// Returns the position from the ratio and angle, given the radius in pixels
		/// </summary>
		/// <param name="realRadius">The reference radius</param>
		public Vector2 CalculateMotherboardPosition(float realRadius)
		{
			float length = setMotherboardRatio * realRadius;
			Vector2 direction = setMotherboardAngle.ToRotationVector2();
			return direction * length;
		}

		/// <summary>
		/// Construct ratio and angle from position
		/// </summary>
		public void SetMotherboardRelativePosition(Vector2 position)
		{
			Vector2 toPosition = position - Player.Center;
			float length = toPosition.Length();
			float radius = ClickerRadiusReal;
			float ratio = length / radius;
			if (ratio < 0.6f)
			{
				//Enforce minimal range
				ratio = 0.6f;
			}
			setMotherboardRatio = ratio;
			setMotherboardAngle = toPosition.ToRotation();
		}

		/// <summary>
		/// Dispels the motherboard position
		/// </summary>
		public void ResetMotherboardPosition()
		{
			setMotherboardRatio = 0f;
			setMotherboardAngle = 0f;
		}

		internal int originalSelectedItem;
		internal bool autoRevertSelectedItem = false;

		/// <summary>
		/// Uses the item in the specified index from the players inventory
		/// </summary>
		public void QuickUseItemInSlot(int index)
		{
			if (index > -1 && index < Main.InventorySlotsTotal && Player.inventory[index].type != ItemID.None)
			{
				if (Player.CheckMana(Player.inventory[index], -1, false, false))
				{
					originalSelectedItem = Player.selectedItem;
					autoRevertSelectedItem = true;
					Player.selectedItem = index;
					Player.controlUseItem = true;
					Player.ItemCheck(Player.whoAmI);
				}
				else
				{
					SoundEngine.PlaySound(SoundID.Drip, (int)Player.Center.X, (int)Player.Center.Y, Main.rand.Next(3));
				}
			}
		}

		/// <summary>
		/// Returns the amount of clicks required for an effect of the given name to trigger (defaults to the item's assigned effect). Includes various bonuses
		/// </summary>
		public int GetClickAmountTotal(ClickerItemCore clickerItem, string name)
		{
			//Doesn't go below 1
			int amount = 1;
			if (ClickerSystem.IsClickEffect(name, out ClickEffect effect))
			{
				amount = effect.Amount;
			}
			float percent = Math.Max(0f, clickerBonusPercent);
			int prePercentAmount = Math.Max(1, amount + clickerItem.clickBoostPrefix - clickerBonus);
			return Math.Max(1, (int)(prePercentAmount * percent));
		}

		/// <summary>
		/// Returns the amount of clicks required for the effect of this item to trigger. Includes various bonuses
		/// </summary>
		public int GetClickAmountTotal(Item item, string name)
		{
			return GetClickAmountTotal(item.GetGlobalItem<ClickerItemCore>(), name);
		}

		public override void ResetEffects()
		{
			//-Clicker-
			//Misc
			clickerRadiusColor = Color.White;
			clickerInRange = false;
			clickerInRangeMotherboard = false;
			clickerSelected = false;
			clickerDrawRadius = false;

			//Click Effects
			ResetAllClickEffects();
			effectHotWings = false;

			//Armor
			setMotherboard = false;
			setMice = false;
			setPrecursor = false;
			setOverclock = false;
			setRGB = false;

			//Acc
			accEnchantedLED = false;
			accEnchantedLED2 = false;
			accHandCream = false;
			accSMedalItem = null;
			accFMedalItem = null;
			accGlassOfMilk = false;
			accCookieItem = null;
			accCookie = false;
			accCookie2 = false;
			accClickingGlove = false;
			accAncientClickingGlove = false;
			accRegalClickingGlove = false;
			accPortableParticleAccelerator = false;
			accPortableParticleAccelerator2 = false;
			accGoldenTicket = false;
			accTriggerFinger = false;
			accIcePack = false;
			accMouseTrap = false;
			accPaperclipsItem = null;
			accHotKeychain = false;
			accHotKeychain2 = false;
			accButtonMasher = false;

			//Stats
			clickerBonus = 0;
			clickerBonusPercent = 1f;
			clickerRadius = 1f;
		}

		public override void UpdateAutopause()
		{
			clickerRadius = 1f;
		}

		public override void Initialize()
		{
			clickerTotal = 0;
			clickerMoneyGenerated = 0;
			
			ClickEffectActive = new Dictionary<string, bool>();
			foreach (var name in ClickerSystem.GetAllEffectNames())
			{
				ClickEffectActive.Add(name, false);
			}

			clickTimers = new List<Ref<float>>();
		}

		public override void SaveData(TagCompound tag)
		{
			tag.Add("clickerTotal", clickerTotal);
			tag.Add("clickerMoneyGenerated", clickerMoneyGenerated);
		}

		public override void LoadData(TagCompound tag)
		{
			clickerTotal = tag.GetInt("clickerTotal");
			clickerMoneyGenerated = tag.GetInt("clickerMoneyGenerated");
		}

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			// checks for frozen, webbed and stoned
			if (Player.CCed)
			{
				return;
			}

			if (ClickerClass.AutoClickKey.JustPressed)
			{
				if (Math.Abs(clickerClassTime - pressedAutoClick) > 60)
				{
					pressedAutoClick = clickerClassTime;

					SoundEngine.PlaySound(SoundID.MenuTick, Player.position);
					clickerAutoClick = clickerAutoClick ? false : true;
				}
			}
		}

		public override void PreUpdate()
		{
			if (Player.whoAmI == Main.myPlayer)
			{
				if (autoRevertSelectedItem)
				{
					if (Player.itemTime == 0 && Player.itemAnimation == 0)
					{
						Player.selectedItem = originalSelectedItem;
						autoRevertSelectedItem = false;
					}
				}
			}

			if (Player.whoAmI == Main.myPlayer)
			{
				if (Player.itemTime == 0 && Player.itemAnimation == 0)
				{
					if (accRegalClickingGlove && accClickingGloveTimer > 30)
					{
						QuickUseItemInSlot(Player.selectedItem);
						accClickingGloveTimer = 0;
					}
					else if (accAncientClickingGlove && accClickingGloveTimer > 60)
					{
						QuickUseItemInSlot(Player.selectedItem);
						accClickingGloveTimer = 0;
					}
					else if (accClickingGlove && accClickingGloveTimer > 180)
					{
						QuickUseItemInSlot(Player.selectedItem);
						accClickingGloveTimer = 0;
					}
				}
			}
		}

		public override void PostUpdateEquips()
		{
			clickerClassTime++;
			if (clickerClassTime > 36000)
			{
				clickerClassTime = 0;
			}

			if (!accHandCream && !accIcePack)
			{
				clickerAutoClick = false;
			}

			if (setAbilityDelayTimer > 0)
			{
				setAbilityDelayTimer--;
			}

			if (!setMotherboard)
			{
				setMotherboardPosition = Vector2.Zero;
				setMotherboardRatio = 0f;
				setMotherboardAngle = 0f;
			}
			else
			{
				setMotherboardAlpha += !setMotherboardFrameShift ? 0.025f : -0.025f;
				if (setMotherboardAlpha >= 1f)
				{
					setMotherboardFrameShift = true;
				}

				if (setMotherboardFrameShift && setMotherboardAlpha <= 0.25f)
				{
					setMotherboardFrame++;
					if (setMotherboardFrame >= 4)
					{
						setMotherboardFrame = 0;
					}
					setMotherboardFrameShift = false;
				}
			}
			
			clickerRadius += 0.005f * accFMedalAmount;

			Item heldItem = Player.HeldItem;
			if (ClickerSystem.IsClickerWeapon(heldItem, out ClickerItemCore clickerItem))
			{
				EnableClickEffect(clickerItem.itemClickEffects);
				clickerSelected = true;
				clickerDrawRadius = true;
				if (HasClickEffect(ClickEffect.PhaseReach))
				{
					clickerDrawRadius = false;
				}

				if (clickerItem.radiusBoost > 0f)
				{
					clickerRadius += clickerItem.radiusBoost;
				}

				if (clickerItem.radiusBoostPrefix > 0f)
				{
					clickerRadius += clickerItem.radiusBoostPrefix;
				}

				//Cache for draw
				clickerRadiusDraw = clickerRadius;

				//collision
				float radiusSQ = ClickerRadiusReal * ClickerRadiusReal;
				if (Vector2.DistanceSquared(Main.MouseWorld, Player.Center) < radiusSQ && Collision.CanHit(new Vector2(Player.Center.X, Player.Center.Y - 12), 1, 1, Main.MouseWorld, 1, 1))
				{
					clickerInRange = true;
				}
				if (setMotherboard)
				{
					//Important: has to be after final clickerRadius calculation because it depends on it
					setMotherboardPosition = Player.Center + CalculateMotherboardPosition(ClickerRadiusReal);
				}

				//collision
				radiusSQ = ClickerRadiusMotherboard * ClickerRadiusMotherboard;
				if (Vector2.DistanceSquared(Main.MouseWorld, setMotherboardPosition) < radiusSQ && Collision.CanHit(setMotherboardPosition, 1, 1, Main.MouseWorld, 1, 1))
				{
					clickerInRangeMotherboard = true;
				}
				clickerRadiusColor = clickerItem.clickerRadiusColor;

				//Cache for draw
				clickerRadiusColorDraw = Color.Lerp(clickerRadiusColorDraw, clickerRadiusColor, clickerRadiusSwitchAlpha);

				//Glove acc
				if (!OutOfCombat && (accClickingGlove || accAncientClickingGlove || accRegalClickingGlove))
				{
					accClickingGloveTimer++;
				}
				else
				{
					accClickingGloveTimer = 0;
				}

				if (setPrecursor && !OutOfCombat && clickerInRange)
				{
					setPrecursorTimer++;
					if (setPrecursorTimer > 10)
					{
						if (Main.myPlayer == Player.whoAmI)
						{
							int damage = Math.Max(1, (int)(heldItem.damage * 0.2f));

							Projectile.NewProjectile(Player.GetSource_FromThis(context: "SetBonus_Precursor"), Main.MouseWorld.X + 8, Main.MouseWorld.Y + 11, 0f, 0f, ModContent.ProjectileType<PrecursorPro>(), damage, 0f, Player.whoAmI);
						}
						setPrecursorTimer = 0;
					}
				}
				else
				{
					setPrecursorTimer = 0;
				}
			}
			else
			{
				clickerRadiusColorDraw = Color.Lerp(Color.Transparent, clickerRadiusColorDraw, clickerRadiusSwitchAlpha);
			}

			if (Player.HasBuff(ModContent.BuffType<Haste>()))
			{
				Player.armorEffectDrawShadow = true;
			}
			
			if (clickerDoubleTap > 0)
			{
				clickerDoubleTap--;
			}
			
			//Clicker Effects
			//Hot Wings
			if (effectHotWings && Player.grappling[0] == -1 && !Player.tongued)
			{
				int dashDir = 0;
				if (Player.controlRight && Player.releaseRight)
				{
					clickerDoubleTap += clickerDoubleTap > 0 ? 0 : 15;
					if (clickerDoubleTap < 15 && clickerDoubleTap > 0)
					{
						dashDir = 1;
					}
				}
				else if (Player.controlLeft && Player.releaseLeft)
				{
					clickerDoubleTap += clickerDoubleTap > 0 ? 0 : 15;
					if (clickerDoubleTap < 15 && clickerDoubleTap > 0)
					{
						dashDir = -1;
					}
				}

				if (Math.Abs(dashDir) == 1)
				{
					effectHotWingsTimer = EffectHotWingsTimerMax;

					SoundEngine.PlaySound(SoundID.Item, (int)Player.position.X, (int)Player.position.Y, 73);
					Player.ClearBuff(ModContent.BuffType<HotWingsBuff>());
					//if (Player.velocity.Y > 0f) Player.velocity.Y = 0f;
					//if (Player.velocity.X < 0f) Player.velocity.X = 0f;
					Player.velocity.Y -= 6f;
					Player.velocity.X = dashDir * 12f;
					Player.ChangeDir(dashDir);

					//Vanilla code
					Point point3 = (Player.Center + new Vector2(dashDir * Player.width / 2 + 2, Player.gravDir * -Player.height / 2f + Player.gravDir * 2f)).ToTileCoordinates();
					Point point4 = (Player.Center + new Vector2(dashDir * Player.width / 2 + 2, 0f)).ToTileCoordinates();
					if (WorldGen.SolidOrSlopedTile(point3.X, point3.Y) || WorldGen.SolidOrSlopedTile(point4.X, point4.Y))
					{
						Player.velocity.X /= 2f;
					}

					for (int k = 0; k < 15; k++)
					{
						Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, 174, Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f), 0, default, 1.25f);
						dust.noGravity = true;
						dust.noLight = true;
					}
				}
			}

			if (effectHotWingsTimer > 0)
			{
				effectHotWingsTimer--;

				if (effectHotWingsTimer % 6 == 0)
				{
					effectHotWingsFrame++;
					if (effectHotWingsFrame >= EffectHotWingsFrameMax)
					{
						effectHotWingsFrame = 0;
					}
				}
			}
			else
			{
				effectHotWingsFrame = 0;
			}

			//Acc
			//Cookie acc
			if (accCookieItem != null && !accCookieItem.IsAir && (accCookie || accCookie2) && clickerSelected)
			{
				accCookieTimer++;
				if (Player.whoAmI == Main.myPlayer && accCookieTimer > 600)
				{
					int radius = (int)(95 * clickerRadius);
					if (radius > 350)
					{
						radius = 350;
					}

					//Circles give me a damn headache...
					double r = radius * Math.Sqrt(Main.rand.NextFloat(0f, 1f));
					double theta = Main.rand.NextFloat(0f, 1f) * MathHelper.TwoPi;
					double xOffset = Player.Center.X + r * Math.Cos(theta);
					double yOffset = Player.Center.Y + r * Math.Sin(theta);

					int frame = 0;
					if (accCookie2 && Main.rand.NextFloat() <= 0.1f)
					{
						frame= 1;
					}
					Projectile.NewProjectile(new EntitySource_ItemUse(Player, accCookieItem), (float)xOffset, (float)yOffset, 0f, 0f, ModContent.ProjectileType<CookiePro>(), 0, 0f, Player.whoAmI, frame);

					accCookieTimer = 0;
				}

				//Cookie Click
				if (Player.whoAmI == Main.myPlayer)
				{
					for (int i = 0; i < 1000; i++)
					{
						Projectile cookieProjectile = Main.projectile[i];

						if (cookieProjectile.active && cookieProjectile.type == ModContent.ProjectileType<CookiePro>() && cookieProjectile.owner == Player.whoAmI)
						{
							if (Main.mouseLeft && Main.mouseLeftRelease && cookieProjectile.DistanceSQ(Main.MouseWorld) < 30 * 30)
							{
								if (cookieProjectile.ai[0] == 1f)
								{
									SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 4);
									Player.AddBuff(ModContent.BuffType<CookieBuff>(), 600);
									Player.HealLife(10);
									for (int k = 0; k < 10; k++)
									{
										Dust dust = Dust.NewDustDirect(cookieProjectile.Center, 20, 20, 87, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 0, default, 1.15f);
										dust.noGravity = true;
									}
								}
								else
								{
									SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 2);
									Player.AddBuff(ModContent.BuffType<CookieBuff>(), 300);
									for (int k = 0; k < 10; k++)
									{
										Dust dust = Dust.NewDustDirect(cookieProjectile.Center, 20, 20, 0, Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4f, 4f), 75, default, 1.5f);
										dust.noGravity = true;
									}
								}
								cookieProjectile.Kill();
							}
						}
					}
				}
			}

			//Portable Particle Accelerator acc
			if (accPortableParticleAccelerator && Main.myPlayer == Player.whoAmI)
			{
				float radius = ClickerRadiusReal * 0.5f;
				if (Player.DistanceSQ(Main.MouseWorld) < radius * radius)
				{
					accPortableParticleAccelerator2 = true;
				}
			}

			if (IsPortableParticleAcceleratorActive)
			{
				Player.GetDamage<ClickerDamage>().Flat += 8;
			}
			
			//Effects related to having cursor within the radius
			if (Player.whoAmI == Main.myPlayer)
			{
				//Balloon Defense effect
				if (clickerInRange)
				{
					int balloonType = ModContent.ProjectileType<BalloonClickerPro>();
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						Projectile balloonProj = Main.projectile[i];

						if (balloonProj.active && clickerSelected && balloonProj.owner == Player.whoAmI && balloonProj.type == balloonType && balloonProj.ai[0] == 0f && balloonProj.ModProjectile is BalloonClickerPro balloon && !balloon.hasChanged)
						{
							if (Main.mouseLeft && Main.mouseLeftRelease && balloonProj.DistanceSQ(new Vector2(Main.MouseWorld.X, Main.MouseWorld.Y + 40)) < 30 * 30)
							{
								balloonProj.ai[0] = 1f; //Handled in the AI
							}
						}
					}
				}
				
				//S Medal effect
				if (accSMedalAmount < 200 && clickerSelected && AccSMedal)
				{
					int sMedalType = ModContent.ProjectileType<SMedalPro>();
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						Projectile medalProj = Main.projectile[i];

						if (medalProj.active && medalProj.owner == Player.whoAmI && medalProj.type == sMedalType)
						{
							float len = (medalProj.Size / 2f).LengthSquared() * 0.78f; //Circle inside the projectile hitbox
							if (medalProj.DistanceSQ(Main.MouseWorld) < len)
							{
								accSMedalAmount++;
								medalProj.ai[1] = 1f;
								Vector2 offset = new Vector2(Main.rand.Next(-20, 21), Main.rand.Next(-20, 21));
								Dust dust = Dust.NewDustDirect(Main.MouseWorld + offset, 8, 8, 86, Scale: 1.25f);
								dust.noGravity = true;
								dust.velocity = -offset * 0.05f;
							}
						}
					}
				}
				
				//F Medal effect
				if (accFMedalAmount < 200 && clickerSelected && AccFMedal)
				{
					int fMedalType = ModContent.ProjectileType<FMedalPro>();
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						Projectile medalProj = Main.projectile[i];

						if (medalProj.active && medalProj.owner == Player.whoAmI && medalProj.type == fMedalType)
						{
							float len = (medalProj.Size / 2f).LengthSquared() * 0.78f; //Circle inside the projectile hitbox
							if (medalProj.DistanceSQ(Main.MouseWorld) < len)
							{
								accFMedalAmount += 2;
								medalProj.ai[1] = 1f;
								Vector2 offset = new Vector2(Main.rand.Next(-20, 21), Main.rand.Next(-20, 21));
								Dust dust = Dust.NewDustDirect(Main.MouseWorld + offset, 8, 8, 173, Scale: 1.25f);
								dust.noGravity = true;
								dust.velocity = -offset * 0.05f;
							}
						}
					}
				}
			}

			//Medal effect
			accMedalRot += 0.025f;

			if (Main.myPlayer == Player.whoAmI)
			{
				int sMedalType = ModContent.ProjectileType<SMedalPro>();
				if (AccSMedal)
				{
					if (Player.ownedProjectileCounts[sMedalType] == 0)
					{
						Projectile.NewProjectile(new EntitySource_ItemUse(Player, accSMedalItem), Player.Center, Vector2.Zero, sMedalType, 0, 0f, Player.whoAmI, 0, 0.5f);
					}
				}
				else
				{
					accSMedalAmount = 0;
				}

				int fMedalType = ModContent.ProjectileType<FMedalPro>();
				if (AccFMedal)
				{
					if (Player.ownedProjectileCounts[fMedalType] == 0)
					{
						Projectile.NewProjectile(new EntitySource_ItemUse(Player, accFMedalItem), Player.Center, Vector2.Zero, fMedalType, 0, 0f, Player.whoAmI, 1, 0.5f);
					}
				}
				else
				{
					accFMedalAmount = 0;
				}
			}
			
			HandleCPS();

			HandleRadiusAlphas();

			//Milk acc
			if (accGlassOfMilk)
			{
				float bonusDamage = (float)(clickerPerSecond * 0.015f);
				if (bonusDamage >= 0.15f)
				{
					bonusDamage = 0.15f;
				}
				Player.GetDamage<ClickerDamage>() += bonusDamage;
			}
			
			//Hot Keychain
			if (accHotKeychain && !OutOfCombat)
			{
				if (clickerSelected)
				{
					if (accHotKeychainAmount < 0)
					{
						accHotKeychainAmount = 0;
					}
					accHotKeychain2 = true;

					accHotKeychainTimer++;
					if (accHotKeychainTimer > 60)
					{
						int accHotKeychainSpice = (int)(8 - clickerPerSecond);
						Color color = new Color(150, 150, 150);
						if (accHotKeychainSpice > 0)
						{
							color = new Color(255, 150, 75);

							for (int k = 0; k < 2 * accHotKeychainSpice; k++)
							{
								Vector2 offset = new Vector2(Main.rand.Next(-25, 26), Main.rand.Next(-25, 26));
								Dust dust = Dust.NewDustDirect(Player.position + offset, Player.width, Player.height, 174, Scale: 1f);
								dust.noGravity = true;
								dust.velocity = -offset * 0.05f;
							}
						}

						CombatText.NewText(Player.Hitbox, color, accHotKeychainSpice, true, true);

						accHotKeychainAmount += accHotKeychainSpice;
						accHotKeychainTimer = 0;

						if (accHotKeychainAmount > 50)
						{
							Player.AddBuff(BuffID.OnFire3, 300);
							SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 74);
							for (int k = 0; k < 10; k++)
							{
								Vector2 offset = new Vector2(Main.rand.Next(-25, 26), Main.rand.Next(-25, 26));
								Dust dust = Dust.NewDustDirect(Player.position + offset, Player.width, Player.height, 174, Scale: 1.5f);
								dust.noGravity = true;
								dust.velocity = -offset * 0.05f;
							}
							accHotKeychainAmount = 0;
						}
					}
				}
			}
			else
			{
				accHotKeychainTimer = 0;
				if (OutOfCombat)
				{
					accHotKeychainAmount = 0;
				}
			}

			// Get variables from other players
			if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
			{
				for (int num = 0; num < Main.maxPlayers; num++)
				{
					Player other = Main.player[num];
					if (num == Player.whoAmI || !other.active || other.dead || other.team != Player.team || other.team == 0)
					{
						continue;
					}
					// Other player that is on the same team

					int distSQ = 800 * 800;
					if (Player.DistanceSQ(other.Center) < distSQ)
					{
						ClickerPlayer clickerPlayer = other.GetModPlayer<ClickerPlayer>();
						if (clickerPlayer.accButtonMasher)
						{
							accButtonMasher = true;
						}
					}
				}
			}

			// Out of Combat timer
			if (outOfCombatTimer > 0)
			{
				outOfCombatTimer--;
			}
		}

		public override void OnEnterWorld(Player player)
		{
			// Clientside
			if (!enteredWorldOnceThisSession)
			{
				enteredWorldOnceThisSession = true;

				Main.NewText($"[c/{Color.Orange.Hex3()}:Welcome to {Mod.DisplayName}!] If your clickers attack slowly, this may be caused by using mods which enable auto-reuse, such as OmniSwing.");
			}
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			if (ClickerSystem.IsClickerProj(proj))
			{
				if (target.GetGlobalNPC<ClickerGlobalNPC>().embrittle)
				{
					damage += 8;
				}
			}

			if (ClickerSystem.IsClickerWeaponProj(proj))
			{
				if (accSMedalAmount >= 20)
				{
					crit = true;
					accSMedalAmount -= 20;
				}
				if (accFMedalAmount >= 20)
				{
					accFMedalAmount -= 20;
				}
			}
		}

		public override void OnHitNPCWithProj(Projectile projectile, NPC target, int damage, float knockback, bool crit)
		{
			//Proc effects only when an actual "click" happens, and not other clicker projectiles
			if (ClickerSystem.IsClickerWeaponProj(projectile))
			{
				ClickerGlobalNPC clickerNPC = target.GetGlobalNPC<ClickerGlobalNPC>();
				if (target.value > 0f)
				{
					if (accGoldenTicket)
					{
						for (int k = 0; k < 15; k++)
						{
							int dust = Dust.NewDust(target.position, 20, 20, 11, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 75, default(Color), 1.25f);
							Main.dust[dust].noGravity = true;
						}

						var entitySource = projectile.GetSource_OnHit(target, context: "GoldenTicket");
						int amount = 1 + Main.rand.Next(6);
						int coin = Item.NewItem(entitySource, target.Hitbox, ItemID.CopperCoin, amount, false, 0, false, false);

						if (amount > 0)
						{
							clickerMoneyGenerated += amount;
						}
						if (Main.netMode == NetmodeID.MultiplayerClient)
						{
							NetMessage.SendData(MessageID.SyncItem, -1, -1, null, coin, 1f);
						}
					}
				}

				if (AccPaperclips && target.CanBeChasedBy())
				{
					int matterAmount = (int)((target.height * target.width) / 200);
					if (matterAmount > 10)
					{
						matterAmount = 10;
					}
					accPaperclipsAmount += matterAmount;

					if (accPaperclipsAmount >= 100)
					{
						SoundEngine.PlaySound(2, (int)Player.position.X, (int)Player.position.Y, 108);
						for (int k = 0; k < 15; k++)
						{
							int dust = Dust.NewDust(target.position, 20, 20, 1, Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f), 150, default(Color), 1.35f);
							Main.dust[dust].noGravity = true;
						}

						if (Main.myPlayer == Player.whoAmI)
						{
							var entitySource = Player.GetSource_ItemUse(accPaperclipsItem);

							for (int k = 0; k < 4; k++)
							{
								Projectile.NewProjectile(entitySource, Main.MouseWorld.X, Main.MouseWorld.Y, Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-6f, -2f), ModContent.ProjectileType<BottomlessBoxofPaperclipsPro>(), damage, 2f, Player.whoAmI);
							}
						}

						accPaperclipsAmount = 0;
					}
				}

				if (clickerNPC.crystalSlime)
				{
					target.RequestBuffRemoval(ModContent.BuffType<Crystalized>());

					target.AddBuff(ModContent.BuffType<CrystalizedFatigue>(), 60); //Give short cooldown between application in case of proccing on the same enemy due to spread

					var entitySource = projectile.GetSource_OnHit(target, context: "Crystalized");
					int crystal = ModContent.ProjectileType<ClearKeychainPro2>();
					bool spawnEffects = true;

					float total = 10f;
					int i = 0;
					
					while (i < total)
					{
						float hasSpawnEffects = spawnEffects ? 1f : 0f;
						Vector2 toDir = Vector2.UnitX * 0f;
						toDir += -Vector2.UnitY.RotatedBy(i * (MathHelper.TwoPi / total)) * new Vector2(10f, 10f);
						toDir = toDir.RotatedBy(target.velocity.ToRotation());
						int damageAmount = (int)(damage * 0.25f);
						damageAmount = damageAmount < 1 ? 1 : damageAmount;

						Projectile.NewProjectile(entitySource, target.Center + toDir, target.velocity * 0f + toDir.SafeNormalize(Vector2.UnitY) * 10f, crystal, damageAmount, 1f, Main.myPlayer, target.whoAmI, hasSpawnEffects);
						i++;
						spawnEffects = false;
					}
				}

				outOfCombatTimer = OutOfCombatTimeMax;
			}
		}

		public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit)
		{
			outOfCombatTimer = OutOfCombatTimeMax;
		}

		public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
		{
			outOfCombatTimer = OutOfCombatTimeMax;
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			// Don't count as in combat after death, in case respawn timer is less than OutOfCombatTimeMax
			outOfCombatTimer = 0;
		}

		public override void HideDrawLayers(PlayerDrawSet drawInfo)
		{
			if (DrawHotWings)
			{
				//Hide the vanilla wings layer. Important that our own replacement layer is not attached to that (via XParent), then it would get hidden aswell :failure:
				PlayerDrawLayers.Wings.Hide();
			}
		}

		public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
		{
			if (attempt.inLava && attempt.CanFishInLava)
			{
				if (Main.rand.NextBool(50)) //Roughly around [Lava] Crate chance, so around 10%
				{
					itemDrop = ModContent.ItemType<HotKeychain>();
				}
			}
		}
	}
}
