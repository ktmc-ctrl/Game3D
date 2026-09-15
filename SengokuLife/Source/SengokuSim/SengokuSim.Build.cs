using UnrealBuildTool;

// SengokuSim: engine-independent simulation layer.
//
// Boundary rules (T001-A):
//   * Depends on "Core" only. No Engine, CoreUObject, Actor, GameFramework, AI,
//     Navigation, StateTree, Mass, UMG, etc.
//   * Must never depend on "SengokuLife" (the game/world layer depends on us,
//     not the other way around).
// Add a dependency here only when a concrete simulation feature needs it, not
// because it "might be useful later".
public class SengokuSim : ModuleRules
{
	public SengokuSim(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[]
		{
			"Core",
		});

		PrivateDependencyModuleNames.AddRange(new string[]
		{
		});
	}
}
