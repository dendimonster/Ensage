using System;
using System.Linq;
using Ensage;
using Ensage.Common.Extensions;
using Ensage.Common;
using Ensage.Common.Menu;
using SharpDX;
using SharpDX.Direct3D9;

namespace TinkerMadness
{
	internal class Program
	{
		private static readonly Menu Menu = new Menu("Tinker Madness", "tinkermadness", true, "npc_dota_hero_tinker", true);
		private static readonly Menu SubMenu = new Menu("More", "more");
		private static Ability Laser, Rocket, ReArm;
		private static Item Blink, Dagon, Hex, Soulring, Ethereal, Veil, Orchid, Shiva, Glimmer;
		private static Hero me;
		private static Hero target;
		private static bool active;
		private static readonly int[] DagonDamage = new int[5] { 400, 500, 600, 700, 800 };
		private static readonly int[] DagonRange = new int[5] { 600, 650, 700, 750, 800 };
		private static readonly int[] LaserDamage = new int[4] { 80, 160, 240, 320 };
		private static readonly int[] RocketDamage = new int[4] { 125, 200, 275, 350 };
		static void Main(string[] args)
		{
			Menu.AddItem(new MenuItem("go", "Combo Key").SetValue(new KeyBind('G', KeyBindType.Press)).SetTooltip("Hoding Key will keep Tinker Madness On"));
			Menu.AddItem(new MenuItem("smartblink", "Smart Blink Key").SetValue(new KeyBind('F', KeyBindType.Press)).SetTooltip("A Smart Blink QuickCast. What is your blink key in dota2 settings? set it here"));
			Menu.AddItem(new MenuItem("gorearm", "Smart Ulti Key").SetValue(new KeyBind('R', KeyBindType.Press)).SetTooltip("What is your rearm key in dota 2 settings? use it here"));
			Menu.AddItem(new MenuItem("note", "Notice: Hotkeys are PressType Keys dont click on ON/OFF"));
			Menu.AddSubMenu(SubMenu);
			SubMenu.AddItem(new MenuItem("ignorelaser", "Ignore Laser in Combo").SetValue(false).SetTooltip("Enabling this will ignore Laser use in Combo"));
			SubMenu.AddItem(new MenuItem("useblink", "Use Blink in Combo").SetValue(true).SetTooltip("Auto use Blink During Madness"));
			SubMenu.AddItem(new MenuItem("safeglimmer", "Glimmer Travel").SetValue(true).SetTooltip("Auto use Glimmer Cape if Tinker uses boots of Travel"));
			SubMenu.AddItem(new MenuItem("autoks", "Auto KS").SetValue(true).SetTooltip("Auto use Dagon, Laser or rocket for Kill Steal"));
			
			Menu.AddToMainMenu();
			Game.OnUpdate += Game_OnUpdate;
			Game.OnWndProc += Game_OnWndProc;
			Game.PrintMessage("<font color='#aaaaaa'>Tinker Madness </font><font color='#33ff66'>Loaded</font>", MessageType.LogMessage);
		}
		public static void Game_OnUpdate(EventArgs args)
		{
			me = ObjectMgr.LocalHero;
			if (me == null || !Game.IsInGame)
			{
				return;
			}
			// Ability init
			Laser = me.Spellbook.Spell1;
			Rocket = me.Spellbook.Spell2;
			ReArm = me.Spellbook.Spell4;
			// Item init
			Blink = me.FindItem("item_blink");
			Dagon = me.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_dagon"));
			Hex = me.FindItem("item_sheepstick");
			Soulring = me.FindItem("item_soul_ring");
			Ethereal = me.FindItem("item_ethereal_blade");
			Veil = me.FindItem("item_veil_of_discord");
			Orchid = me.FindItem("item_orchid");
			Shiva = me.FindItem("item_shivas_guard");
			Glimmer = me.FindItem("item_glimmer_cape");
			// Glimmer Use on Boots of Travel
			if (Glimmer !=null && me.IsChanneling() && Glimmer.CanBeCasted() && Utils.SleepCheck("Glimmer") && !ReArm.IsChanneling && (SubMenu.Item("safeglimmer").GetValue<bool>()))
				{
					Glimmer.UseAbility(me);
					Utils.Sleep(100 + Game.Ping, "Glimmer");
				}
			// Smart Blink
			if (Blink !=null && !me.IsChanneling() && Blink.CanBeCasted() && Utils.SleepCheck("Blink") && (Menu.Item("smartblink").GetValue<KeyBind>().Active))
			{
				Blink.UseAbility(Game.MousePosition);
				Utils.Sleep(750 + Game.Ping, "Blink");
			}
			// KS Section
			if (SubMenu.Item("autoks").GetValue<bool>() && !active)
			{
				var enemy = ObjectMgr.GetEntities<Hero>()
					.Where(x => x.Team != me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion && !x.UnitState.HasFlag(UnitState.MagicImmune))
					.ToList();
				foreach (var i in enemy)
				{
					var linken = i.Inventory.Items.FirstOrDefault(x => x.Name == "item_sphere");
					var sphere = i.Modifiers.Any(x => x.Name == "modifier_item_sphere_target");
					var ta = i.Modifiers.Any(x => x.Name == "modifier_templar_assassin_refraction_damage");
					var dazzle = i.Modifiers.Any(x => x.Name == "modifier_dazzle_shallow_grave");
					var abaddon = i.Modifiers.Any(x => x.Name == "modifier_abaddon_borrowed_time");
					var pipe = i.Modifiers.Any(x => x.Name == "modifier_item_pipe_barrier");
					
					if (Dagon.CanBeCasted() && Utils.SleepCheck("dagon") && Utils.SleepCheck("rearm") && !ReArm.IsChanneling)
						{
							if ((linken != null && linken.Cooldown == 0) || (sphere || ta || dazzle || abaddon || pipe))
								return;
							var range = DagonRange[Dagon.Level - 1];
							var damage = Math.Floor(DagonDamage[Dagon.Level - 1] * (1 - i.MagicDamageResist));
							if (me.Distance2D(i) < range && i.Health < damage)
								Dagon.UseAbility(i);
								Utils.Sleep(500 + Game.Ping, "dagon");
						}
						if (Laser.CanBeCasted() && Utils.SleepCheck("laser") && Utils.SleepCheck("rearm") && !ReArm.IsChanneling)
						{
							if ((linken != null && linken.Cooldown == 0) || (sphere || ta || dazzle || abaddon))
								return;
							var damage = Math.Floor(LaserDamage[Laser.Level - 1] - (1 - i.MagicDamageResist));
							if (me.Distance2D(i) < 650 && i.Health < damage)
								Laser.UseAbility(i);
								Utils.Sleep(500 + Game.Ping, "laser");
						}
						if (Rocket.CanBeCasted() && Utils.SleepCheck("rocket") && Utils.SleepCheck("rearm") && !ReArm.IsChanneling)
						{
							if (ta || dazzle || abaddon)
								return;
							var damage = Math.Floor(RocketDamage[Rocket.Level - 1] * (1 - i.MagicDamageResist));
							if (me.Distance2D(i) < 2500 && i.Health < damage)
								Rocket.UseAbility();
								Utils.Sleep(500 + Game.Ping, "rocket");
						}
				}
			}
			// Conrol Rearm
			if (ReArm !=null && !ReArm.IsChanneling && ReArm.CanBeCasted() && Utils.SleepCheck("rearm") && (Menu.Item("gorearm").GetValue<KeyBind>().Active))
			{
				ReArm.UseAbility();
				Utils.ChainStun(me, (ReArm.ChannelTime * 1000) + Game.Ping + 400, null, false);
				Utils.Sleep(700 + Game.Ping, "rearm");
			}
			// Main combo
			if (active)
			{
				target = me.ClosestToMouseTarget(1000);
				if ((target == null || !target.IsVisible) && !me.IsChanneling() && (me.Distance2D(Game.MousePosition) < 3000) && Utils.SleepCheck("rearm"))
				{
					me.Move(Game.MousePosition);
				}
				if (target != null && target.IsAlive && !target.IsIllusion && !target.IsMagicImmune() && Utils.SleepCheck("rearm") && !me.IsChanneling() && (me.Distance2D(target) < 3000))
				{
					if (Soulring != null && Soulring.CanBeCasted() && me.Health > 300 && Utils.SleepCheck("soulring") && Utils.SleepCheck("rearm"))
					{
						Soulring.UseAbility();
						Utils.Sleep(150 + Game.Ping, "soulring");
					}
					// Blink
					if (Blink != null && Blink.CanBeCasted() && (me.Distance2D(target) > 500) && Utils.SleepCheck("Blink") && SubMenu.Item("useblink").GetValue<bool>() && !nothingCanCast() && !me.IsChanneling() && !ReArm.IsChanneling && (me.Mana > 200) && Utils.SleepCheck("rearm"))
					{
						Blink.UseAbility(target.Position);
						Utils.Sleep(750 + Game.Ping, "Blink");
					}
					// Items
					else if (Shiva != null && Shiva.CanBeCasted() && Utils.SleepCheck("shiva") && Utils.SleepCheck("rearm"))
					{
						Shiva.UseAbility();
						Utils.Sleep(100 + Game.Ping, "shiva");
						Utils.ChainStun(me, 200 + Game.Ping, null, false);
					}
					else if (Veil != null && Veil.CanBeCasted() && Utils.SleepCheck("veil") && Utils.SleepCheck("rearm"))
					{
						Veil.UseAbility(target.Position);
						Utils.Sleep(150 + Game.Ping, "veil");
						Utils.Sleep(300 + Game.Ping, "ve");
						Utils.ChainStun(me, 170 + Game.Ping, null, false);
					}
					else if (Hex != null && Hex.CanBeCasted() && Utils.SleepCheck("hex") && Utils.SleepCheck("rearm"))
					{
						Hex.UseAbility(target);
						Utils.Sleep(150 + Game.Ping, "hex");
						Utils.Sleep(300 + Game.Ping, "h");
						Utils.ChainStun(me, 170 + Game.Ping, null, false);
					}
					else if (Ethereal != null && Ethereal.CanBeCasted() && Utils.SleepCheck("ethereal") && Utils.SleepCheck("rearm"))
					{
						Ethereal.UseAbility(target);
						Utils.Sleep(270 + Game.Ping, "ethereal");
						Utils.ChainStun(me, 200 + Game.Ping, null, false);
					}
					else if (Dagon != null && Dagon.CanBeCasted() && Utils.SleepCheck("ethereal") && Utils.SleepCheck("h") && Utils.SleepCheck("dagon") && Utils.SleepCheck("veil") && Utils.SleepCheck("rearm"))
					{
						Dagon.UseAbility(target);
						Utils.Sleep(270 + Game.Ping, "dagon");
						Utils.ChainStun(me, 200 + Game.Ping, null, false);
					}
					// Skills
					else if (Rocket != null && Rocket.CanBeCasted() && Utils.SleepCheck("rocket") && Utils.SleepCheck("ethereal") && Utils.SleepCheck("veil") && Utils.SleepCheck("rearm"))
					{
						Rocket.UseAbility();
						Utils.Sleep(150 + Game.Ping, "rocket");
						Utils.ChainStun(me, 150 + Game.Ping, null, false);
					}
					else if (Laser != null && Laser.CanBeCasted() && Utils.SleepCheck("laser") && Utils.SleepCheck("ethereal") && Utils.SleepCheck("rocket") && Utils.SleepCheck("rearm") && !(SubMenu.Item("ignorelaser").GetValue<bool>()))
					{
						Laser.UseAbility(target);
						Utils.Sleep(150 + Game.Ping, "laser");
						Utils.ChainStun(me, 150 + Game.Ping, null, false);
					}
					else if (ReArm != null && ReArm.CanBeCasted() && me.Mana > 200 && Utils.SleepCheck("rearm") && !ReArm.IsChanneling && nothingCanCast())
					{
						ReArm.UseAbility();
						Utils.ChainStun(me, (ReArm.ChannelTime * 1000) + Game.Ping + 400, null, false);
						Utils.Sleep(700 + Game.Ping, "rearm");
					}
					else if (!me.IsChanneling() && !ReArm.IsChanneling && nothingCanCast() && Utils.SleepCheck("rearm"))
					{
						me.Attack(target);
					}
				}
			}
		}
		private static bool nothingCanCast()
		{
			if ((!Laser.CanBeCasted() && !Rocket.CanBeCasted() && !Ethereal.CanBeCasted() && !Dagon.CanBeCasted() && !Hex.CanBeCasted() && !Shiva.CanBeCasted() && !Veil.CanBeCasted() && !(SubMenu.Item("ignorelaser").GetValue<bool>())) || (Laser.CanBeCasted() && !Rocket.CanBeCasted() && !Ethereal.CanBeCasted() && !Dagon.CanBeCasted() && !Hex.CanBeCasted() && !Shiva.CanBeCasted() && !Veil.CanBeCasted() && (SubMenu.Item("ignorelaser").GetValue<bool>())))
				return true;
			
				return false;	
		}
		private static void Game_OnWndProc(WndEventArgs args)
		{
			if (!Game.IsChatOpen)
			{
				if (Menu.Item("go").GetValue<KeyBind>().Active)
				{
					active = true;
				}
				else
				{
					active = false;
				}						
			}
		}
	}
}
