using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using static PerspectiveOres.ModSettings_PerspectiveOres;

namespace PerspectiveOres
{   
    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
	public class PerspectiveOresSetup
    {
        static void Postfix(Map __instance)
        {
            ProcessMap(__instance);
        }

        public static void ProcessMap(Map map, bool reset = false)
        {
            Dictionary<(GraphicData, Color), Graphic> graphicCache = new Dictionary<(GraphicData, Color), Graphic>();
            Dictionary<Building, int> lumps = new Dictionary<Building, int>(); //thingID, lumpID
            Dictionary<int, Color> lumpColors = new Dictionary<int, Color>(); //lumpID, assosicated stone
            if (skippedMineableDefs == null) skippedMineableDefs = new HashSet<string>();

            int nextLumpID = 0;
            
            var list = map.listerThings.listsByGroup[2];
            var length = list.Count;
            for (int i = 0; i < length; i++)
            {
                var thing = list[i];
                if (thing is not Mineable mineable || ModSettings_PerspectiveOres.skippedMineableDefs.Contains(thing.def.defName)) 
                {
                    if (reset) thing.graphicInt = null; //Reset
                    continue;
                }
                if (!mineable.def.building.isResourceRock || !mineable.def.graphicData.Linked) continue;

                if (!lumps.ContainsKey(mineable))
                {
                    lumps.Add(mineable, nextLumpID);
                    DetermineLump(thing.def, nextLumpID++, thing.Position);
                }
            }
            if (lumps.Count == 0)
            {
                if (reset) map.mapDrawer.RegenerateEverythingNow();
                return; //Found nothing
            }
            Log.Message("[Perspective: Ores] identified " + lumps.Count.ToString() + " resource lumps.");

            AssociateLumps();
            LongEventHandler.QueueLongEvent(() => RecolorMineables(), null, false, null);
            LongEventHandler.QueueLongEvent(() => map.mapDrawer.RegenerateEverythingNow(), null, false, null);

            void DetermineLump(ThingDef def, int lumpID, IntVec3 pos)
            {
                foreach (var item in GenAdjFast.AdjacentCells8Way(pos).ToList())
                {
                    var edifice = item.GetEdifice(map);
                    if (edifice == null || edifice.def != def || lumps.ContainsKey(edifice) || edifice is not Mineable mineableEdifice) continue;
                    if (!mineableEdifice.def.building.isResourceRock) continue;

                    lumps.Add(edifice, lumpID);
                    DetermineLump(def, lumpID, item);
                }
            }
            void AssociateLumps()
            {
                foreach (var lumpCell in lumps) //Key = the mineable thing, Value = its lumpID
                {
                    if (lumpColors.ContainsKey(lumpCell.Value)) continue; //This lump has already found its match, skip.

                    //Look around this mineral and try to find a stone
                    foreach (var pos in GenAdjFast.AdjacentCells8Way(lumpCell.Key.Position))
                    {
                        var edifice = pos.GetEdifice(map);
                        if (edifice == null || edifice.def == lumpCell.Key.def || edifice is not Mineable mineableEdifice) continue;
                        if (mineableEdifice.def.building.isResourceRock || !mineableEdifice.def.building.isNaturalRock) continue;

                        lumpColors.Add(lumpCell.Value, edifice.DrawColor);
                        break;
                    }
                }
            }
            void RecolorMineables()
            {
                foreach (var item in lumps)
                {
                    var color = lumpColors.TryGetValue(item.Value);
                    if (color == null) continue;
                    var graphic = item.Key.def.graphicData;
                    //Check if the graphic already exists
                    var cachedGraphic = graphicCache.TryGetValue((graphic, color));
                    if (cachedGraphic == null)
                    {
                        cachedGraphic = GraphicDatabase.Get(graphic.graphicClass, graphic.texPath, 
                            (graphic.shaderType ?? ShaderTypeDefOf.Cutout).Shader, graphic.drawSize, color, graphic.colorTwo, graphic, graphic.shaderParameters, graphic.maskPath);
                        
                        cachedGraphic = GraphicUtility.WrapLinked(cachedGraphic, graphic.linkType);
                        graphicCache.Add((graphic, color), cachedGraphic);
                    }
                    item.Key.graphicInt = cachedGraphic;
                }
            }
        }
    }
}