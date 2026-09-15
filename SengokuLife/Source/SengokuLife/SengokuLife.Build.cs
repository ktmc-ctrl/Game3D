using UnrealBuildTool;

// SengokuLife: game / Unreal world layer (Gameplay Framework, Actors, World
// Bridge, Interaction, AI, UI, Persistence in later tasks).
//
// Boundary rules (T001-A):
//   * SengokuLife -> SengokuSim is allowed (declared below).
//   * SengokuSim must never reference this module.
public class SengokuLife : ModuleRules
{
	public SengokuLife(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
			"CoreUObject",
			"Engine",
			"InputCore",
			"SengokuSim",
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
		});
	}
}
