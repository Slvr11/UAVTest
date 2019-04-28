using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfinityScript;

namespace UAVTest
{
    public class UAVStrike : BaseScript
    {
        private int Laser_FX;

        public UAVStrike()
            : base()
        {
            Laser_FX = Call<int>("loadfx", "misc/laser_glow");

            PlayerConnected += new Action<Entity>(entity =>
            {
                entity.SetField("killstreak", 0);
                entity.Call("notifyonplayercommand", "useks", "+actionslot 3");
                entity.OnNotify("useks", e1 =>
                    {
                        CheckStreak(entity, "uav");
                    });
                entity.OnNotify("weapon_change", (player, newWeap) =>
                        {
                            if (mayDropWeapon((string)newWeap))
                                entity.SetField("lastDroppableWeapon", (string)newWeap);
                            KillstreakUseWaiter(entity, (string)newWeap);
                        });

                entity.OnNotify("weapon_fired", (ent, weaponName) =>
                {
                    if ((string)weaponName != "uav_strike_marker_mp")
                        return;

                    entity.AfterDelay(900, player => TakeUAVWeapon(entity));

                    PrintNameInFeed(entity);

                    if (entity.GetField<string>("customStreak") == "uav")
                    {
                        entity.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, false);
                        entity.Call("SetPlayerData", "killstreaksState", "icons", 0, 0);
                        entity.SetField("customStreak", string.Empty);
                    }

                    Vector3 playerForward = ent.Call<Vector3>("gettagorigin", "tag_weapon") + Call<Vector3>("AnglesToForward", ent.Call<Vector3>("getplayerangles")) * 100000;

                    Entity refobject = Call<Entity>("spawn", "script_model", ent.Call<Vector3>("gettagorigin", "tag_weapon_left"));
                    refobject.Call("setmodel", "com_plasticcase_beige_big");
                    refobject.SetField("angles", ent.Call<Vector3>("getplayerangles"));
                    refobject.Call("moveto", playerForward, 100);
                    refobject.Call("hide"); //for some reason we have to keep the model, oh well, we'll just hide it.

                    /*
                   Entity effectobject = Call<Entity>("spawn", "script_model", refobject.Origin);
                   effectobject.Call("setmodel", "mil_emergency_flare_mp");
                   Entity fxEnt = Call<Entity>("SpawnFx", TI_FX, effectobject);
                   fxEnt.Call("linkto", effectobject);
                   effectobject.Call("linkto", refobject);
                   Call("TriggerFx", fxEnt);
                    */

                    refobject.OnInterval(10, (refent) =>
                    {
                        if (CollidingSoon(refent, ent))
                        {
                            Call("magicbullet", "uav_strike_projectile_mp", new Vector3(refent.Origin.X, refent.Origin.Y, refent.Origin.Z + 6000), refent.Origin, ent);

                            Entity redfx = Call<Entity>("spawnfx", Laser_FX, refent.Origin);
                            Call("triggerfx", redfx);
                            AfterDelay(4500, () => { redfx.Call("delete"); });
                            return false;
                        }

                        return true;
                    });
                });
            });
        }

        private bool CollidingSoon(Entity refobject, Entity player)
        {
            Vector3 endorigin = refobject.Origin + Call<Vector3>("anglestoforward", refobject.GetField<Vector3>("angles")) * 100;

            if (SightTracePassed(refobject.Origin, endorigin, false, player))
                return false;
            else
                return true;
        }
        private bool SightTracePassed(Vector3 StartOrigin, Vector3 EndOrigin, bool tracecharacters, Entity ignoringent)
        {
            int trace = Call<int>("SightTracePassed", new Parameter(StartOrigin), new Parameter(EndOrigin), tracecharacters, new Parameter(ignoringent));
            if (trace > 0)
                return true;
            else
                return false;
        }
        public void PrintNameInFeed(Entity player)
        {
            Call(334, string.Format("UAV Strike called in by {0}", player.GetField<string>("name")));
        }
        public void TakeUAVWeapon(Entity player)
        {
            player.TakeWeapon("uav_strike_marker_mp");
        }
        private void CheckStreak(Entity player, string streakName)
        {
            string wep = getKillstreakWeapon(streakName);
            if (string.IsNullOrEmpty(wep))
                return;

            if (player.GetField<int>("killstreak") == 0)
            {
                player.SetField("customStreak", streakName);

                player.Call(33392, "uav_strike", 0);
                player.Call("giveWeapon", wep, 0, false);
                player.Call("setActionSlot", 4, "weapon", wep);
                player.Call("SetPlayerData", "killstreaksState", "hasStreak", 0, true);
                player.Call("SetPlayerData", "killstreaksState", "icons", 0, getKillstreakIndex("predator_missile"));
            }
            else
                return;
        }
        private string getKillstreakWeapon(string streakName)
        {
            string ret = string.Empty;
            ret = Call<string>("tableLookup", "mp/killstreakTable.csv", 1, streakName, 12);
            return ret;
        }
        private int getKillstreakIndex(string streakName)
        {
            int ret = 0;
            ret = Call<int>("tableLookupRowNum", "mp/killstreakTable.csv", 1, streakName) - 1;

            return ret;
        }
  private void KillstreakUseWaiter(Entity ent, string weapon)
        {
            if (weapon == "killstreak_uav_mp")
            {
                    var elem = HudElem.CreateFontString(ent, "hudlarge", 2.5f);
                    elem.SetPoint("BOTTOMCENTER", "BOTTOMCENTER", 0, -60);
                    elem.SetText("Lase target for Predator Strike.");
                    ent.TakeWeapon("killstreak_uav_mp");
                    ent.AfterDelay(3500, player => elem.SetText(""));
                    ent.GiveWeapon("uav_strike_marker_mp");
                    ent.SwitchToWeapon("uav_strike_marker_mp");
                }
        }
  private bool mayDropWeapon(string weapon)
  {
      if (weapon == "none")
          return false;

      if (weapon.Contains("ac130"))
          return false;

      string invType = Call<string>("WeaponInventoryType", weapon);
      if (invType != "primary")
          return false;

      return true;
  }
    }
}