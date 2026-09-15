using UnrealBuildTool;
using System.Collections.Generic;

public class SengokuLifeTarget : TargetRules
{
	public SengokuLifeTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.V5;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;

		// Dependency direction: SengokuLife -> SengokuSim. Never the reverse.
		ExtraModuleNames.AddRange(new string[] { "SengokuSim", "SengokuLife" });
	}
}
