using System;
using UnityEngine;

namespace MyPlugin
{
   // KSPAddon.Startup.MainMenu makes sure we already have loaded all ingame textures
   // make sure once is set to true
   [KSPAddon(KSPAddon.Startup.MainMenu, true)]
   class Example : MonoBehaviour
   {
      // place your ribbon png files (size 120x32 pixel) here...
      private const String RIBBON_BASE = "MyPluginInGameData/Ribbons/";

      // UNIQUE (!) ribbon code
      private const String RIBBON_CODE = "KSP";
      // UNIQUE (!) custom ribbon id (used for custom ribbons only)
      private const int RIBBON_ID = 1001;

      private FinalFrontierAdapter adapter;

      // optional (if you want to award ribbons by their corresponding objects instead of ribbon code)
      private object KspRibbon;

      public void Start()
      {
         // just a log that this plugin has started
         Debug.Log("starting example");

         // create the adapter
         this.adapter = new FinalFrontierAdapter();
         // plugin to Final Frontier
         this.adapter.Plugin();

         // optional: log the version of Final Frontier
         Debug.Log("Final Frontier version: "+adapter.GetVersion());

         if (this.adapter.IsInstalled()) // optional test
         {
            // register the ribbon with a unique code, a path to the png file and a name for a ribbon and an optional description
            // prestige and boolean attribute for a ribbon that has to be a first awarded ribbon are optional
            // IMPORTANT: do not register ribbons twice and not before all textures are loaded (not until GameScene LOADING is done)!
            KspRibbon = this.adapter.RegisterRibbon(
               RIBBON_CODE,                                                        // unique ribbon code
               RIBBON_BASE + "SpaceProgram",                                       // path to ribbon png file
               "Space Program Ribbon",                                             // name of ribbon
               "Awarded to every applicant that joines the kerbal space program"   // description (optional)
              );
            //
            // this is an example for a custom ribbon (i.e. the player can award manually)
            this.adapter.RegisterCustomRibbon(
                           RIBBON_ID,                                              // unique ribbon id
                           RIBBON_BASE + "Custom1001",                             // path to ribbon png file
                           "Custom Ribbon 1001",                                   // name of ribbon
                           "Awarded manually by the player"                        // description (optional)
                          );
         }

         // example usage:
         // ok, we want to award this ribbon to every applicant that enters the space program for real duties;
         // see callback below
         GameEvents.OnCrewmemberHired.Add(this.OnCrewmemberHired);
         
         // example usage:
         // everytime a vessel is recovered, we want to log some statistical data of the crew;
         // see callback below
         GameEvents.onVesselRecovered.Add(this.OnVesselRecovered);
      }

      // callback for new crew member hired
      private void OnCrewmemberHired(ProtoCrewMember kerbal, int value)
      {
         // its better to be safe than sorry
         if (kerbal == null) return;
         //
         Debug.Log("applicant " + kerbal.name+ " will receive the KSP ribbon");
         //
         // now we want to award the KSP ribbon
         this.adapter.AwardRibbonToKerbal(RIBBON_CODE, kerbal);
         // an alternative to award the ribbon:
         //this.adapter.AwardRibbonToKerbal(KspRibbon, kerbal);
      }

      // callback for vessel recovered
      private void OnVesselRecovered(ProtoVessel vessel, bool flag)
      {
         // its better to be safe than sorry
         if (vessel == null) return;
         //
         // log crew statistics (not really useful)
         foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
         {
            Debug.Log("crew member " + kerbal.name + " total missions: " + adapter.GetMissionsFlownForKerbal(kerbal)); // without the current one
            Debug.Log("crew member " + kerbal.name + " research points: " + adapter.GetResearchForKerbal(kerbal));
            Debug.Log("crew member " + kerbal.name + " dockings: " + adapter.GetDockingsForKerbal(kerbal));
            Debug.Log("crew member " + kerbal.name + " contracts complteted: " + adapter.GetContractsCompletedForKerbal(kerbal));
         }
      }
   }
}
