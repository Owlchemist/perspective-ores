using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;
using RimWorld;
using HarmonyLib;
using static PerspectiveOres.ModSettings_PerspectiveOres;
 
namespace PerspectiveOres
{
	[StaticConstructorOnStartup]
    public static class DrawUtility
	{
		static DrawUtility()
        {
            var list = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = list.Count; i-- > 0;)
            {
                var def = list[i];
                if (def.thingClass == mineable && def.building != null && def.building.isResourceRock) mineableDefs.Add(def);
            }
            new Harmony("Owlchemist.PerspectiveOres").PatchAll();

			//Setup new user data
			if (skippedMineableDefs == null) skippedMineableDefs = new HashSet<string>() { DefDatabase<ThingDef>.GetNamed("MineableComponentsIndustrial").defName };
        }

		static System.Type mineable = typeof(Mineable);
		static List<ThingDef> mineableDefs = new List<ThingDef>();
		public static int lineNumber, cellPosition;
		public const int lineHeight = 22; //Text.LineHeight + options.verticalSpacing;
		public static void DrawList(Rect container, Listing_Standard options)
		{
			lineNumber = cellPosition = 0; //Reset
			//List out all the unremoved defs from the compiled database
			for (int i = mineableDefs.Count; i-- > 0;)
			{
				ThingDef def = mineableDefs[i];
				if (def != null)
				{
					DrawListItem(options, def);
					cellPosition += lineHeight;
					++lineNumber;
				}
			}
		}

		public static void DrawListItem(Listing_Standard options, ThingDef def)
		{
			//Determine checkbox status...
			bool checkOn = skippedMineableDefs.Contains(def.defName);
			
			//Fetch bounding rect
			Rect rect = options.GetRect(lineHeight);
			rect.y = cellPosition;

			//Label
			string dataString = def.label + " :: " + def.modContentPack?.Name + " :: " + def.defName;

			//Actually draw the line item
			if (options.BoundingRectCached == null || rect.Overlaps(options.BoundingRectCached.Value))
			{
				CheckboxLabeled(rect, dataString, def.label, ref checkOn, def);
			}

			//Handle row coloring and spacing
			options.Gap(options.verticalSpacing);
			if (lineNumber % 2 != 0) Widgets.DrawLightHighlight(rect);
			Widgets.DrawHighlightIfMouseover(rect);
			
			//Add to working list if missing
			if (checkOn && !skippedMineableDefs.Contains(def.defName)) skippedMineableDefs.Add(def.defName);
			//Remove from working list
			else if (!checkOn && skippedMineableDefs.Contains(def.defName)) skippedMineableDefs.Remove(def.defName);
		}

		static void CheckboxLabeled(Rect rect, string data, string label, ref bool checkOn, ThingDef def)
		{
			Rect leftHalf = rect.LeftHalf();
			
			//Is there an icon?
			Rect iconRect = new Rect(leftHalf.x, leftHalf.y, 32f, leftHalf.height);
			Texture2D icon = null;
			if (def is BuildableDef) icon = ((BuildableDef)def).uiIcon;
			if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 1f, Color.white, 0f, 0f);

			//If there is a label, split the cell in half, otherwise use the full cell for data
			if (!label.NullOrEmpty())
			{
				Rect dataRect = new Rect(iconRect.xMax, iconRect.y, leftHalf.width - 32f, leftHalf.height);

				Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
				Rect rightHalf = rect.RightHalf();
				Widgets.Label(rightHalf, label.Truncate(rightHalf.width - 12f, InspectPaneUtility.truncatedLabelsCached));
			}
			else
			{
				Rect dataRect = new Rect(iconRect.xMax, iconRect.y, rect.width - 32f, leftHalf.height);
				Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
			}

			//Checkbox
			Widgets.Checkbox(new Vector2(rect.xMax - 24f, rect.y), ref checkOn, paintable: true);
		}
	}
}
