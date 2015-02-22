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
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
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
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 550);

            Q.SetSkillshot(0.25f, 80f, 1150, true, SkillshotType.SkillshotLine);
            E.SetTargetted(0.25f, 2000f);

            Config = new Menu("Scientific Quinn", "Quinn", true);
            Orbwalker = new Orbwalking.Orbwalker(Config.AddSubMenu(new Menu("[SQ]: Orbwalker", "Orbwalker")));
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("[SQ]: Target Selector", "Target Selector")));

            //COMBOMENU

            var combo = Config.AddSubMenu(new Menu("[SQ]: Combo Settings", "Combo Settings"));
            var harass = Config.AddSubMenu(new Menu("[SQ]: Harass Settings", "Harass Settings"));

            combo.AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("UseE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("UseRburst", "Use R|Target is Killable|").SetValue(false));
            combo.AddItem(new MenuItem("ASMode", "Assassin Mode [TOGGLE]").SetValue(new KeyBind('K', KeyBindType.Toggle)));

            combo.SubMenu("Item Usage").AddItem(new MenuItem("UseBOTRK", "Use Blade of the Ruined King").SetValue(true));
            combo.SubMenu("Item Usage")
                .AddItem(new MenuItem("eL", "  Enemy HP Percentage").SetValue(new Slider(80, 100, 0)));
            combo.SubMenu("Item Usage")
                .AddItem(new MenuItem("oL", "  Own HP Percentage").SetValue(new Slider(65, 100, 0)));
            combo.SubMenu("Item Usage").AddItem(new MenuItem("UseBilge", "Use Bilgewater Cutlass").SetValue(true));
            combo.SubMenu("Item Usage")
                .AddItem(new MenuItem("HLe", "  Enemy HP Percentage").SetValue(new Slider(80, 100, 0)));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += ASMode;
        }

        private static void ASMode(EventArgs args)
        {
            var starget = TargetSelector.GetSelectedTarget();
            var epos = Drawing.WorldToScreen(starget.Position);

            Drawing.DrawText(epos.X - 100, epos.Y + 50, Color.IndianRed, "Assassination Target");
            Render.Circle.DrawCircle(starget.Position, 140, Color.LightSeaGreen);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            var pos = Drawing.WorldToScreen(player.Position);

            if (Config.Item("ASMode").GetValue<KeyBind>().Active)
                Drawing.DrawText(pos.X - 100, pos.Y + 50, Color.DarkOrange, "Assassin Mode Active!");

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                combo1();

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

            if (R.IsReady() && (Config.Item("ASMode").GetValue<KeyBind>().Active))
                rlogic();
            if (R.IsReady() && (Config.Item("ASMode").GetValue<KeyBind>().Active))
                ASMode();
            if (R.IsReady() && (Config.Item("ASMode").GetValue<KeyBind>().Active))
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

            if(R.IsReady() && player.Position.CountEnemiesInRange(500) > 0)
                R.Cast(player);
        }


            }
        }
    


  


