using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;

namespace ScientificQuinn
{
    internal class Program
    {
        public const string ChampName = "Quinn";
        public static HpBarIndicator Hpi = new HpBarIndicator();
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        private static SpellSlot Ignite;
        private static int LastCast;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            //Welcome Message upon loading assembly.
            Game.PrintChat(
                "<font color=\"#00BFFF\">Scientific Quinn -<font color=\"#FFFFFF\"> Recommended Version Successfully Loaded.</font>");
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            if (player.ChampionName != ChampName)
                return;

            Q = new Spell(SpellSlot.Q, 1010);
            E = new Spell(SpellSlot.E, 660);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 80f, 1150, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.25f, 2000f);

            Config = new Menu("Scientific Quinn", "Quinn", true);
            Orbwalker = new Orbwalking.Orbwalker(Config.AddSubMenu(new Menu("[SQ]: Orbwalker", "Orbwalker")));
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("[SQ]: Target Selector", "Target Selector")));

            //COMBOMENU

            var combo = Config.AddSubMenu(new Menu("[SQ]: Combo Settings", "Combo Settings"));
            var harass = Config.AddSubMenu(new Menu("[SQ]: Harass Settings", "Harass Settings"));
            var laneclear = Config.AddSubMenu(new Menu("[SQ]: Laneclear Settings", "Laneclear"));
            var jungleclear = Config.AddSubMenu(new Menu("[SQ]: Jungleclear Settings", "Jungle"));
            var drawing = Config.AddSubMenu(new Menu("[SQ]: Draw Settings", "Draw"));

            combo.AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("UseR", "Use R in Combo [TOGGLE]").SetValue(new KeyBind('K', KeyBindType.Toggle)));

            combo.SubMenu("Item Usage").AddItem(new MenuItem("useGhostBlade", "Use Youmuu's Ghostblade").SetValue(true));
            combo.SubMenu("Item Usage").AddItem(new MenuItem("UseBOTRK", "Use Blade of the Ruined King").SetValue(true));
            combo.SubMenu("Item Usage")
                .AddItem(new MenuItem("eL", "  Enemy HP Percentage").SetValue(new Slider(80, 100, 0)));
            combo.SubMenu("Item Usage")
                .AddItem(new MenuItem("oL", "  Own HP Percentage").SetValue(new Slider(65, 100, 0)));
            combo.SubMenu("Item Usage").AddItem(new MenuItem("UseBilge", "Use Bilgewater Cutlass").SetValue(true));
            combo.SubMenu("Item Usage")
                .AddItem(new MenuItem("HLe", "  Enemy HP Percentage").SetValue(new Slider(80, 100, 0)));

            drawing.AddItem(new MenuItem("Draw_Disabled", "Disable All Spell Drawings").SetValue(false));
            drawing.AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(true));
            drawing.AddItem(new MenuItem("Edraw", "Draw E Range").SetValue(true));
            drawing.AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(true));

            harass.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));
            Config.SubMenu("[SQ]: Misc Settings").AddItem(new MenuItem("DrawD", "Damage Indicator").SetValue(true));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
        }

        private static void OnEndScene(EventArgs args)
        {
            {
                //Damage Indicator
                if (Config.SubMenu("[SQ]: Misc Settings").Item("DrawD").GetValue<bool>())
                {
                    foreach (var enemy in
                        ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                    {
                        Hpi.unit = enemy;
                        Hpi.drawDmg(CalcDamage(enemy), Color.Green);
                    }
                }
            }
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            var pos = Drawing.WorldToScreen(player.Position);
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    combo1();
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    harass();

                if (Config.Item("UseR").GetValue<KeyBind>().Active)
                    Drawing.DrawText(pos.X - 75, pos.Y + 50, Color.Orange, "[R] ComboMode ON!");
            }

            var starget = TargetSelector.GetSelectedTarget();
            var epos = Drawing.WorldToScreen(starget.Position);

            if (starget.IsDead || starget.IsVisible == false)
                return;
            Drawing.DrawText(epos.X - 50, epos.Y + 60, Color.OrangeRed, "Current Target");
            Render.Circle.DrawCircle(starget.Position, 100, Color.Red, 10);
            {
            }

        }

        private static void harass()
        {
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var qpred = Q.GetPrediction(target, true);

            if (Q.IsReady() && qpred.Hitchance >= HitChance.High && Config.Item("harassQ").GetValue<bool>() &&
                target.IsValidTarget(Q.Range) &&
                player.ManaPercentage() >= harassmana)
                Q.Cast(target);

            if (E.IsReady() && target.HasBuff("QuinnW"))
                return;

            if (E.IsReady() && Config.Item("harassE").GetValue<bool>() && target.IsValidTarget(E.Range) &&
                player.ManaPercentage() >= harassmana)

                E.CastOnUnit(target);


        }

        private static void combo1()
        {

            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            var qpred = Q.GetPrediction(target, true);
            var botrk = LeagueSharp.Common.Data.ItemData.Blade_of_the_Ruined_King.GetItem();
            var Ghost = LeagueSharp.Common.Data.ItemData.Youmuus_Ghostblade.GetItem();
            var cutlass = LeagueSharp.Common.Data.ItemData.Bilgewater_Cutlass.GetItem();

            if (target.IsValidTarget(Q.Range + 150))
                Render.Circle.DrawCircle(target.Position, 140, Color.CornflowerBlue);

            if (R.IsReady() && (Config.Item("UseR").GetValue<KeyBind>().Active))
                rlogic();
            if (R.IsReady() && (Config.Item("UseR").GetValue<KeyBind>().Active))
                ASMode();
            if (R.IsReady() && (Config.Item("UseR").GetValue<KeyBind>().Active))
                return;

            if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
                && target.HealthPercentage() <= Config.Item("eL").GetValue<Slider>().Value
                && Config.Item("UseBOTRK").GetValue<bool>())

                botrk.Cast(target);

            if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
                && player.HealthPercentage() <= Config.Item("oL").GetValue<Slider>().Value
                && Config.Item("UseBOTRK").GetValue<bool>())

                botrk.Cast(target);

            if (cutlass.IsReady() && cutlass.IsOwned(player) && cutlass.IsInRange(target) &&
                target.HealthPercentage() <= Config.Item("HLe").GetValue<Slider>().Value
                && Config.Item("UseBilge").GetValue<bool>())

                cutlass.Cast(target);

            if (Ghost.IsReady() && Ghost.IsOwned(player) && target.IsValidTarget(E.Range)
                && target.HealthPercentage() <= Config.Item("Ghostblade").GetValue<Slider>().Value
                && Config.Item("useGhostblade").GetValue<bool>())

                Ghost.Cast();

            if (Q.IsReady() && target.IsValidTarget(Q.Range) && qpred.Hitchance >= HitChance.High)
                Q.Cast(target);

            if (E.IsReady() && target.HasBuff("QuinnW"))
                return;

            if (E.IsReady() && target.IsValidTarget(E.Range))

                E.CastOnUnit(target);

            if (E.IsReady() && target.IsValidTarget(150))
                E.CastOnUnit(target);

        }


        private static bool rmode()
        {
            return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "QuinnRFinale";
        }

        private static void rlogic()
        {
            var target = TargetSelector.GetSelectedTarget();

            if (player.HasBuff("quinnrtimeout") || player.HasBuff("QuinnRForm"))
                return;

            if (E.IsReady() && Q.IsReady() && R.IsReady() && target.IsValidTarget(1800))
                R.Cast();
        }

        private static void ASMode()
        {
            var botrk = LeagueSharp.Common.Data.ItemData.Blade_of_the_Ruined_King.GetItem();
            var Ghost = LeagueSharp.Common.Data.ItemData.Youmuus_Ghostblade.GetItem();
            var cutlass = LeagueSharp.Common.Data.ItemData.Bilgewater_Cutlass.GetItem();

            var target = TargetSelector.GetSelectedTarget();
            var ultfinisher = player.CalcDamage(target, Damage.DamageType.Physical,
                (75 + (R.Level*55) + (player.FlatPhysicalDamageMod*0.5))*(2 - (target.Health/target.MaxHealth)));

            if (botrk.IsReady() && target.IsValidTarget(botrk.Range))
                botrk.Cast(target);
            if (cutlass.IsReady() && target.IsValidTarget(cutlass.Range))
                botrk.Cast(target);
            if (Ghost.IsReady() && target.IsValidTarget(900))
                Ghost.Cast();

            if (E.IsReady())
                E.CastOnUnit(target);

            if (Q.IsReady() && target.IsValidTarget(200) && player.Position.CountEnemiesInRange(200) > 0)
                Q.Cast(target);

            if (Q.IsReady())
                return;

            if (R.IsReady() && ultfinisher > target.Health - 200 && player.Position.CountEnemiesInRange(500) > 0)
                R.Cast(player);
        }


        private static int CalcDamage(Obj_AI_Base target)
        {
            var AA = player.CalcDamage(target, Damage.DamageType.Physical,
                player.FlatPhysicalDamageMod + player.BaseAttackDamage);
            var damage = AA;

            if (Ignite != SpellSlot.Unknown &&
                player.Spellbook.CanUseSpell(Ignite) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += player.GetItemDamage(target, Damage.DamageItems.Botrk); //ITEM BOTRK

            if (E.IsReady() && Config.Item("UseE").GetValue<bool>()) // edamage
            {
                damage += player.CalcDamage(target, Damage.DamageType.Physical,
                    10 + (E.Level*30) + (player.FlatPhysicalDamageMod*0.2));
            }

            if (Q.IsReady() && Config.Item("UseQ").GetValue<bool>()) // qdamage
            {
                damage += Q.GetDamage(target);
            }

            if (rmode() && target.HasBuff("QuinnW") && !E.IsReady())
            {
                damage += player.CalcDamage(target, Damage.DamageType.Physical,
                    15 + (player.Level*10) + (player.FlatPhysicalDamageMod*0.5)); // passive


                if (R.IsReady() && Config.Item("UseR").GetValue<bool>()) // rdamage
                
                        damage += player.CalcDamage(target, Damage.DamageType.Physical,
                            (75 + (R.Level*55) + (player.FlatPhysicalDamageMod*0.5))*
                            (2 - ((target.Health - damage)/target.MaxHealth)));
                    }
                    return (int) damage;
                }
            
        private static
                void OnDraw(EventArgs args)
        {
            //Draw Skill Cooldown on Champ
            var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            {

                if (Config.Item("Draw_Disabled").GetValue<bool>())
                    return;

                if (Config.Item("Qdraw").GetValue<bool>())
                    if (Q.Level > 0)
                        Utility.DrawCircle(ObjectManager.Player.Position, Q.Range,
                            Q.IsReady() ? Color.DeepSkyBlue : Color.Red);

                if (Config.Item("Edraw").GetValue<bool>())
                    if (E.Level > 0)
                        Utility.DrawCircle(ObjectManager.Player.Position, E.Range - 1,
                            E.IsReady() ? Color.Blue : Color.Red);

                if (Config.Item("Rdraw").GetValue<bool>())
                    if (R.Level > 0)
                        Utility.DrawCircle(ObjectManager.Player.Position, 1200 - 2,
                            R.IsReady() ? Color.CornflowerBlue : Color.Red);
            }
        }
    }
}

                
            
            
        


            
        
    
        
    


            
        
    


  


