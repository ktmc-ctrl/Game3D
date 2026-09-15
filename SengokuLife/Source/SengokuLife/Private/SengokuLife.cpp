#include "SengokuLife.h"

#include "Modules/ModuleManager.h"

// Allowed direction: SengokuLife -> SengokuSim.
#include "SengokuSim.h"

DEFINE_LOG_CATEGORY(LogSengokuLife);

void FSengokuLifeModule::StartupModule()
{
	// Make sure the simulation layer is up before the game layer. This also
	// exercises the SengokuLife -> SengokuSim include/link dependency at build time.
	FModuleManager::LoadModuleChecked<FSengokuSimModule>(TEXT("SengokuSim"));

	UE_LOG(LogSengokuLife, Log, TEXT("SengokuLife module started"));
}

void FSengokuLifeModule::ShutdownModule()
{
	UE_LOG(LogSengokuLife, Log, TEXT("SengokuLife module shut down"));
}

IMPLEMENT_PRIMARY_GAME_MODULE(FSengokuLifeModule, SengokuLife, "SengokuLife");
