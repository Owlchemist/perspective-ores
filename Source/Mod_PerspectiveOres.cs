using Verse;
using UnityEngine;
using System.Collections.Generic;
using static PerspectiveOres.ModSettings_PerspectiveOres;
using static PerspectiveOres.DrawUtility;
 
namespace PerspectiveOres
{
    public class Mod_PerspectiveOres : Mod
	{
		public Mod_PerspectiveOres(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_PerspectiveOres>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			//Make stationary rect for the filter box
			Rect filterRect = new Rect(inRect.x, inRect.y + 30f, inRect.width, 100f);
			//Prepare scrollable view area rect
			Rect scrollViewRect = inRect;
			scrollViewRect.y += 30f;
			scrollViewRect.yMax -= 30f;
			
			//Prepare line height cache
			TextAnchor anchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;

			//Calculate size of rect based on content
			Rect listRect = new Rect(0f, 0f, inRect.width - 30f, (lineNumber + 2) * lineHeight);

			options.Begin(inRect);
			options.End();
			Widgets.BeginScrollView(scrollViewRect, ref scrollPos, listRect, true);
				options.Begin(listRect);
				options.Label("PerspectiveOres.Settings.Header".Translate());
				DrawList(inRect, options);
				Text.Anchor = anchor;
				options.End();
			Widgets.EndScrollView();
			
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Perspective: Ores";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null) PerspectiveOresSetup.ProcessMap(Find.CurrentMap, true);
		}
	}

	public class ModSettings_PerspectiveOres : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Collections.Look(ref skippedMineableDefs, "skippedMineableDefs", LookMode.Value);
			base.ExposeData();
		}

		public static HashSet<string> skippedMineableDefs;
		public static Vector2 scrollPos;
	}
}
