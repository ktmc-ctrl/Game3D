using UnrealBuildTool;
using System.Collections.Generic;

public class SengokuLifeEditorTarget : TargetRules
{
	public SengokuLifeEditorTarget(TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.V5;
		IncludeOrderVersion = EngineIncludeOrderVersion.Latest;

		// Dependency direction: SengokuLife -> SengokuSim. Never the reverse.
		ExtraModuleNames.AddRange(new string[] { "SengokuSim", "SengokuLife" });
	}
}
